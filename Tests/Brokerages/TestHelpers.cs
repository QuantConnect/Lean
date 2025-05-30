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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Tests.Brokerages
{
    public class TestsHelpers
    {
        public static Security GetSecurity(decimal price = 1m, SecurityType securityType = SecurityType.Crypto, Resolution resolution = Resolution.Minute, string symbol = "BTCUSD", string market = Market.Coinbase, string quoteCurrency = "USD", bool marketAlwaysOpen = true)
        {
            var config = CreateConfig(symbol, market, securityType, resolution);
            var marketHours = marketAlwaysOpen
                ? SecurityExchangeHours.AlwaysOpen(TimeZones.Utc)
                : MarketHoursDatabase.FromDataFolder().GetExchangeHours(config);

            return new Security(
                marketHours,
                config,
                new Cash(quoteCurrency, 1000, price),
                #pragma warning disable CS0618
                SymbolPropertiesDatabase.FromDataFolder().GetSymbolProperties(market, config.Symbol, securityType, quoteCurrency),
                #pragma warning restore CS0618
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

        public static HistoryRequest GetHistoryRequest(Symbol symbol, DateTime startDateTime, DateTime endDateTime, Resolution resolution, TickType tickType, DateTimeZone dateTimeZone = null)
        {
            if (startDateTime > endDateTime)
            {
                throw new ArgumentException("The startDateTime is greater then endDateTime");
            }

            if (dateTimeZone == null)
            {
                dateTimeZone = TimeZones.NewYork;
            }

            var dataType = LeanData.GetDataType(resolution, tickType);

            return new HistoryRequest(
                startDateTime,
                endDateTime,
                dataType,
                symbol,
                resolution,
                SecurityExchangeHours.AlwaysOpen(dateTimeZone),
                dateTimeZone,
                null,
                false,
                false,
                DataNormalizationMode.Raw,
                tickType
                );
        }

        public static IEnumerable<DateTimeZone> GetTimeZones()
        {
            yield return TimeZones.NewYork;
            yield return TimeZones.EasternStandard;
            yield return TimeZones.London;
            yield return TimeZones.HongKong;
            yield return TimeZones.Tokyo;
            yield return TimeZones.Rome;
            yield return TimeZones.Sydney;
            yield return TimeZones.Vancouver;
            yield return TimeZones.Toronto;
            yield return TimeZones.Chicago;
            yield return TimeZones.LosAngeles;
            yield return TimeZones.Phoenix;
            yield return TimeZones.Auckland;
            yield return TimeZones.Moscow;
            yield return TimeZones.Madrid;
            yield return TimeZones.BuenosAires;
            yield return TimeZones.Brisbane;
            yield return TimeZones.SaoPaulo;
            yield return TimeZones.Cairo;
            yield return TimeZones.Johannesburg;
            yield return TimeZones.Anchorage;
            yield return TimeZones.Denver;
            yield return TimeZones.Detroit;
            yield return TimeZones.MexicoCity;
            yield return TimeZones.Jerusalem;
            yield return TimeZones.Shanghai;
            yield return TimeZones.Melbourne;
            yield return TimeZones.Amsterdam;
            yield return TimeZones.Athens;
            yield return TimeZones.Berlin;
            yield return TimeZones.Bucharest;
            yield return TimeZones.Dublin;
            yield return TimeZones.Helsinki;
            yield return TimeZones.Istanbul;
            yield return TimeZones.Minsk;
            yield return TimeZones.Paris;
            yield return TimeZones.Zurich;
            yield return TimeZones.Honolulu;
            yield return TimeZones.Kolkata;
        }
    }
}
