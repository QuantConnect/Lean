using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    public class PremierStochasticOscillatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new PremierStochasticOscillator("PSO", 8, 5);
        }

        protected override string TestFileName => "spy_pso.csv";

        protected override string TestColumnName => "pso";

        protected override Action<IndicatorBase<IBaseDataBar>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double)((PremierStochasticOscillator)indicator).Pso.Current.Value, 1e-3);
    }
}
