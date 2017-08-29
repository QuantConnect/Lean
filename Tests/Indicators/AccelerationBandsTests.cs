using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class AccelerationBandsTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new AccelerationBands(20, 2m);
        }

        [Test]
        public void ComparesWithExternalDataUpperBand() { }

        [Test]
        public void ComparesWithExternalDataLowerBand() { }

        protected override string TestFileName { get; }
        protected override string TestColumnName { get; }
    }

    /// <summary>
    /// The Acceleration Bands created by Price Headley plots upper and lower envelope bands around a moving average. 
    /// </summary>
    /// <seealso cref="QuantConnect.Indicators.IndicatorBase{QuantConnect.Data.Market.TradeBar}" />
    public class AccelerationBands : IndicatorBase<TradeBar>
    {
        private decimal _with;

        /// <summary>
        /// Gets the type of moving average
        /// </summary>
        public MovingAverageType MovingAverageType { get; private set; }

        /// <summary>
        /// Gets the middle acceleration band (moving average)
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> MiddleBand { get; private set; }

        /// <summary>
        /// Gets the upper acceleration band  (High * ( 1 + Width * (High - Low) / (High + Low)))
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> UpperBand { get; private set; }

        /// <summary>
        /// Gets the lower acceleration band  (Low * (1 - Width * (High - Low)/ (High + Low)))
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> LowerBand { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="AccelerationBands"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="period">The period.</param>
        /// <param name="width">The width.</param>
        /// <param name="movingAverageType">Average type of the moving.</param>
        public AccelerationBands(string name, int period, decimal width,
            MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            _with = width;
            MovingAverageType = movingAverageType;
            MiddleBand = movingAverageType.AsIndicator(name + "_MiddleBand", period);
            LowerBand = movingAverageType.AsIndicator(name + "_LowerBand", period);
            UpperBand = movingAverageType.AsIndicator(name + "_UpperBand", period);

        }

        public override bool IsReady { get; }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            MiddleBand.Update(new IndicatorDataPoint
            {
                Time = input.Time,
                Value = input.Close
            });

            LowerBand.Update(new IndicatorDataPoint
            {
                Time = input.Time,
                Value = input.Low * (1 - _with * (input.High - input.Low) / (input.High + input.Low))
            });

            UpperBand.Update(new IndicatorDataPoint
            {
                Time = input.Time,
                Value = input.Low * (1 + _with * (input.High - input.Low) / (input.High + input.Low))
            });

            return MiddleBand;
        }

    }
}
