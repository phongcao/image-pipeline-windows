using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// CacheTrimStrategy helper class
    /// </summary>
    public class CacheTrimStrategy : ICacheTrimStrategy
    {
        private readonly Func<double, double> _func;

        /// <summary>
        /// Instantiates the <see cref="CacheTrimStrategy"/>.
        /// </summary>
        /// <param name="func">Delegate function</param>
        public CacheTrimStrategy(Func<double, double> func)
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
