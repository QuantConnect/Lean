using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Premier Stochastic Oscillator (PSO) Indicator implementation.
    /// This indicator combines a stochastic oscillator with exponential moving averages to provide
    /// a normalized output between -1 and 1, which can be useful for identifying trends and 
    /// potential reversal points in the market.
    /// </summary>
    public class PremierStochasticOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Exponential Moving Averages (EMA) used in the calculation of the Premier Stochastic Oscillator (PSO).
        /// _ema1 performs the first smoothing of the Normalized Stochastic (0.1 * (Fast%K - 50)),
        /// and _ema2 applies a second smoothing, resulting in the Double-Smoothed Normalized Stochastic.
        /// </summary>
        private readonly ExponentialMovingAverage _ema1;
        private readonly ExponentialMovingAverage _ema2;

        /// <summary>
        /// Stochastic oscillator used to calculate the K value.
        /// </summary>
        private readonly Stochastic _stochastic;
        /// <summary>
        /// The PSO indicator itself, computed based on the normalized stochastic values.
        /// </summary>
        public IndicatorBase<IBaseDataBar> Pso { get; }

        /// <summary>
        /// The warm-up period necessary before the PSO indicator is considered ready.
        /// </summary>
        public int WarmUpPeriod { get; }
        /// <summary>
        /// Constructor for the Premier Stochastic Oscillator.
        /// Initializes the Stochastic and EMA indicators and calculates the warm-up period.
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="period">The period given to calculate FastK.</param>
        /// <param name="emaPeriod">The period for EMA calculations.</param>
        public PremierStochasticOscillator(string name, int period, int emaPeriod) : base(name)
        {
            _stochastic = new Stochastic(name, period, period, period);
            _ema1 = new ExponentialMovingAverage(name + "_EMA1", emaPeriod);
            _ema2 = new ExponentialMovingAverage(name + "_EMA2", emaPeriod);
            Pso = new FunctionalIndicator<IBaseDataBar>(
                name + "_PSO",
                input => ComputePSO(input),
                pso => _stochastic.IsReady && _ema1.IsReady && _ema2.IsReady,
                () => { }
            );
            WarmUpPeriod = period + 2 * (emaPeriod - 1);
        }
        /// <summary>
        /// Overloaded constructor to facilitate instantiation with a default name format.
        /// </summary>
        /// <param name="period">The period given to calculate FastK.</param>
        /// <param name="emaPeriod">The period for EMA calculations.</param>
        public PremierStochasticOscillator(int period, int emaPeriod)
            : this($"PSO({period},{emaPeriod})", period, emaPeriod)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Pso.IsReady;

        /// <summary>
        /// Computes the next value of the PSO indicator based on the current input bar.
        /// Updates the PSO and returns its current value.
        /// </summary>
        /// <param name="input">The current input bar containing market data.</param>
        /// <returns>The computed value of the PSO.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            Pso.Update(input);
            return Pso.Current.Value;
        }

        /// <summary>
        /// Computes the Premier Stochastic Oscillator (PSO) based on the current input.
        /// This calculation involves updating the stochastic oscillator and the EMAs,
        /// followed by calculating the PSO using the formula:
        /// PSO = (exp(EMA2) - 1) / (exp(EMA2) + 1)
        /// </summary>
        /// <param name="input">The current input bar containing market data.</param>
        /// <returns>The computed value of the PSO.</returns>
        private decimal ComputePSO(IBaseDataBar input)
        {
            _stochastic.Update(input);
            if (!_stochastic.IsReady)
            {
                return decimal.Zero;
            }

            var k = _stochastic.FastStoch.Current.Value;
            var nsk = 0.1m * (k - 50);
            _ema1.Update(new IndicatorDataPoint(DateTime.UtcNow, nsk));
            if (!_ema1.IsReady)
            {
                return decimal.Zero;
            }
            _ema2.Update(new IndicatorDataPoint(DateTime.UtcNow, _ema1.Current.Value));
            if (!_ema2.IsReady)
            {
                return decimal.Zero;
            }
            var expss = (decimal)Math.Exp((double)_ema2.Current.Value);
            return (expss - 1) / (expss + 1);
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _stochastic.Reset();
            Pso.Reset();
            _ema1.Reset();
            _ema2.Reset();
            base.Reset();
        }
    }
}
