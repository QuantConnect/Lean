using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Diagnostics.Eventing.Reader;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class MassIndexTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new MassIndex();
        }

        protected override string TestFileName => "spy_mass_index_25.txt";
        protected override string TestColumnName => "MassIndex";
    }
}