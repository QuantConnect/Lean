using System;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture]
    public class OptionSecurityTests
    {
        [Test]
        public void FutureOptionSecurityUsesFutureOptionMarginModel()
        {
            var underlyingFuture = Symbol.CreateFuture(
                QuantConnect.Securities.Futures.Indices.SP500EMini,
                Market.CME,
                new DateTime(2021, 3, 19));

            var futureOption = Symbol.CreateOption(underlyingFuture,
                Market.CME,
                OptionStyle.American,
                OptionRight.Call,
                2550m,
                new DateTime(2021, 3, 19));

            var futureOptionSecurity = new Option(
                futureOption,
                MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.CME, futureOption, futureOption.SecurityType),
                new Cash("USD", 100000m, 1m),
                new OptionSymbolProperties(string.Empty, "USD", 1m, 0.01m, 1m),
                new CashBook(),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache());

            Assert.IsTrue(futureOptionSecurity.BuyingPowerModel is FutureOptionMarginModel);
        }

        [Test]
        public void EquityOptionSecurityUsesOptionMarginModel()
        {
            var underlyingEquity = Symbol.Create("TWX", SecurityType.Equity, Market.USA);
            var equityOption = Symbol.CreateOption(underlyingEquity,
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                42.5m,
                new DateTime(2014, 6, 21));

            var equityOptionSecurity = new Option(
                equityOption,
                MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.CME, equityOption, equityOption.SecurityType),
                new Cash("USD", 100000m, 1m),
                new OptionSymbolProperties(string.Empty, "USD", 100m, 0.0001m, 1m),
                new CashBook(),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache());

            Assert.IsTrue(equityOptionSecurity.BuyingPowerModel is OptionMarginModel);
        }
    }
}
