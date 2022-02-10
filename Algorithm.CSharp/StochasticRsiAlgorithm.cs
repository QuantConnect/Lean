using QuantConnect.Data;
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

        public static bool CrossAbove(this RollingWindow<IndicatorDataPoint> rw, int level)
        {
            if (rw == null)
                return false;

            if (rw[1] <= level && rw[0] > level)
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

        public static bool CrossBelow(this RollingWindow<IndicatorDataPoint> rw, int level)
        {
            if (rw == null)
                return false;

            if (rw[1] >= level && rw[0] < level)
                return true;

            return false;
        }
    }

    public class StochasticRsiAlgorithm : QCAlgorithm
    {
        private RelativeStrengthIndex _rsi;
        private Stochastic _sto;
        private ExponentialMovingAverage _ema;
        private readonly RollingWindow<IndicatorDataPoint> _stoKHistory = new(5);
        private readonly RollingWindow<IndicatorDataPoint> _stoDHistory = new(5);
        private readonly RollingWindow<IndicatorDataPoint> _rsiHistory = new(60);
        private readonly RollingWindow<decimal> _lowHistory = new(60);
        private DateTime _plotSample;
        private TimeSpan _plotSamplePeriod;
        private readonly string _symbol = "EURUSD";
        private Chart _stockChart;
        private Series _ohlcSeries;
        private readonly DateTime _startDate = new DateTime(2011, 1, 1);
        private readonly DateTime _endDate = new DateTime(2015, 1, 1);
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(_startDate);
            SetEndDate(_endDate);

            var sec = AddForex(_symbol, Resolution.Hour, market:Market.Oanda);
            _rsi = RSI(_symbol, 14);
            _sto = STO(_symbol, 14);
            _ema = EMA(_symbol, 200);
            _sto.StochK.Updated += (sender, updated) => _stoKHistory.Add(updated);
            _sto.StochD.Updated += (sender, updated) => _stoDHistory.Add(updated);
            _rsi.Updated += (sender, updated) => _rsiHistory.Add(updated);

            _stockChart = new Chart(_symbol + " Stock Plot");
            _ohlcSeries = new Series(_symbol, SeriesType.Candle);
            _stockChart.AddSeries(_ohlcSeries);
            _stockChart.AddSeries(new Series("EMA", SeriesType.Line, "$", color: System.Drawing.Color.Red));
            _stockChart.AddSeries(new Series("RSI", SeriesType.Line, 1));
            AddChart(_stockChart);

            _plotSamplePeriod = TimeSpan.FromHours((_endDate - _startDate).TotalHours / 2000);
            SetWarmup(Math.Max(Math.Max(_rsi.WarmUpPeriod, _sto.WarmUpPeriod), _ema.WarmUpPeriod) + 5);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public override void OnData(Slice slice)
        {
            if (slice == null || slice.HasData == false) return;

            var data = slice.QuoteBars;
            _lowHistory.Add(data[_symbol].Low);

            if (IsWarmingUp) return;

            var holding = Portfolio[_symbol];

            var tolerance = 0.0025m;

            /*
             * Long (Buy) whenever a bullish hidden divergence appears on the RSI which will be the first trigger.
             * Then, we will look for a positive cross between the stochastic oscillator and its moving average (Smoothing factor).
             * Finally, all of this must be done while the market is above its 200-period exponential moving average.
             */
            if (holding.Quantity <= 0 && 
                data[_symbol].Close + tolerance > _ema && 
                _stoKHistory.CrossAbove(_stoDHistory) &&
                BullHiddenDivergence(_rsiHistory, _lowHistory) )
            {
                // longterm says buy as well
                SetHoldings(_symbol, 1.0);
            }
            /* Short (Sell) whenever a bearish hidden divergence appears on the RSI which will be the first trigger.
             * Then, we will look for a negative cross between the stochastic oscillator and its moving average (Smoothing factor).
             * Finally, all of this must be done while the market is below its 200-period exponential moving average.
             */
            else if (holding.Quantity >= 0 && 
                     data[_symbol].Close - tolerance < _ema &&
                     _stoKHistory.CrossBelow(_stoDHistory))
            {
                Liquidate(_symbol);
            }

            // plot lines
            // if (Time > _plotSample)
            {
                _plotSample = Time.Add(_plotSamplePeriod);
                Plot("STO", _sto.StochK, _sto.FastStoch, _sto.StochD);
                Plot(_stockChart.Name, "EMA", _ema);
                Plot(_stockChart.Name, "RSI", _rsi);

                _ohlcSeries.AddPoint(data[_symbol].EndTime.AddMinutes(-59), data[_symbol].Open);
                _ohlcSeries.AddPoint(data[_symbol].EndTime.AddMinutes(-30), data[_symbol].High);
                _ohlcSeries.AddPoint(data[_symbol].EndTime.AddMinutes(-30), data[_symbol].Low);
                _ohlcSeries.AddPoint(data[_symbol].EndTime, data[_symbol].Close);
            }

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
        private static bool BullHiddenDivergence(RollingWindow<IndicatorDataPoint> rsi, RollingWindow<decimal> low, int level = 50)
        {
            if (rsi.CrossBelow(level))
            {
                // Lookback
                for (int a = 0; a < rsi.Count; a++)
                {
                    // When RSI > Level
                    if (rsi[a] > level)
                    {
                        // Lookback from RSI > Level
                        for (int r = a + 1; r < rsi.Count; r++) 
                        {
                            // If RSI < Level AND RSI < Current RSI AND Low > Current Low
                            if (rsi[r] < level && rsi[r] < rsi[0] && low[r] > low[0])
                                return true;
                        }
                    }
                }
            }
            return false;
        }

    }
}
