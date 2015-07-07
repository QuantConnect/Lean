using System;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Brokerages.Oanda;
using QuantConnect.Brokerages.Oanda.DataType.Communications;
using QuantConnect.Brokerages.Oanda.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using Environment = QuantConnect.Brokerages.Oanda.Environment;

namespace QuantConnect.Tests.Brokerages.Oanda
{
    [TestFixture]
    public class OandaBrokerageTests : BrokerageTests
    {
        /// <summary>
        ///     Creates the brokerage under test and connects it
        /// </summary>
        /// <returns>A connected brokerage instance</returns>
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, IHoldingsProvider holdingsProvider)
        {
            var oandaBrokerage = new OandaBrokerage(orderProvider, holdingsProvider, 0);
            var tokens = OandaBrokerageFactory.GetTokens();

            var requestString = EndpointResolver.ResolveEndpoint(Environment.Sandbox, Server.Account) + "accounts";
            using (var task = oandaBrokerage.MakeRequestAsync<AccountResponse>(requestString, "POST"))
            {
                task.Wait();
                var accountResponse = task.Result;
                oandaBrokerage.SetAccountId(accountResponse.accountId);
                oandaBrokerage.SetEnvironment("sandbox");
            }

            var qcUserId = OandaBrokerageFactory.Configuration.QuantConnectUserId;
            oandaBrokerage.SetTokens(qcUserId, tokens.AccessToken, tokens.RefreshToken, tokens.IssuedAt,
                TimeSpan.FromSeconds(tokens.ExpiresIn));
            
            // keep the tokens up to date in the event of a refresh
            oandaBrokerage.SessionRefreshed +=
                (sender, args) =>
                {
                    File.WriteAllText(OandaBrokerageFactory.TokensFile,
                        JsonConvert.SerializeObject(args, Formatting.Indented));
                };

            return oandaBrokerage;
        }

        /// <summary>
        ///     Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override string Symbol
        {
            get { return "EUR_USD"; }
        }

        /// <summary>
        ///     Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType
        {
            get { return SecurityType.Forex; }
        }

        /// <summary>
        ///     Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        protected override decimal HighPrice
        {
            get { return 1000m; }
        }

        /// <summary>
        ///     Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice
        {
            get { return 0.01m; }
        }

        /// <summary>
        ///     Gets the current market price of the specified security
        /// </summary>
        protected override decimal GetAskPrice(string symbol, SecurityType securityType)
        {
            var oanda = (OandaBrokerage) Brokerage;
            return new decimal(0.0);
        }
    }
}
