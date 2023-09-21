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
using System.IO;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Lean fundamental data class
    /// </summary>
    public class Fundamental : FineFundamental
    {
        /// <summary>
        /// Gets the day's dollar volume for this symbol
        /// </summary>
        public override double DollarVolume => FundamentalService.Get<double>(Time, Symbol.ID, "DollarVolume");

        /// <summary>
        /// Gets the day's total volume
        /// </summary>
        public override long Volume => FundamentalService.Get<long>(Time, Symbol.ID, "Volume");

        /// <summary>
        /// Returns whether the symbol has fundamental data for the given date
        /// </summary>
        public override bool HasFundamentalData => FundamentalService.Get<bool>(Time, Symbol.ID, "HasFundamentalData");

        /// <summary>
        /// Gets the price factor for the given date
        /// </summary>
        public override decimal PriceFactor => FundamentalService.Get<decimal>(Time, Symbol.ID, "PriceFactor");

        /// <summary>
        /// Gets the split factor for the given date
        /// </summary>
        public override decimal SplitFactor => FundamentalService.Get<decimal>(Time, Symbol.ID, "SplitFactor");

        /// <summary>
        /// Gets the raw price
        /// </summary>
        public override decimal Value => FundamentalService.Get<decimal>(Time, Symbol.ID, "Value");

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public Fundamental()
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="time">The current time</param>
        /// <param name="symbol">The associated symbol</param>
        public Fundamental(DateTime time, Symbol symbol)
            : base(time, symbol)
        {
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var path = Path.Combine(Globals.DataFolder, "equity", config.Market, "fundamental", "coarse", $"{date:yyyyMMdd}.csv");
            return new SubscriptionDataSource(path, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="line"></param>
        /// <param name="date"></param>
        /// <param name="isLiveMode"></param>
        /// <returns></returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            try
            {
                var csv = line.Split(',');
                var symbol = new Symbol(SecurityIdentifier.Parse(csv[0]), csv[1]);
                return new Fundamental(date, symbol);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override BaseData Clone()
        {
            return new Fundamental(Time, Symbol);
        }

        /// <summary>
        /// Creates the symbol used for coarse fundamental data
        /// </summary>
        /// <param name="market">The market</param>
        /// <returns>A coarse universe symbol for the specified market</returns>
        public static Symbol CreateUniverseSymbol(string market)
        {
            market = market.ToLowerInvariant();
            var ticker = $"qc-universe-fundamental-{market}-{Guid.NewGuid()}";
            var sid = SecurityIdentifier.GenerateEquity(SecurityIdentifier.DefaultDate, ticker, market);
            return new Symbol(sid, ticker);
        }
    }
}
