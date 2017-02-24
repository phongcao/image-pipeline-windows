using FBCore.Common.Time;
using FBCore.Concurrency;
using ImagePipeline.Image;
using System;
using System.Threading.Tasks;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Manages jobs so that only one can be executed at a time and no more
    /// often than once in <code>_minimumJobIntervalMs</code> milliseconds.
    /// </summary>
    public class JobScheduler
    {
        internal const string QUEUE_TIME_KEY = "queueTime";

        internal enum JobState
        {
            IDLE,
            QUEUED,
            RUNNING,
            RUNNING_AND_PENDING
        }

        private readonly object _gate = new object();

        private readonly IExecutorService _executor;
        private readonly Func<EncodedImage, bool, Task> _jobRunnable;
        private readonly int _minimumJobIntervalMs;

        /// <summary>
        /// Job data.
        /// </summary>
        internal EncodedImage _encodedImage;
        internal bool _isLast;

        /// <summary>
        /// Job state.
        /// </summary>
        internal JobState _jobState;
        internal long _jobSubmitTime;
        internal long _jobStartTime;

        /// <summary>
        /// Instantiates the <see cref="JobScheduler"/>.
        /// </summary>
        public JobScheduler(
            IExecutorService executor,
            Func<EncodedImage, bool, Task> jobRunnable, 
            int minimumJobIntervalMs)
        {
            _executor = executor;
            _jobRunnable = jobRunnable;
            _minimumJobIntervalMs = minimumJobIntervalMs;
            _encodedImage = null;
            _isLast = false;
            _jobState = JobState.IDLE;
            _jobSubmitTime = 0;
            _jobStartTime = 0;
        }

        /// <summary>
        /// Clears the currently set job.
        ///
        /// <para /> In case the currently set job has been scheduled but
        /// not started yet, the job won't be executed.
        /// </summary>
        public void ClearJob()
        {
            EncodedImage oldEncodedImage;
            lock (_gate)
            {
                oldEncodedImage = _encodedImage;
                _encodedImage = null;
                _isLast = false;
            }

            EncodedImage.CloseSafely(oldEncodedImage);
        }

        /// <summary>
        /// Updates the job.
        ///
        /// <para />This just updates the job, but it doesn't schedule it.
        /// In order to be executed, the job has to be scheduled after
        /// being set. In case there was a previous job scheduled that has
        /// not yet started, this new job will be executed instead.
        /// </summary>
        /// <returns>
        /// Whether the job was successfully updated.
        /// </returns>
        public bool UpdateJob(EncodedImage encodedImage, bool isLast)
        {
            if (!ShouldProcess(encodedImage, isLast))
            {
                return false;
            }

            EncodedImage oldEncodedImage;
            lock (_gate)
            {
                oldEncodedImage = _encodedImage;
                _encodedImage = EncodedImage.CloneOrNull(encodedImage);
                _isLast = isLast;
            }

            EncodedImage.CloseSafely(oldEncodedImage);
            return true;
        }

        /// <summary>
        /// Schedules the currently set job (if any).
        ///
        /// <para /> This method can be called multiple times. It is
        /// guaranteed that each job set will be executed no more than
        /// once. It is guaranteed that the last job set will be executed,
        /// unless the job was cleared first.
        /// <para />The job will be scheduled no sooner than
        /// <code>minimumJobIntervalMs</code> milliseconds since the last
        /// job started.
        /// </summary>
        /// <returns>
        /// true if the job was scheduled, false if there was no valid job
        /// to be scheduled.
        /// </returns>
        public bool ScheduleJob()
        {
            long now = SystemClock.UptimeMillis;
            long when = 0;
            bool shouldEnqueue = false;
            lock (_gate)
            {
                if (!ShouldProcess(_encodedImage, _isLast))
                {
                    return false;
                }

                switch (_jobState)
                {
                    case JobState.IDLE:
                        when = Math.Max(_jobStartTime + _minimumJobIntervalMs, now);
                        shouldEnqueue = true;
                        _jobSubmitTime = now;
                        _jobState = JobState.QUEUED;
                        break;

                    case JobState.QUEUED:
                        // do nothing, the job is already queued
                        break;

                    case JobState.RUNNING:
                        _jobState = JobState.RUNNING_AND_PENDING;
                        break;

                    case JobState.RUNNING_AND_PENDING:
                        // do nothing, the next job is already pending
                        break;
                }
            }

            if (shouldEnqueue)
            {
                EnqueueJob(when - now);
            }

            return true;
        }

        private void EnqueueJob(long delay)
        {
            if (delay > 0)
            {
                JobStartExecutorSupplier.Get().Schedule(DoJob, delay);
            }
            else
            {
                _executor.Execute(DoJob);
            }
        }

        private async Task DoJob()
        {
            long now = SystemClock.UptimeMillis;
            EncodedImage input;
            bool isLast;
            lock (_gate)
            {
                input = _encodedImage;
                isLast = _isLast;
                _encodedImage = null;
                _isLast = false;
                _jobState = JobState.RUNNING;
                _jobStartTime = now;
            }

            try
            {
                // we need to do a check in case the job got cleared in the meantime
                if (ShouldProcess(input, isLast))
                {
                    await _jobRunnable.Invoke(input, isLast).ConfigureAwait(false);
                }
            }
            finally
            {
                EncodedImage.CloseSafely(input);
                OnJobFinished();
            }
        }

        private void OnJobFinished()
        {
            long now = SystemClock.UptimeMillis;
            long when = 0;
            bool shouldEnqueue = false;
            lock (_gate)
            {
                if (_jobState == JobState.RUNNING_AND_PENDING)
                {
                    when = Math.Max(_jobStartTime + _minimumJobIntervalMs, now);
                    shouldEnqueue = true;
                    _jobSubmitTime = now;
                    _jobState = JobState.QUEUED;
                }
                else
                {
                    _jobState = JobState.IDLE;
                }
            }

            if (shouldEnqueue)
            {
                EnqueueJob(when - now);
            }
        }

        private static bool ShouldProcess(EncodedImage encodedImage, bool isLast)
        {
            // the last result should always be processed, whereas
            // an intermediate result should be processed only if valid
            return isLast || EncodedImage.IsValid(encodedImage);
        }

        /// <summary>
        /// Gets the queued time in milliseconds for the currently running job.
        /// </summary>
        public long GetQueuedTime()
        {
            lock (_gate)
            {
                return _jobStartTime - _jobSubmitTime;
            }
        }

        internal static class JobStartExecutorSupplier
        {
            private static IScheduledExecutorService _jobStarterExecutor;

            internal static IScheduledExecutorService Get()
            {
                if (_jobStarterExecutor == null)
                {
                    _jobStarterExecutor = Executors.NewFixedThreadPool(1);
                }

                return _jobStarterExecutor;
            }
        }
    }
}
