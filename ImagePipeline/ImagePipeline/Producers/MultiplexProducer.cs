using FBCore.Common.Internal;
using ImagePipeline.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Producer for combining multiple identical requests into a single request.
    ///
    /// <para />Requests using the same key will be combined into a single request. 
    /// This request is only cancelled when all underlying requests are cancelled, 
    /// and returns values to all underlying consumers. If the request has already 
    /// return one or more results but has not finished, then any requests with the 
    /// same key will have the most recent result returned to them immediately.
    ///
    /// K: type of the key
    /// T: type of the closeable reference result that is returned to this producer
    /// </summary>
    public abstract class MultiplexProducer<K, T> : IProducer<T> where T : IDisposable
    {
        private readonly object _gate = new object();

        /// <summary>
        /// Map of multiplexers guarded by _gate lock. The lock should be used
        /// only to synchronize accesses to this map. In particular, no callbacks
        /// or third party code should be run under _gate lock.
        ///
        /// <para />The map might contain entries in progress, entries in progress
        /// for which cancellation has been requested and ignored, or cancelled
        /// entries for which OnCancellation has not been called yet.
        /// </summary>
        internal readonly IDictionary<K, Multiplexer> _multiplexers;
        private readonly IProducer<T> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="MultiplexProducer{K, T}"/>.
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        protected MultiplexProducer(IProducer<T> inputProducer)
        {
            _inputProducer = inputProducer;
            _multiplexers = new Dictionary<K, Multiplexer>();
        }

        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(IConsumer<T> consumer, IProducerContext context)
        {
            K key = GetKey(context);
            Multiplexer multiplexer;
            bool createdNewMultiplexer;

            // We do want to limit scope of this lock to guard only accesses
            // to _multiplexers map. However what we would like to do here
            // is to atomically lookup _multiplexers, add new consumer to
            // consumers set associated with the map's entry and call consumer's
            // callback with last intermediate result. We should not do all of
            // those things under _gate lock.
            do
            {
                createdNewMultiplexer = false;
                lock (_gate)
                {
                    multiplexer = GetExistingMultiplexer(key);
                    if (multiplexer == null)
                    {
                        multiplexer = CreateAndPutNewMultiplexer(key);
                        createdNewMultiplexer = true;
                    }
                }

                // AddNewConsumer may call consumer's OnNewResult method immediately. 
                // For this reason we release _gate lock. If multiplexer is removed 
                // from _multiplexers in the meantime, which is not very probable, 
                // then AddNewConsumer will fail and we will be able to retry.
            }
            while (!multiplexer.AddNewConsumer(consumer, context));

            if (createdNewMultiplexer)
            {
                multiplexer.StartInputProducerIfHasAttachedConsumers();
            }
        }

        private Multiplexer GetExistingMultiplexer(K key)
        {
            lock (_gate)
            {
                Multiplexer val = default(Multiplexer);
                _multiplexers.TryGetValue(key, out val);
                return val;
            }
        }

        private Multiplexer CreateAndPutNewMultiplexer(K key)
        {
            lock (_gate)
            {
                Multiplexer multiplexer = new Multiplexer(this, _inputProducer, key);
                _multiplexers.Add(key, multiplexer);
                return multiplexer;
            }
        }

        private void RemoveMultiplexer(K key, Multiplexer multiplexer)
        {
            lock (_gate)
            {
                Multiplexer val = default(Multiplexer);
                _multiplexers.TryGetValue(key, out val);
                if (val == multiplexer)
                {
                    _multiplexers.Remove(key);
                }
            }
        }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        protected abstract K GetKey(IProducerContext producerContext);

        /// <summary>
        /// Clones the result.
        /// </summary>
        public abstract T CloneOrNull(T result);

        /// <summary>
        /// Multiplexes same requests - passes the same result to multiple
        /// consumers, manages cancellation and maintains last intermediate
        /// result.
        ///
        /// <para />Multiplexed computation might be in one of 3 states:
        /// <ul>
        ///   <li>In progress.</li>
        ///   <li>
        ///     In progress after requesting cancellation
        ///     (cancellation has been denied).
        ///   </li>
        ///   <li>
        ///     Cancelled, but without OnCancellation method being called yet.
        ///   </li>
        /// </ul>
        ///
        /// <para /> In last case new consumers may be added before OnCancellation
        /// is called. When it is, the Multiplexer has to check if it is the case
        /// and start next producer once again if so.
        /// </summary>
        internal class Multiplexer
        {
            private readonly object _gate = new object();

            private MultiplexProducer<K, T> _parent;
            private IProducer<T> _inputProducer;
            private readonly K _key;

            /// <summary>
            /// Set of consumer-context pairs participating in multiplexing.
            /// Cancelled pairs are removed from the set.
            ///
            /// <para />Following invariant is maintained:
            /// if _consumerContextPairs is not empty, then this instance of
            /// Multiplexer is present in _multiplexers map.
            /// This way all ongoing multiplexed requests might be attached
            /// to by other requests.
            ///
            /// <para />A Multiplexer is removed from the map only if
            /// <ul>
            ///   <li>Final result is received.</li>
            ///   <li>Error is received.</li>
            ///   <li>
            ///     Cancellation notification is received and
            ///     _consumerContextPairs is empty.
            ///   </li>
            /// </ul>
            /// </summary>
            private readonly ConcurrentDictionary<Tuple<IConsumer<T>, IProducerContext>, object> 
                _consumerContextPairs;

            private T _lastIntermediateResult;
            private float _lastProgress;

            /// <summary>
            /// Producer context used for cancelling producers below
            /// MultiplexProducers, and for setting whether the request is
            /// a prefetch or not.
            ///
            /// <para />If not null, then underlying computation has been
            /// started, and no OnCancellation callback has been received yet.
            /// </summary>
            private BaseProducerContext _multiplexProducerContext;

            /// <summary>
            /// Currently used consumer of next producer.
            ///
            /// <para />The same Multiplexer might call
            /// _inputProducer.ProduceResults multiple times when cancellation
            /// happens. This field is used to guard against late callbacks.
            ///
            /// <para />If not null, then underlying computation has been
            /// started, and no OnCancellation callback has been received yet.
            /// </summary>
            private ForwardingConsumer _forwardingConsumer;

            internal Multiplexer(MultiplexProducer<K, T> parent, IProducer<T> inputProducer, K key)
            {
                _consumerContextPairs = new ConcurrentDictionary<Tuple<IConsumer<T>, IProducerContext>, object>();
                _parent = parent;
                _inputProducer = inputProducer;
                _key = key;
            }

            /// <summary>
            /// Tries to add consumer to set of consumers participating in
            /// multiplexing. If successful and appropriate intermediate
            /// result is already known, then it will be passed to the
            /// consumer.
            ///
            /// <para />This function will fail and return false if the
            /// multiplexer is not present in _multiplexers map.
            /// </summary>
            /// <returns>
            /// true if consumer was added successfully.
            /// </returns>
            internal bool AddNewConsumer(
                IConsumer<T> consumer,
                IProducerContext producerContext)
            {
                var consumerContextPair =
                    new Tuple<IConsumer<T>, IProducerContext>(consumer, producerContext);
                T lastIntermediateResult;
                IList<IProducerContextCallbacks > prefetchCallbacks;
                IList<IProducerContextCallbacks> priorityCallbacks;
                IList<IProducerContextCallbacks> intermediateResultsCallbacks;
                float lastProgress;

                // Check if Multiplexer is still in _multiplexers map, and if so
                // add new consumer. Also store current intermediate result - we
                // will notify consumer after acquiring appropriate lock.
                lock (_gate)
                {
                    if (_parent.GetExistingMultiplexer(_key) != this)
                    {
                        return false;
                    }

                    _consumerContextPairs.TryAdd(consumerContextPair, new object());
                    prefetchCallbacks = UpdateIsPrefetch();
                    priorityCallbacks = UpdatePriority();
                    intermediateResultsCallbacks = UpdateIsIntermediateResultExpected();
                    lastIntermediateResult = _lastIntermediateResult;
                    lastProgress = _lastProgress;
                }

                BaseProducerContext.CallOnIsPrefetchChanged(prefetchCallbacks);
                BaseProducerContext.CallOnPriorityChanged(priorityCallbacks);
                BaseProducerContext.CallOnIsIntermediateResultExpectedChanged(intermediateResultsCallbacks);

                lock (consumerContextPair)
                {
                    // Check if last result changed in the mean time.
                    // In such case we should not propagate it
                    lock (_gate)
                    {
                        if (!Equals(lastIntermediateResult, _lastIntermediateResult))
                        {
                            lastIntermediateResult = default(T);
                        }
                        else if (lastIntermediateResult != null)
                        {
                            lastIntermediateResult = _parent.CloneOrNull(lastIntermediateResult);
                        }
                    }

                    if (lastIntermediateResult != null)
                    {
                        if (lastProgress > 0)
                        {
                            consumer.OnProgressUpdate(lastProgress);
                        }

                        consumer.OnNewResult(lastIntermediateResult, false);
                        CloseSafely(lastIntermediateResult);
                    }
                }

                AddCallbacks(consumerContextPair, producerContext);
                return true;
            }

            /// <summary>
            /// Register callbacks to be called when cancellation of consumer
            /// is requested, or if the prefetch status of the consumer changes.
            /// </summary>
            private void AddCallbacks(
                Tuple<IConsumer<T>, IProducerContext> consumerContextPair,
                IProducerContext producerContext)
            {
                producerContext.AddCallbacks(
                    new BaseProducerContextCallbacks(
                        () =>
                        {
                            BaseProducerContext contextToCancel = null;
                            IList<IProducerContextCallbacks> isPrefetchCallbacks = null;
                            IList<IProducerContextCallbacks> priorityCallbacks = null;
                            IList<IProducerContextCallbacks> isIntermediateResultExpectedCallbacks = null;
                            bool pairWasRemoved = false;

                            lock (_gate)
                            {
                                object val = default(object);
                                pairWasRemoved = _consumerContextPairs.TryRemove(consumerContextPair, out val);
                                if (pairWasRemoved)
                                {
                                    if (_consumerContextPairs.IsEmpty)
                                    {
                                        contextToCancel = _multiplexProducerContext;
                                    }
                                    else
                                    {
                                        isPrefetchCallbacks = UpdateIsPrefetch();
                                        priorityCallbacks = UpdatePriority();
                                        isIntermediateResultExpectedCallbacks = UpdateIsIntermediateResultExpected();
                                    }
                                }
                            }

                            BaseProducerContext.CallOnIsPrefetchChanged(isPrefetchCallbacks);
                            BaseProducerContext.CallOnPriorityChanged(priorityCallbacks);
                            BaseProducerContext.CallOnIsIntermediateResultExpectedChanged(
                                isIntermediateResultExpectedCallbacks);

                            if (contextToCancel != null)
                            {
                                contextToCancel.Cancel();
                            }

                            if (pairWasRemoved)
                            {
                                consumerContextPair.Item1.OnCancellation();
                            }
                        },
                        () =>
                        {
                            BaseProducerContext.CallOnIsPrefetchChanged(UpdateIsPrefetch());
                        },
                        () =>
                        {
                            BaseProducerContext.CallOnIsIntermediateResultExpectedChanged(
                               UpdateIsIntermediateResultExpected());
                        },
                        () =>
                        {
                            BaseProducerContext.CallOnPriorityChanged(UpdatePriority());
                        }));
            }

            /// <summary>
            /// Starts next producer if it is not started yet and there is
            /// at least one Consumer waiting for the data. If all consumers
            /// are cancelled, then this multiplexer is removed from _request
            /// map to clean up.
            /// </summary>
            internal void StartInputProducerIfHasAttachedConsumers()
            {
                BaseProducerContext multiplexProducerContext;
                ForwardingConsumer forwardingConsumer;
                lock (_gate)
                {
                    Preconditions.CheckArgument(_multiplexProducerContext == null);
                    Preconditions.CheckArgument(_forwardingConsumer == null);

                    // Cleanup if all consumers have been cancelled before
                    // this method was called
                    if (_consumerContextPairs.IsEmpty)
                    {
                        _parent.RemoveMultiplexer(_key, this);
                        return;
                    }

                    var iterator = _consumerContextPairs.GetEnumerator();
                    iterator.MoveNext();
                    IProducerContext producerContext = iterator.Current.Key.Item2;
                    _multiplexProducerContext = new BaseProducerContext(
                        producerContext.ImageRequest,
                        producerContext.Id,
                        producerContext.Listener,
                        producerContext.CallerContext,
                        producerContext.LowestPermittedRequestLevel,
                        ComputeIsPrefetch(),
                        ComputeIsIntermediateResultExpected(),
                        ComputePriority());

                    _forwardingConsumer = new ForwardingConsumer(this);
                    multiplexProducerContext = _multiplexProducerContext;
                    forwardingConsumer = _forwardingConsumer;
                }

                _inputProducer.ProduceResults(
                    forwardingConsumer,
                    multiplexProducerContext);
            }

            private IList<IProducerContextCallbacks> UpdateIsPrefetch()
            {
                lock (_gate)
                {
                    if (_multiplexProducerContext == null)
                    {
                        return null;
                    }

                    return _multiplexProducerContext.SetIsPrefetchNoCallbacks(ComputeIsPrefetch());
                }
            }

            private bool ComputeIsPrefetch()
            {
                lock (_gate)
                {
                    foreach (var pair in _consumerContextPairs)
                    {
                        if (!pair.Key.Item2.IsPrefetch)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            private IList<IProducerContextCallbacks> UpdateIsIntermediateResultExpected()
            {
                lock (_gate)
                {
                    if (_multiplexProducerContext == null)
                    {
                        return null;
                    }

                    return _multiplexProducerContext.SetIsIntermediateResultExpectedNoCallbacks(
                        ComputeIsIntermediateResultExpected());
                }
            }

            private bool ComputeIsIntermediateResultExpected()
            {
                lock (_gate)
                {
                    foreach (var pair in _consumerContextPairs)
                    {
                        if (pair.Key.Item2.IsIntermediateResultExpected)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            private IList<IProducerContextCallbacks> UpdatePriority()
            {
                lock (_gate)
                {
                    if (_multiplexProducerContext == null)
                    {
                        return null;
                    }

                    return _multiplexProducerContext.SetPriorityNoCallbacks(ComputePriority());
                }
            }

            private int ComputePriority()
            {
                lock (_gate)
                {
                    int priority = Priority.LOW;
                    foreach (var pair in _consumerContextPairs)
                    {
                        priority = Priority.GetHigherPriority(priority, pair.Key.Item2.Priority);
                    }

                    return priority;
                }
            }

            public void OnFailure(ForwardingConsumer consumer, Exception t)
            {
                IEnumerator<KeyValuePair<Tuple<IConsumer<T>, IProducerContext>, object>> iterator;
                lock (_gate)
                {
                    // Check for late callbacks
                    if (_forwardingConsumer != consumer)
                    {
                        return;
                    }

                    iterator = _consumerContextPairs.GetEnumerator();
                    while (iterator.MoveNext())
                    {
                        iterator.Current.Key.Item1.OnFailure(t);
                    }

                    _consumerContextPairs.Clear();
                    _parent.RemoveMultiplexer(_key, this);
                    CloseSafely(_lastIntermediateResult);
                    _lastIntermediateResult = default(T);
                }
            }

            public void OnNextResult(
                ForwardingConsumer consumer,
                T closeableObject,
                bool isFinal)
            {
                IEnumerator<KeyValuePair<Tuple<IConsumer<T>, IProducerContext>, object>> iterator;
                lock (_gate)
                {
                    // Check for late callbacks
                    if (_forwardingConsumer != consumer)
                    {
                        return;
                    }

                    CloseSafely(_lastIntermediateResult);
                    _lastIntermediateResult = default(T);

                    iterator = _consumerContextPairs.GetEnumerator();
                    while (iterator.MoveNext())
                    {
                        iterator.Current.Key.Item1.OnNewResult(closeableObject, isFinal);
                    }

                    if (!isFinal)
                    {
                        _lastIntermediateResult = _parent.CloneOrNull(closeableObject);
                    }
                    else
                    {
                        _consumerContextPairs.Clear();
                        _parent.RemoveMultiplexer(_key, this);
                    }
                }
            }

            public void OnCancelled(ForwardingConsumer forwardingConsumer)
            {
                lock (_gate)
                {
                    // Check for late callbacks
                    if (_forwardingConsumer != forwardingConsumer)
                    {
                        return;
                    }

                    _forwardingConsumer = null;
                    _multiplexProducerContext = null;
                    CloseSafely(_lastIntermediateResult);
                    _lastIntermediateResult = default(T);
                }

                StartInputProducerIfHasAttachedConsumers();
            }

            public void OnProgressUpdate(ForwardingConsumer forwardingConsumer, float progress)
            {
                IEnumerator<KeyValuePair<Tuple<IConsumer<T>, IProducerContext>, object>> iterator;
                lock (_gate)
                {
                    // Check for late callbacks
                    if (_forwardingConsumer != forwardingConsumer)
                    {
                        return;
                    }

                    _lastProgress = progress;
                    iterator = _consumerContextPairs.GetEnumerator();
                }

                while (iterator.MoveNext())
                {
                    lock (iterator)
                    {
                        iterator.Current.Key.Item1.OnProgressUpdate(progress);
                    }
                }
            }

            private void CloseSafely(IDisposable obj)
            {
                try
                {
                    if (obj != null)
                    {
                        obj.Dispose();
                    }
                }
                catch (IOException)
                {
                    throw;
                }
            }

            /// <summary>
            /// Forwards <see cref="IConsumer{T}"/> methods to Multiplexer.
            /// </summary>
            internal class ForwardingConsumer : BaseConsumer<T>
            {
                Multiplexer _parent;

                internal ForwardingConsumer(Multiplexer parent)
                {
                    _parent = parent;
                }

                protected override void OnNewResultImpl(T newResult, bool isLast)
                {
                    _parent.OnNextResult(this, newResult, isLast);
                }

                protected override void OnFailureImpl(Exception t)
                {
                    _parent.OnFailure(this, t);
                }

                protected override void OnCancellationImpl()
                {
                    _parent.OnCancelled(this);
                }

                protected override void OnProgressUpdateImpl(float progress)
                {
                    _parent.OnProgressUpdate(this, progress);
                }
            }
        }
    }
}
