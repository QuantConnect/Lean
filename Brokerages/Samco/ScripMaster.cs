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
using CsvHelper.Configuration.Attributes;

namespace QuantConnect.Brokerages.Samco
{
    public class ScripMaster
    {
        [Name("exchange")]
        public string Exchange { get; set; }
        [Name("exchangeSegment")]
        public string ExchangeSegment { get; set; }
        [Name("symbolCode")]
        public string SymbolCode { get; set; }
        [Name("tradingSymbol")]
        public string TradingSymbol { get; set; }
        [Name("name")]
        public string Name { get; set; }
        [Name("lastPrice")]
        public decimal LastPrice { get; set; }
        [Name("instrument")]
        public string Instrument { get; set; }
        [Name("lotSize")]
        public string LotSize { get; set; }
        [Name("strikePrice")]
        public string StrikePrice { get; set; }
        [Name("expiryDate")]
        public string ExpiryDate { get; set; }
        [Name("tickSize")]
        public string TickSize { get; set; }
    }
}