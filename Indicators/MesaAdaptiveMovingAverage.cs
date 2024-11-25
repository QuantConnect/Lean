using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics;
using QuantConnect.Api;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    public class MesaAdaptiveMovingAverage : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly decimal _fastLimit;
        private readonly decimal _slowLimit;
        private readonly RollingWindow<decimal> _priceHistory;
        private readonly RollingWindow<decimal> _smoothHistory;
        private readonly RollingWindow<decimal> _detrendHistory;
        private readonly RollingWindow<decimal> _i1History;
        private readonly RollingWindow<decimal> _q1History;


        private decimal _prevPeriod;
        private decimal _prevI2;
        private decimal _prevQ2;
        private decimal _prevRe;
        private decimal _prevIm;
        private decimal _prevSmoothPeriod;
        private decimal _prevPhase;
        private decimal _prevMama;
        private decimal _prevFama;

        public MesaAdaptiveMovingAverage(string name, decimal fastLimit = 0.5m, decimal slowLimit = 0.05m)
            : base(name)
        {
            _fastLimit = fastLimit;
            _slowLimit = slowLimit;
            _priceHistory = new RollingWindow<decimal>(7);
            _smoothHistory = new RollingWindow<decimal>(7);
            _detrendHistory = new RollingWindow<decimal>(7);
            _i1History = new RollingWindow<decimal>(7);
            _q1History = new RollingWindow<decimal>(7);
            _prevPeriod = 0m;
            _prevI2 = 0m;
            _prevQ2 = 0m;
            _prevRe = 0m;
            _prevIm = 0m;
            _prevSmoothPeriod = 0m;
            _prevPhase = 0m;
            _prevMama = 0m;
            _prevFama = 0m;
        }

        public override bool IsReady => _priceHistory.IsReady;

        public int WarmUpPeriod => 7;

        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            //var price = (input.High + input.Low) / 2;
            var price = input.Close;
            if (_priceHistory.Count == 6)
            {
                _prevMama = _priceHistory.Average();
            }
            _priceHistory.Add(price);
            if (!_priceHistory.IsReady)
            {
                return decimal.Zero;
            }
            var adjustedPeriod = 0.075m * _prevPeriod + 0.54m;
            var smooth = (4 * _priceHistory[0] + 3 * _priceHistory[1] + 2 * _priceHistory[2] + _priceHistory[3]) / 10;
            var detrender = (0.0962m * smooth + 0.5769m * _smoothHistory[1] - 0.5769m * _smoothHistory[3] - 0.0962m * _smoothHistory[5]) * adjustedPeriod;

            //
            var q1 = (0.0962m * detrender + 0.5769m * _detrendHistory[1] - 0.5769m * _detrendHistory[3] - 0.0962m * _detrendHistory[5]) * adjustedPeriod;
            var i1 = _detrendHistory[2];

            //
            var ji = (0.0962m * i1 + 0.5769m * _i1History[1] - 0.5769m * _i1History[3] - 0.0962m * _i1History[5]) * adjustedPeriod;
            var jq = (0.0962m * q1 + 0.5769m * _q1History[1] - 0.5769m * _q1History[3] - 0.0962m * _q1History[5]) * adjustedPeriod;

            //
            var i2 = i1 - jq;
            var q2 = q1 + ji;

            //
            i2 = 0.2m * i2 + 0.8m * _prevI2;
            q2 = 0.2m * q2 + 0.8m * _prevQ2;

            //
            var re = i2 * _prevI2 + q2 * _prevQ2;
            var im = i2 * _prevQ2 - q2 * _prevI2;
            re = 0.2m * re + 0.8m * _prevRe;
            im = 0.2m * im + 0.8m * _prevIm;
            var period = 0m;
            if (im != 0 && re != 0)
            {
                //period = 360m / (decimal)Math.Atan((double)(im / re));
                const decimal Pi = 3.1415926535897932384626433833m;

                decimal ratio = im / re;
                double atanDouble = Math.Atan((double)ratio);
                decimal atan = (decimal)atanDouble;
                period = (atan != 0) ? 2 * Pi / atan : 0m;
                //period = (decimal)(2 * Math.PI / Math.Atan((double)(im / re)));
            }
            if (period > 1.5m * _prevPeriod)
            {
                period = 1.5m * _prevPeriod;
            }
            else if (period < 0.67m * _prevPeriod)
            {
                period = 0.67m * _prevPeriod;
            }
            if (period < 6)
            {
                period = 6;
            }
            else if (period > 50)
            {
                period = 50;
            }
            period = 0.2m * period + 0.8m * _prevPeriod;
            var smoothPeriod = 0.33m * period + 0.67m * _prevSmoothPeriod;
            var phase = 0m;
            if (i1 != 0)
            {
                //phase = (decimal)Math.Atan((double)(q1 / i1));
                phase = (decimal)(Math.Atan((double)(q1 / i1)) * 180 / Math.PI);
            }
            var deltaPhase = _prevPhase - phase;
            if (deltaPhase < 1)
            {
                deltaPhase = 1;
            }
            var alpha = _fastLimit / deltaPhase;
            if (alpha < _slowLimit)
            {
                alpha = _slowLimit;
            }
            var mama = alpha * _priceHistory[0] + (1 - alpha) * _prevMama;
            var fama = 0.5m * alpha * mama + (1 - 0.5m * alpha) * _prevFama;

            //Update
            _smoothHistory.Add(smooth);
            _detrendHistory.Add(detrender);
            _i1History.Add(i1);
            _q1History.Add(q1);
            _prevI2 = i2;
            _prevQ2 = q2;
            _prevRe = re;
            _prevIm = im;
            _prevPeriod = period;
            _prevSmoothPeriod = smoothPeriod;
            _prevPhase = phase;
            _prevMama = mama;
            _prevFama = fama;

            //Final
            return mama;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _priceHistory.Reset();
            _smoothHistory.Reset();
            _detrendHistory.Reset();
            _i1History.Reset();
            _q1History.Reset();
            _prevPeriod = 0m;
            _prevI2 = 0m;
            _prevQ2 = 0m;
            _prevRe = 0m;
            _prevIm = 0m;
            _prevSmoothPeriod = 0m;
            _prevPhase = 0m;
            _prevMama = 0m;
            _prevFama = 0m;
            base.Reset();
        }
    }
}
