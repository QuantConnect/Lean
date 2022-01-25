using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    public static class RollingWindowExtensions
    {
        public static bool CrossAbove(this RollingWindow<IndicatorDataPoint> rw, RollingWindow<IndicatorDataPoint> other)
        {
            if (rw == null || other == null)
                return false;

            if (rw[1] <= other[1] && rw[0] > other[0])
                return true;

            return false;
        }

        public static bool CrossBelow(this RollingWindow<IndicatorDataPoint> rw, RollingWindow<IndicatorDataPoint> other)
        {
            if (rw == null || other == null)
                return false;

            if (rw[1] >= other[1] && rw[0] < other[0])
                return true;

            return false;
        }
    }

    public class StochasticRsiAlgorithm : QCAlgorithm
    {
        private DateTime _previous;
        private RelativeStrengthIndex _rsi;
        private Stochastic _sto;
        private ExponentialMovingAverage _ema;
        private readonly RollingWindow<IndicatorDataPoint> _stoKHistory = new(5);
        private readonly RollingWindow<IndicatorDataPoint> _stoDHistory = new(5);
        private readonly string _symbol = "SPY";
        private Chart _stockChart;
        private Series _ohlcSeries;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2004, 01, 01);
            SetEndDate(2015, 01, 01);

            AddSecurity(SecurityType.Equity, _symbol, Resolution.Daily);

            _rsi = RSI(_symbol, 14);
            _sto = STO(_symbol, 14);
            _ema = EMA(_symbol, 200);
            _sto.StochK.Updated += (sender, updated) => _stoKHistory.Add(updated);
            _sto.StochD.Updated += (sender, updated) => _stoDHistory.Add(updated);

            _stockChart = new Chart(_symbol + " Stock Plot");
            _ohlcSeries = new Series(_symbol, SeriesType.Candle);
            _stockChart.AddSeries(_ohlcSeries);
            _stockChart.AddSeries(new Series("EMA", SeriesType.Line, "$", color: System.Drawing.Color.Red));
            AddChart(_stockChart);

            SetWarmup(Math.Max(Math.Max(_rsi.WarmUpPeriod, _sto.WarmUpPeriod), _ema.WarmUpPeriod ) + 5);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            // only once per day
            if (_previous.Date == Time.Date) return;

            if (IsWarmingUp) return;

            var holding = Portfolio[_symbol];

            var tolerance = 0.0025m;

            /*
             * Long (Buy) whenever a bullish hidden divergence appears on the RSI which will be the first trigger.
             * Then, we will look for a positive cross between the stochastic oscillator and its moving average (Smoothing factor).
             * Finally, all of this must be done while the market is above its 200-period exponential moving average.
             */
            if (holding.Quantity <= 0 && _ema > data[_symbol].Close + tolerance && _stoKHistory.CrossAbove(_stoDHistory))
            {
                // longterm says buy as well
                SetHoldings(_symbol, 1.0);
            }
            /* Short (Sell) whenever a bearish hidden divergence appears on the RSI which will be the first trigger.
             * Then, we will look for a negative cross between the stochastic oscillator and its moving average (Smoothing factor).
             * Finally, all of this must be done while the market is below its 200-period exponential moving average.
             */
            else if (holding.Quantity >= 0 && _ema < data[_symbol].Close - tolerance && _stoKHistory.CrossBelow(_stoDHistory))
            {
                Liquidate(_symbol);
            }

            // plot lines
            Plot("RSI", _rsi.AverageLoss, _rsi.AverageGain);
            Plot("STO", _sto.StochK, _sto.FastStoch, _sto.StochD);
            Plot(_stockChart.Name, "EMA", _ema);

            _ohlcSeries.AddPoint(data[_symbol].EndTime.AddHours(-23), data[_symbol].Open);
            _ohlcSeries.AddPoint(data[_symbol].EndTime.AddHours(-12), data[_symbol].High);
            _ohlcSeries.AddPoint(data[_symbol].EndTime.AddHours(-12), data[_symbol].Low);
            _ohlcSeries.AddPoint(data[_symbol].EndTime, data[_symbol].Close);

            _previous = Time;
        }

        /* What is a hidden divergence? 
         * A normal divergence is when prices are rising and making new tops while a price-based indicator is making lower tops: 
         *  - When prices are making higher highs while the indicator is making lower highs, it is called a bearish divergence,
         *    and the market might stall.
         *  - When prices are making lower lows while the indicator is making higher lows, it is called a bullish divergence,
         *    and the market might show some upside potential.
         *    
         * A hidden divergence can be seen as the mirror view of a normal divergence. Here’s how:
         *    - When prices are making lower highs while the indicator is making higher highs, it is called a bearish hidden divergence,
         *      and the market might stall and continue lower.
         *    - When prices are making higher lows while the indicator is making lower lows, it is called a bullish hidden divergence,
         *      and the market might show some upside potential.
         */
        private bool hidden_divergence()
        {
            return false;
        }

    }
}
