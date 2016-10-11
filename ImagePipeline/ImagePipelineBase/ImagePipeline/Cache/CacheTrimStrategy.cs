using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// CacheTrimStrategy helper class
    /// </summary>
    public class CacheTrimStrategyHelper : ICacheTrimStrategy
    {
        private readonly Func<double, double> _func;

        /// <summary>
        /// Instantiates the <see cref="CacheTrimStrategyHelper"/>.
        /// </summary>
        /// <param name="func">Delegate function</param>
        public CacheTrimStrategyHelper(Func<double, double> func)
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
