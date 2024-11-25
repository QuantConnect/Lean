using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class MesaAdaptiveMovingAverageTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new MesaAdaptiveMovingAverage("MAMA");
        }
        protected override string TestFileName => "spy_mama.csv";

        protected override string TestColumnName => "mama";
    }
}