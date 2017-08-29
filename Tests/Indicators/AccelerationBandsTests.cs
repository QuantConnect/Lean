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
            return new AccelerationBands(20, 4m);
        }

        [Test]
        public void ComparesWithExternalDataUpperBand()
        {
            var abands = CreateIndicator();
            TestHelper.TestIndicator(
                abands,
                "spy_acceleration_bands_20_4.txt",
                "UpperBand",
                (ind, expected) => Assert.AreEqual(expected, (double)((AccelerationBands)ind).UpperBand.Current.Value, 1e-4, "Upper band test fail.")
            );
        }

        [Test]
        public void ComparesWithExternalDataLowerBand()
        {
            var abands = CreateIndicator();
            TestHelper.TestIndicator(
                abands,
                "spy_acceleration_bands_20_4.txt",
                "LowerBand",
                (ind, expected) => Assert.AreEqual(expected, (double)((AccelerationBands)ind).LowerBand.Current.Value, 1e-4, "Lower band test fail.")
            );
        }

        protected override string TestFileName
        {
            get { return "spy_acceleration_bands_20_4.txt"; }
        }

        protected override string TestColumnName { get { return "MiddleBand"; } }
    }

    /// <summary>
    /// The Acceleration Bands created by Price Headley plots upper and lower envelope bands around a moving average. 
    /// </summary>
    /// <seealso cref="QuantConnect.Indicators.IndicatorBase{QuantConnect.Data.Market.TradeBar}" />
    public class AccelerationBands : IndicatorBase<TradeBar>
    {
        private decimal _width;

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
        /// <param name="name">The name of this indicator.</param>
        /// <param name="period">The period of the three moving average (middle, upper and lower band).</param>
        /// <param name="width">A coefficient specifying the distance between the middle band and upper or lower bands.</param>
        /// <param name="movingAverageType">Type of the moving average .</param>
        public AccelerationBands(string name, int period, decimal width,
            MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            _width = width;
            MovingAverageType = movingAverageType;
            MiddleBand = movingAverageType.AsIndicator(name + "_MiddleBand", period);
            LowerBand = movingAverageType.AsIndicator(name + "_LowerBand", period);
            UpperBand = movingAverageType.AsIndicator(name + "_UpperBand", period);

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccelerationBands"/> class.
        /// </summary>
        /// <param name="period">The period of the three moving average (middle, upper and lower band).</param>
        /// <param name="width">A coefficient specifying the distance between the middle band and upper or lower bands.</param>
        public AccelerationBands(int period, decimal width)
            : this(string.Format("ABANDS({0},{1})", period, width), period, width)
        { }

        public AccelerationBands(int period)
            : this(string.Format("ABANDS({0},{1})", period, 2), period, 2)
        { }

        public override bool IsReady => MiddleBand.IsReady && LowerBand.IsReady && UpperBand.IsReady;

        public override void Reset()
        {
            base.Reset();
            MiddleBand.Reset();
            LowerBand.Reset();
            UpperBand.Reset();
        }

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
                Value = input.Low * (1 - _width * (input.High - input.Low) / (input.High + input.Low))
            });

            UpperBand.Update(new IndicatorDataPoint
            {
                Time = input.Time,
                Value = input.High * (1 + _width * (input.High - input.Low) / (input.High + input.Low))
            });

            return MiddleBand;
        }

    }
}
