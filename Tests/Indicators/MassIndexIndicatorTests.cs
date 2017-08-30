using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Diagnostics.Eventing.Reader;

namespace QuantConnect.Indicators
{
    public class MassIndexIndicator : IndicatorBase<TradeBar>
    {
        private int _sumPeriod;
        private Identity _highLow;
        private ExponentialMovingAverage _ema;
        private ExponentialMovingAverage _ema2;
        private Sum _sum;

        /// <summary>
        /// Initializes a new instance of the <see cref="MassIndexIndicator"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="emaPeriod">The ema period.</param>
        /// <param name="sumPeriod">The sum period.</param>
        public MassIndexIndicator(string name, int emaPeriod, int sumPeriod)
            : base(name)
        {
            _sumPeriod = sumPeriod;
            _highLow = new Identity("HighLow");
            _ema = new ExponentialMovingAverage(emaPeriod).Of(_highLow);
            _ema2 = new ExponentialMovingAverage(emaPeriod).Of(_ema);
            _sum = new Sum(sumPeriod).Of(_ema.Over(_ema2));
        }

        public MassIndexIndicator(int emaPeriod = 9, int sumPeriod = 25)
            : this(string.Format("MII_{0}_{1}", emaPeriod, sumPeriod), emaPeriod, sumPeriod)
        {}

        public override bool IsReady => _sum.IsReady;

        public override void Reset()
        {
            base.Reset();
            _highLow.Reset();
            _ema.Reset();
            _ema2.Reset();
            _sum.Reset();
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            _highLow.Update(new IndicatorDataPoint
            {
                Time = input.Time,
                Value = input.High - input.Low
            });

            if (!_sum.IsReady)
            {
                return _sumPeriod;
            }
            else
            {
                return _sum;
            }

        }
    }
}

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class MassIndexIndicatorTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new MassIndexIndicator();
        }

        protected override string TestFileName => "spy_mass_index_25.csv";
        protected override string TestColumnName => "MassIndex";
    }
}