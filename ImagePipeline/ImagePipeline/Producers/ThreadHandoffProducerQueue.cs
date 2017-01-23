using FBCore.Common.Internal;
using FBCore.Concurrency;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// <see cref="ThreadHandoffProducer{T}"/> queue
    /// </summary>
    public class ThreadHandoffProducerQueue
    {
        private readonly object _gate = new object();

        private bool _queueing;
        private readonly LinkedList<Action> _runnableList;
        private readonly IExecutorService _executor;

        /// <summary>
        /// Instantiates the <see cref="ThreadHandoffProducerQueue"/>
        /// </summary>
        /// <param name="executor"></param>
        public ThreadHandoffProducerQueue(IExecutorService executor)
        {
            _queueing = false;
            _executor = Preconditions.CheckNotNull(executor);
            _runnableList = new LinkedList<Action>();
        }

        /// <summary>
        /// Adds the action to the end of the queue
        /// </summary>
        public void AddToQueueOrExecute(Action runnable)
        {
            lock (_gate)
            {
                if (_queueing)
                {
                    _runnableList.AddLast(runnable);
                }
                else
                {
                    _executor.Execute(runnable);
                }
            }
        }

        /// <summary>
        /// Starts queueing
        /// </summary>
        public void StartQueueing()
        {
            lock (_gate)
            {
                _queueing = true;
            }
        }

        /// <summary>
        /// Stops queueing
        /// </summary>
        public void StopQueuing()
        {
            lock (_gate)
            {
                _queueing = false;
                ExecInQueue();
            }
        }

        private void ExecInQueue()
        {
            while (_runnableList.Count != 0)
            {
                _executor.Execute(_runnableList.Last.Value);
                _runnableList.RemoveLast();
            }

            _runnableList.Clear();
        }

        /// <summary>
        /// Removes the action
        /// </summary>
        public void Remove(Action runnable)
        {
            lock (_gate)
            {
                _runnableList.Remove(runnable);
            }
        }

        /// <summary>
        /// Checks if it's queueing
        /// </summary>
        public bool IsQueueing()
        {
            lock (_gate)
            {
                return _queueing;
            }
        }
    }
}
