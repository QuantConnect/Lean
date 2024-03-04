/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages
{
    public class TestsHelpers
    {
        public static Security GetSecurity(decimal price = 1m, SecurityType securityType = SecurityType.Crypto, Resolution resolution = Resolution.Minute, string symbol = "BTCUSD", string market = Market.Coinbase, string quoteCurrency = "USD")
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                CreateConfig(symbol, market, securityType, resolution),
                new Cash(quoteCurrency, 1000, price),
                SymbolPropertiesDatabase.FromDataFolder().GetSymbolProperties(market, symbol, SecurityType.Crypto, quoteCurrency),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private static SubscriptionDataConfig CreateConfig(string symbol, string market, SecurityType securityType = SecurityType.Crypto, Resolution resolution = Resolution.Minute)
        {
            Symbol actualSymbol;
            switch (securityType)
            {
                case SecurityType.FutureOption:
                    actualSymbol = Symbols.CreateFutureOptionSymbol(Symbols.CreateFutureSymbol(symbol, new DateTime(2020, 4, 28)), OptionRight.Call,
                        1000, new DateTime(2020, 3, 26));
                    break;

                case SecurityType.Option:
                case SecurityType.IndexOption:
                    actualSymbol = Symbols.CreateOptionSymbol(symbol, OptionRight.Call, 1000, new DateTime(2020, 3, 26));
                    break;

                default:
                    actualSymbol = Symbol.Create(symbol, securityType, market);
                    break;
            }

            return new SubscriptionDataConfig(typeof(TradeBar), actualSymbol, resolution, TimeZones.Utc, TimeZones.Utc, false, true, false);
        }

        public static HistoryRequest GetHistoryRequest(Symbol symbol, DateTime startDateTime, DateTime endDateTime, Resolution resolution, TickType tickType)
        {
            if (startDateTime > endDateTime)
            {
                throw new ArgumentException("The startDateTime is greater then endDateTime");
            }

            var dataType = LeanData.GetDataType(resolution, tickType);

            return new HistoryRequest(
                startDateTime,
                endDateTime,
                dataType,
                symbol,
                resolution,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                TimeZones.NewYork,
                null,
                false,
                false,
                DataNormalizationMode.Raw,
                tickType
                );
        }
    }
}
