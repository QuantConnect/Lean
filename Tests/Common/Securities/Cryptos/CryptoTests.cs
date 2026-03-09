using NodaTime;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using System;

namespace QuantConnect.Tests.Common.Securities.Cryptos
{
    [TestFixture]
    public class CryptoTests
    {
        private static TimeKeeper TimeKeeper
        {
            get { return new TimeKeeper(DateTime.Now, new[] { TimeZones.NewYork }); }
        }

        [Test]
        [TestCase("BTCUSD", "USD")]
        [TestCase("BTCEUR", "EUR")]
        [TestCase("ETHBTC", "BTC")]
        [TestCase("ETHUSDT", "USDT")]
        public void ConstructorParseBaseCurrencyBySymbolProps(string ticker, string quote)
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions, new AlgorithmSettings());
            if (portfolio.CashBook.ContainsKey(quote))
            {
                portfolio.CashBook[quote].SetAmount(1000);
            }
            else
            {
                portfolio.CashBook.Add(quote, 0, 1000);
            }
            var cash = portfolio.CashBook[quote];
            var symbol = Symbol.Create(ticker, SecurityType.Crypto, Market.Coinbase);

            var crypto = new Crypto(
                symbol,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                new Cash(ticker.StartsWith("BTC", StringComparison.InvariantCultureIgnoreCase) ? "BTC" : "ETH", 0, 0),
                SymbolProperties.GetDefault(quote),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            Assert.AreEqual(symbol.Value.RemoveFromEnd(quote), crypto.BaseCurrency.Symbol);
        }
    }
}
