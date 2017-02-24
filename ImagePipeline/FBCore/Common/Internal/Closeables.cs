using System;
using System.Diagnostics;
using System.IO;

namespace FBCore.Common.Internal
{
    /// <summary>
    /// Utility methods for working with <see cref="IDisposable"/> objects.
    ///
    /// @author Michael Lancaster
    /// @since 1.0
    /// </summary>
    public static class Closeables
    {
        /// <summary>
        /// Closes a <see cref="IDisposable"/>, with control over whether an 
        /// <code>IOException</code> may be thrown.
        /// This is primarily useful in a finally block, where a thrown exception 
        /// needs to be logged but not propagated (otherwise the original exception 
        /// will be lost).
        ///
        /// <para />If <code>swallowIOException</code> is true then we never throw 
        /// <code>IOException</code> but merely log it.
        ///
        /// <para />Example:
        /// <code>
        ///   public void UseStreamNicely() 
        ///   {
        ///     SomeStream stream = new SomeStream("foo");
        ///     boolean threw = true;
        ///     try 
        ///     {
        ///       // ... code which does something with the stream ...
        ///       threw = false;
        ///     } 
        ///     finally 
        ///     {
        ///       // If an exception occurs, rethrow it only if threw == false:
        ///       Closeables.close(stream, threw);
        ///     }
        ///   }
        /// </code>
        /// </summary>
        /// <param name="closeable">
        /// The <code>IDisposable</code> object to be closed, or null, in which case this
        /// method does nothing.
        /// </param>
        /// <param name="swallowIOException">
        /// If true, don't propagate IO exceptions thrown by the <code>Dispose</code> methods.
        /// </param>
        /// <exception cref="IOException">
        /// If <code>swallowIOException</code> is false and <code>Dispose</code> throws an
        /// <code>IOException</code>.
        /// </exception>
        public static void Close(IDisposable closeable, bool swallowIOException)
        {
            if (closeable == null)
            {
                return;
            }

            try
            {
                closeable.Dispose();
            }
            catch (IOException e)
            {
                if (swallowIOException)
                {
                    Debug.WriteLine($"IOException thrown while closing Closeable: { e.ToString() }");
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Closes the given <see cref="Stream"/>, logging any <code>IOException</code> 
        /// that's thrown rather than propagating it.
        ///
        /// <para />While it's not safe in the general case to ignore exceptions that are 
        /// thrown when closing an I/O resource, it should generally be safe in the case 
        /// of a resource that's being used only for reading, such as an 
        /// <code>inputStream</code>. 
        /// Unlike with writable resources, there's no chance that a failure that occurs 
        /// when closing the stream indicates a meaningful problem such as a failure to 
        /// flush all bytes to the underlying resource.
        /// </summary>
        /// <param name="inputStream">
        /// The input stream to be closed, or <code>null</code> in which case this method
        /// does nothing.
        /// </param>
        public static void CloseQuietly(Stream inputStream)
        {
            try
            {
                Close(inputStream, true);
            }
            catch (IOException)
            {
                throw;
            }
        }

        /// <summary>
        /// Closes the given <see cref="StreamReader"/>, logging any <code>IOException</code>
        /// that's thrown rather than propagating it.
        ///
        /// <para />While it's not safe in the general case to ignore exceptions that 
        /// are thrown when closing an I/O resource, it should generally be safe in the 
        /// case of a resource that's being used only for reading, such as a <code>Reader</code>.
        /// Unlike with writable resources, there's no chance that a failure that occurs 
        /// when closing the reader indicates a meaningful problem such as a failure to 
        /// flush all bytes to the underlying resource.
        /// </summary>
        /// <param name="reader">
        /// The reader to be closed, or <code>null</code> in which case this method
        /// does nothing.
        /// </param>
        public static void CloseQuietly(StreamReader reader)
        {
            try
            {
                Close(reader, true);
            }
            catch (IOException)
            {
                throw;
            }
        }
    }
}
