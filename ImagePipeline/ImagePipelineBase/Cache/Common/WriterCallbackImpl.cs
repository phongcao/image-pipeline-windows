using System;
using System.IO;

namespace Cache.Common
{
    /// <summary>
    /// Provides custom implementation for <see cref="IWriterCallback"/>
    /// </summary>
    public class WriterCallbackImpl : IWriterCallback
    {
        private Action<Stream> _func;

        /// <summary>
        /// Instantiates the <see cref="WriterCallbackImpl"/>
        /// </summary>
        /// <param name="func"></param>
        public WriterCallbackImpl(Action<Stream> func)
        {
            _func = func;
        }

        /// <summary>
        /// Write to the output stream
        /// </summary>
        /// <param name="os"></param>
        public void Write(Stream os)
        {
            _func(os);
        }
    }
}
