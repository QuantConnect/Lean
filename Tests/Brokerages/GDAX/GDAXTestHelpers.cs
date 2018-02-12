using QuantConnect.Brokerages.GDAX;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages.GDAX
{
    public class GDAXTestsHelpers
    {
        private const string accountCurrency = "USD";

        public static Security GetSecurity(decimal price = 1m, SecurityType securityType = SecurityType.Crypto)
        {
              return new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.Utc), CreateConfig(securityType), new Cash(accountCurrency, 1000, price, accountCurrency),
              new SymbolProperties("BTCUSD", accountCurrency, 1, 1, 0.01m));
        }

        private static SubscriptionDataConfig CreateConfig(SecurityType securityType = SecurityType.Crypto)
        {
                return new SubscriptionDataConfig(typeof(TradeBar), Symbol.Create("BTCUSD", securityType, Market.GDAX), Resolution.Minute, TimeZones.Utc, TimeZones.Utc,
                false, true, false);
        }



        public static void AddOrder(GDAXBrokerage unit, int id, string brokerId, decimal quantity)
        {
            var order = new Orders.MarketOrder { BrokerId = new List<string> { brokerId }, Quantity = quantity, Id = id };
            unit.CachedOrderIDs.TryAdd(1, order);
            unit.FillSplit.TryAdd(id, new GDAXFill(order));
        }

        public static WebSocketMessage GetArgs(string json)
        {
            return new WebSocketMessage(json);
        }
    }
}
