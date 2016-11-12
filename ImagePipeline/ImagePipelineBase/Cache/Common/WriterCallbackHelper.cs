using System;
using System.IO;

namespace Cache.Common
{
    /// <summary>
    /// Helper class for <see cref="IWriterCallback"/>
    /// </summary>
    public class WriterCallbackHelper : IWriterCallback
    {
        private Action<Stream> _func;

        /// <summary>
        /// Instantiates the <see cref="WriterCallbackHelper"/>
        /// </summary>
        /// <param name="func"></param>
        public WriterCallbackHelper(Action<Stream> func)
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
