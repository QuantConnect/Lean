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
 *
*/

using System;
using NodaTime;
using System.IO;
using Newtonsoft.Json;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Data.Custom.AlphaStreams
{
    /// <summary>
    /// Snapshot of an algorithms portfolio state
    /// </summary>
    public class AlphaStreamsPortfolioState : BaseData
    {
        /// <summary>
        /// The deployed alpha id. This is the id generated upon submission to the alpha marketplace
        /// </summary>
        [JsonProperty("alphaId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AlphaId { get; set; }

        /// <summary>
        /// The algorithm's unique deploy identifier
        /// </summary>
        [JsonProperty("algorithmId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AlgorithmId { get; set; }

        /// <summary>
        /// The source of this data point, 'live trading' or in sample
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Portfolio state id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Algorithms account currency
        /// </summary>
        public string AccountCurrency { get; set; }

        /// <summary>
        /// The current total portfolio value
        /// </summary>
        public decimal TotalPortfolioValue { get; set; }

        /// <summary>
        /// The margin used
        /// </summary>
        public decimal TotalMarginUsed { get; set; }

        /// <summary>
        /// The different positions groups
        /// </summary>
        [JsonProperty("positionGroups", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<PositionGroupState> PositionGroups { get; set; }

        /// <summary>
        /// Gets the cash book that keeps track of all currency holdings (only settled cash)
        /// </summary>
        [JsonProperty("cashBook", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, Cash> CashBook { get; set; }

        /// <summary>
        /// Gets the cash book that keeps track of all currency holdings (only unsettled cash)
        /// </summary>
        [JsonProperty("unsettledCashBook", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, Cash> UnsettledCashBook { get; set; }

        /// <summary>
        /// Return the Subscription Data Source
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Subscription Data Source.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var source = Path.Combine(
                Globals.DataFolder,
                "alternative",
                "alphastreams",
                "portfoliostate",
                config.Symbol.Value.ToLowerInvariant(),
                $"{date:yyyyMMdd}.json"
            );
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Content of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>New data point object</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var dataPoint = JsonConvert.DeserializeObject<AlphaStreamsPortfolioState>(line);
            dataPoint.Symbol = config.Symbol;
            return dataPoint;
        }

        /// <summary>
        /// Specifies the data time zone for this data type
        /// </summary>
        /// <remarks>Will throw <see cref="InvalidOperationException"/> for security types
        /// other than <see cref="SecurityType.Base"/></remarks>
        /// <returns>The <see cref="DateTimeZone"/> of this data type</returns>
        public override DateTimeZone DataTimeZone()
        {
            return DateTimeZone.Utc;
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        public override BaseData Clone()
        {
            return new AlphaStreamsPortfolioState
            {
                Id = Id,
                Time = Time,
                Source = Source,
                Symbol = Symbol,
                AlphaId = AlphaId,
                DataType = DataType,
                CashBook = CashBook,
                AlgorithmId = AlgorithmId,
                PositionGroups = PositionGroups,
                TotalMarginUsed = TotalMarginUsed,
                AccountCurrency = AccountCurrency,
                UnsettledCashBook = UnsettledCashBook,
                TotalPortfolioValue = TotalPortfolioValue,
            };
        }

        /// <summary>
        /// Indicates that the data set is expected to be sparse
        /// </summary>
        public override bool IsSparseData()
        {
            return true;
        }
    }

    /// <summary>
    /// Snapshot of a position group state
    /// </summary>
    public class PositionGroupState
    {
        /// <summary>
        /// Currently margin used
        /// </summary>
        public decimal MarginUsed { get; set; }

        /// <summary>
        /// The margin used by this position in relation to the total portfolio value
        /// </summary>
        public decimal PortfolioValuePercentage { get; set; }

        /// <summary>
        /// THe positions which compose this group
        /// </summary>
        public List<PositionState> Positions { get; set; }
    }

    /// <summary>
    /// Snapshot of a position state
    /// </summary>
    public class PositionState : IPosition
    {
        /// <summary>
        /// The symbol
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// The quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// The unit quantity. The unit quantities of a group define the group. For example, a covered
        /// call has 100 units of stock and -1 units of call contracts.
        /// </summary>
        public decimal UnitQuantity { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public static PositionState Create(IPosition position)
        {
            return new PositionState
            {
                Symbol = position.Symbol,
                Quantity = position.Quantity,
                UnitQuantity = position.UnitQuantity
            };
        }
    }
}
