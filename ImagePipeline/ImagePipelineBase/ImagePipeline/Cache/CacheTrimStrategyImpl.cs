using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Provides custom implementation for <see cref="ICacheTrimStrategy"/>
    /// </summary>
    public class CacheTrimStrategyImpl : ICacheTrimStrategy
    {
        private readonly Func<double, double> _func;

        /// <summary>
        /// Instantiates the <see cref="CacheTrimStrategyImpl"/>.
        /// </summary>
        /// <param name="func">Delegate function</param>
        public CacheTrimStrategyImpl(Func<double, double> func)
        {
            _func = func;
        }

        /// <summary>
        /// Gets the trim ratio 
        /// </summary>
        /// <param name="trimType"></param>
        /// <returns></returns>
        public double GetTrimRatio(double trimType)
        {
            return _func(trimType);
        }
    }
}
