using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Request
{
    /// <summary>
    /// Provides the custom implementation for <see cref="BasePostprocessor"/>.
    /// </summary>
    public class BasePostprocessorImpl : BasePostprocessor
    {
        private Func<string> _nameFunc;
        private Action<SoftwareBitmap> _processFunc;

        /// <summary>
        /// Instantiates the <see cref="BasePostprocessorImpl"/>
        /// </summary>
        public BasePostprocessorImpl(Action<SoftwareBitmap> processFunc) : 
            this(null, processFunc)
        {
        }

        /// <summary>
        /// Instantiates the <see cref="BasePostprocessorImpl"/>
        /// </summary>
        public BasePostprocessorImpl(Func<string> nameFunc, Action<SoftwareBitmap> processFunc)
        {
            _nameFunc = nameFunc;
            _processFunc = processFunc;
        }

        /// <summary>
        /// Returns the name of this postprocessor.
        ///
        /// <para />Used for logging and analytics.
        /// </summary>
        public override string Name
        {
            get
            {
                if (_nameFunc == null)
                {
                    return base.Name;
                }
                else
                {
                    return _nameFunc();
                }
            }
        }

        /// <summary>
        /// The provided bitmap is a copy of the source bitmap and the implementation is 
        /// free to modify it.
        ///
        /// <param name="bitmap">The bitmap to be used both as input and as output.</param>
        /// </summary>
        public override void Process(SoftwareBitmap bitmap)
        {
            _processFunc(bitmap);
        }
    }
}
