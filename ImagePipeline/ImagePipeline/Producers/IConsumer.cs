using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Consumes data produced by <see cref="IProducer"/>.
    ///
    /// <para /> The producer uses this interface to notify its client when new data is ready or an error
    /// occurs. Execution of the image request is structured as a sequence of Producers. Each one
    /// consumes data produced by producer preceding it in the sequence.
    ///
    /// <para />For example decode is a producer that consumes data produced by the disk cache get producer.
    ///
    /// <para /> The consumer is passed new intermediate results via OnNewResult(isLast = false) method. Each
    /// consumer should expect that one of the following methods will be called exactly once, as the very
    /// last producer call:
    /// <ul>
    ///   <li> OnNewResult(isLast = true) if producer finishes successfully with a final result </li>
    ///   <li> OnFailure if producer failed to produce a final result </li>
    ///   <li> OnCancellation if producer was cancelled before a final result could be created </li>
    /// </ul>
    ///
    /// <para /> Implementations of this interface must be thread safe, as callback methods might be called
    /// on different threads.
    ///
    /// </summary>
    public interface IConsumer<T>
    {
        /// <summary>
        /// Called by a producer whenever new data is produced. This method should not throw an exception.
        ///
        /// <para /> In case when result is closeable resource producer will close it after onNewResult returns.
        /// Consumer needs to make copy of it if the resource must be accessed after that. Fortunately,
        /// with CloseableReferences, that should not impose too much overhead.
        ///
        /// <param name="newResult"></param>
        /// <param name="isLast">true if newResult is the last result</param>
        /// </summary>
        void OnNewResult(T newResult, bool isLast);

        /// <summary>
        /// Called by a producer whenever it terminates further work due to Throwable being thrown. This
        /// method should not throw an exception.
        /// </summary>
        void OnFailure(Exception error);

        /// <summary>
        /// Called by a producer whenever it is cancelled and won't produce any more results
        /// </summary>
        void OnCancellation();

        /// <summary>
        /// Called when the progress updates.
        ///
        /// <param name="progress">in range [0, 1]</param>
        /// </summary>
        void OnProgressUpdate(float progress);
    }
}
