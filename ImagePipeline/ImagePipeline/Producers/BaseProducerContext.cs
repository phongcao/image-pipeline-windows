using ImagePipeline.Request;
using System.Collections.Generic;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// ProducerContext that can be cancelled. Exposes low level API to manipulate state of the
    /// ProducerContext.
    /// </summary>
    public class BaseProducerContext : IProducerContext
    {
        private readonly object _gate = new object();

        private readonly ImageRequest _imageRequest;
        private readonly string _id;
        private readonly IProducerListener _producerListener;
        private readonly object _callerContext;
        private readonly int _lowestPermittedRequestLevel;

        private bool _isPrefetch;
        private int _priority;
        private bool _isIntermediateResultExpected;
        private bool _isCancelled;
        private readonly IList<IProducerContextCallbacks> _callbacks;

        /// <summary>
        /// Instantiates the <see cref="BaseProducerContext"/>
        /// </summary>
        public BaseProducerContext(
            ImageRequest imageRequest,
            string id,
            IProducerListener producerListener,
            object callerContext,
            int lowestPermittedRequestLevel,
            bool isPrefetch,
            bool isIntermediateResultExpected,
            int priority)
        {
            _imageRequest = imageRequest;
            _id = id;
            _producerListener = producerListener;
            _callerContext = callerContext;
            _lowestPermittedRequestLevel = lowestPermittedRequestLevel;

            _isPrefetch = isPrefetch;
            _priority = priority;
            _isIntermediateResultExpected = isIntermediateResultExpected;

            _isCancelled = false;
            _callbacks = new List<IProducerContextCallbacks>();
        }

        /// <summary>
        /// @return image request that is being executed
        /// </summary>
        public ImageRequest ImageRequest
        {
            get
            {
                return _imageRequest;
            }
        }

        /// <summary>
        /// @return id of this request
        /// </summary>
        public string Id
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// @return IProducerListener for producer's events
        /// </summary>
        public IProducerListener Listener
        {
            get
            {
                return _producerListener;
            }
        }

        /// <summary>
        /// @return the <see cref="object"/> that indicates the caller's context
        /// </summary>
        public object CallerContext
        {
            get
            {
                return _callerContext;
            }
        }

        /// <summary>
        /// @return the lowest permitted <see cref="RequestLevel"/>
        /// </summary>
        public int LowestPermittedRequestLevel
        {
            get
            {
                return _lowestPermittedRequestLevel;
            }
        }

        /// <summary>
        /// @return true if the request is a prefetch, false otherwise.
        /// </summary>
        public bool IsPrefetch
        {
            get
            {
                lock (_gate)
                {
                    return _isPrefetch;
                }
            }
        }

        /// <summary>
        /// @return priority of the request.
        /// </summary>
        public int Priority
        {
            get
            {
                lock (_gate)
                {
                    return _priority;
                }
            }
        }

        /// <summary>
        /// @return true if request's owner expects intermediate results
        /// </summary>
        public bool IsIntermediateResultExpected
        {
            get
            {
                lock (_gate)
                {
                    return _isIntermediateResultExpected;
                }
            }
        }

        /// <summary>
        /// @return true if the request is cancelled, false otherwise.
        /// </summary>
        public bool IsCancelled
        {
            get
            {
                lock (_gate)
                {
                    return _isCancelled;
                }
            }
        }

        /// <summary>
        /// Adds callbacks to the set of callbacks that are executed at various points during the
        /// processing of a request.
        /// <param name="callbacks">callbacks to be executed</param>
        /// </summary>
        public void AddCallbacks(IProducerContextCallbacks callbacks)
        {
            bool cancelImmediately = false;
            lock (_gate)
            {
                _callbacks.Add(callbacks);
                if (_isCancelled)
                {
                    cancelImmediately = true;
                }
            }

            if (cancelImmediately)
            {
                callbacks.OnCancellationRequested();
            }
        }

        /// <summary>
        /// Cancels the request processing and calls appropriate callbacks.
        /// </summary>
        public void Cancel()
        {
            CallOnCancellationRequested(CancelNoCallbacks());
        }

        /// <summary>
        /// Changes isPrefetch property.
        ///
        /// <para /> This method does not call any callbacks. Instead, caller of this method is responsible for
        /// iterating over returned list and calling appropriate method on each callback object.
        /// {@see CallOnIsPrefetchChanged}
        ///
        /// @return list of callbacks if the value actually changes, null otherwise
        /// </summary>
        public IList<IProducerContextCallbacks> SetIsPrefetchNoCallbacks(bool isPrefetch)
        {
            lock (_gate)
            {
                if (isPrefetch == _isPrefetch)
                {
                    return null;
                }

                _isPrefetch = isPrefetch;
                return new List<IProducerContextCallbacks>(_callbacks);
            }
        }

        /// <summary>
        /// Changes priority.
        ///
        /// <para /> This method does not call any callbacks. Instead, caller of this method is responsible for
        /// iterating over returned list and calling appropriate method on each callback object.
        /// {@see CallOnPriorityChanged}
        ///
        /// @return list of callbacks if the value actually changes, null otherwise
        /// </summary>
        public IList<IProducerContextCallbacks> SetPriorityNoCallbacks(int priority)
        {
            lock (_gate)
            {
                if (priority == _priority)
                {
                    return null;
                }

                _priority = priority;
                return new List<IProducerContextCallbacks>(_callbacks);
            }
        }

        /// <summary>
        /// Changes IsIntermediateResultExpected property.
        ///
        /// <para /> This method does not call any callbacks. Instead, caller of this method is responsible for
        /// iterating over returned list and calling appropriate method on each callback object.
        /// {@see CallOnIntermediateResultChanged}
        ///
        /// @return list of callbacks if the value actually changes, null otherwise
        /// </summary>
        public IList<IProducerContextCallbacks> SetIsIntermediateResultExpectedNoCallbacks(
            bool isIntermediateResultExpected)
        {
            lock (_gate)
            {
                if (isIntermediateResultExpected == _isIntermediateResultExpected)
                {
                    return null;
                }

                _isIntermediateResultExpected = isIntermediateResultExpected;
                return new List<IProducerContextCallbacks>(_callbacks);
            }
        }

        /// <summary>
        /// Marks this ProducerContext as cancelled.
        ///
        /// <para /> This method does not call any callbacks. Instead, caller of this method is responsible for
        /// iterating over returned list and calling appropriate method on each callback object.
        /// {@see CallOnCancellationRequested}
        ///
        /// @return list of callbacks if the value actually changes, null otherwise
        /// </summary>
        public IList<IProducerContextCallbacks> CancelNoCallbacks()
        {
            lock (_gate)
            {
                if (_isCancelled)
                {
                    return null;
                }

                _isCancelled = true;
                return new List<IProducerContextCallbacks>(_callbacks);
            }
        }

        /// <summary>
        /// Calls <code> OnCancellationRequested</code> on each element of the list. Does nothing if list == null
        /// </summary>
        public static void CallOnCancellationRequested(
            IList<IProducerContextCallbacks> callbacks)
        {
            if (callbacks == null)
            {
                return;
            }

            foreach (var callback in callbacks)
            {
                callback.OnCancellationRequested();
            }
        }

        /// <summary>
        /// Calls <code> OnIsPrefetchChanged</code> on each element of the list. Does nothing if list == null
        /// </summary>
        public static void CallOnIsPrefetchChanged(
            IList<IProducerContextCallbacks> callbacks)
        {
            if (callbacks == null)
            {
                return;
            }

            foreach (var callback in callbacks)
            {
                callback.OnIsPrefetchChanged();
            }
        }

        /// <summary>
        /// Calls <code> OnIsIntermediateResultExpected</code> on each element of the list. Does nothing if
        /// list == null
        /// </summary>
        public static void CallOnIsIntermediateResultExpectedChanged(
            IList<IProducerContextCallbacks> callbacks)
        {
            if (callbacks == null)
            {
                return;
            }

            foreach (var callback in callbacks)
            {
                callback.OnIsIntermediateResultExpectedChanged();
            }
        }

        /// <summary>
        /// Calls <code> OnPriorityChanged</code> on each element of the list. Does nothing if list == null
        /// </summary>
        public static void CallOnPriorityChanged(IList<IProducerContextCallbacks> callbacks)
        {
            if (callbacks == null)
            {
                return;
            }

            foreach (var callback in callbacks)
            {
                callback.OnPriorityChanged();
            }
        }
    }
}
