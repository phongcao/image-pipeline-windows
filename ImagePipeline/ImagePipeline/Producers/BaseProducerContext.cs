using ImagePipeline.Request;
using System.Collections.Generic;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// IProducerContext that can be cancelled.
    /// Exposes low level API to manipulate state of the IProducerContext.
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
        /// Gets the image request.
        /// </summary>
        /// <returns>Image request that is being executed.</returns>
        public ImageRequest ImageRequest
        {
            get
            {
                return _imageRequest;
            }
        }

        /// <summary>
        /// Gets the request id.
        /// </summary>
        /// <returns>Id of this request.</returns>
        public string Id
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// Gets the producer listener.
        /// </summary>
        /// <returns>
        /// IProducerListener for producer's events.
        /// </returns>
        public IProducerListener Listener
        {
            get
            {
                return _producerListener;
            }
        }

        /// <summary>
        /// Gets the caller context.
        /// </summary>
        /// <returns>
        /// The <see cref="object"/> that indicates the caller's context.
        /// </returns>
        public object CallerContext
        {
            get
            {
                return _callerContext;
            }
        }

        /// <summary>
        /// Gets the lowest permitted request level.
        /// </summary>
        /// <returns>
        /// The lowest permitted <see cref="RequestLevel"/>.
        /// </returns>
        public int LowestPermittedRequestLevel
        {
            get
            {
                return _lowestPermittedRequestLevel;
            }
        }

        /// <summary>
        /// Checks if the request is a prefetch.
        /// </summary>
        /// <returns>
        /// true if the request is a prefetch, false otherwise.
        /// </returns>
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
        /// Gets the priority of the request.
        /// </summary>
        /// <returns>Priority of the request.</returns>
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
        /// Checks if request's owner expects intermediate results.
        /// </summary>
        /// <returns>
        /// true if request's owner expects intermediate results.
        /// </returns>
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
        /// Checks if the request is cancelled.
        /// </summary>
        /// <returns>
        /// true if the request is cancelled, false otherwise.
        /// </returns>
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
        /// Adds callbacks to the set of callbacks that are executed at
        /// various points during the processing of a request.
        /// </summary>
        /// <param name="callbacks">Callbacks to be executed.</param>
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
        /// <para />This method does not call any callbacks. Instead,
        /// caller of this method is responsible for iterating over
        /// returned list and calling appropriate method on each
        /// callback object. See <see cref="CallOnIsPrefetchChanged"/>.
        /// </summary>
        /// <returns>
        /// List of callbacks if the value actually changes,
        /// null otherwise.
        /// </returns>
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
        /// <para />This method does not call any callbacks. Instead,
        /// caller of this method is responsible for iterating over
        /// returned list and calling appropriate method on each
        /// callback object. See <see cref="CallOnPriorityChanged"/>.
        /// </summary>
        /// <returns>
        /// List of callbacks if the value actually changes,
        /// null otherwise.
        /// </returns>
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
        /// <para />This method does not call any callbacks. Instead,
        /// caller of this method is responsible for iterating over
        /// returned list and calling appropriate method on each
        /// callback object.
        /// </summary>
        /// <returns>
        /// List of callbacks if the value actually changes,
        /// null otherwise.
        /// </returns>
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
        /// Marks this IProducerContext as cancelled.
        ///
        /// <para />This method does not call any callbacks. Instead,
        /// caller of this method is responsible for iterating over
        /// returned list and calling appropriate method on each
        /// callback object. See <see cref="CallOnCancellationRequested"/>.
        /// </summary>
        /// <returns>
        /// List of callbacks if the value actually changes,
        /// null otherwise.
        /// </returns>
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
        /// Calls <code>OnCancellationRequested</code> on each element
        /// of the list. Does nothing if list == null.
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
        /// Calls <code>OnIsPrefetchChanged</code> on each element
        /// of the list. Does nothing if list == null.
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
        /// Calls <code>OnIsIntermediateResultExpected</code> on
        /// each element of the list. Does nothing if list == null
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
        /// Calls <code>OnPriorityChanged</code> on each element
        /// of the list. Does nothing if list == null.
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
