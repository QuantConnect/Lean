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
using System.Globalization;
using System.IO;

namespace QuantConnect.Data.Custom.BrainData
{
    /// <summary>
    /// Brain Data Machine Learning Alpha Ranking provides rankings for stocks
    /// </summary>
    public class BrainDataRankingsTenDay : BaseData
    {
        /// <summary>
        /// Ranking data/Machine Learning Alpha score average over period of five days
        /// </summary>
        public decimal RankingScore { get; set; }

        /// <summary>
        /// Empty constructor required for <see cref="Slice.Get{T}()"/>
        /// </summary>
        public BrainDataRankingsTenDay()
        {
        }

        /// <summary>
        /// Gets the source of the data and directs LEAN where to look for it
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="date">Algorithm date</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>Location of the data</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "braindata",
                    "rankings_ten_day",
                    $"{config.Symbol.Value.ToLower()}.csv"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv
            );
        }

        /// <summary>
        /// Reads and creates a <see cref="BaseData"/> instance that loads the data into LEAN
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="line">Line of data</param>
        /// <param name="date">Date of the algorithm</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>New BaseData instance</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var csv = line.ToCsv(size: 3);

            return new BrainDataRankingsTenDay
            {
                Symbol = config.Symbol,
                Time = DateTime.ParseExact(csv[0], "yyyyMMdd HH:mm", CultureInfo.InvariantCulture),
                RankingScore = Convert.ToDecimal(csv[1], CultureInfo.InvariantCulture),
            };
        }

        /// <summary>
        /// Clones the object and creates a new instance
        /// </summary>
        /// <returns>A clone of the object</returns>
        public override BaseData Clone()
        {
            return new BrainDataRankingsTenDay
            {
                Symbol = Symbol,
                Time = Time,
                RankingScore = RankingScore
            };
        }
    }
}
