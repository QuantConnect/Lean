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

using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace QuantConnect.Report
{
    /// <summary>
    /// Lightweight portfolio at a point in time
    /// </summary>
    public class PointInTimePortfolio
    {
        /// <summary>
        /// Time that this point in time portfolio is for
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// The total value of the portfolio. This is cash + absolute value of holdings
        /// </summary>
        public decimal TotalPortfolioValue { get; private set; }

        /// <summary>
        /// The cash the portfolio has
        /// </summary>
        public decimal Cash { get; private set; }

        /// <summary>
        /// The order we just processed
        /// </summary>
        [JsonIgnore]
        public Order Order { get; private set; }

        /// <summary>
        /// A list of holdings at the current moment in time
        /// </summary>
        public List<PointInTimeHolding> Holdings { get; private set; }

        /// <summary>
        /// Portfolio leverage - provided for convenience
        /// </summary>
        public decimal Leverage { get; private set; }

        /// <summary>
        /// Creates an instance of the PointInTimePortfolio object
        /// </summary>
        /// <param name="order">Order applied to the portfolio</param>
        /// <param name="portfolio">Algorithm portfolio at a point in time</param>
        public PointInTimePortfolio(Order order, SecurityPortfolioManager portfolio)
        {
            Time = order.Time;
            Order = order;
            TotalPortfolioValue = portfolio.TotalPortfolioValue;
            Cash = portfolio.Cash;
            Holdings = portfolio.Securities.Values.Select(x => new PointInTimeHolding(x.Symbol, x.Holdings.HoldingsValue, x.Holdings.Quantity)).ToList();
            Leverage = Holdings.Sum(x => x.AbsoluteHoldingsValue) / TotalPortfolioValue;
        }

        /// <summary>
        /// Clones the provided portfolio
        /// </summary>
        /// <param name="portfolio">Portfolio</param>
        /// <param name="time">Time</param>
        public PointInTimePortfolio(PointInTimePortfolio portfolio, DateTime time)
        {
            Time = time;
            Order = portfolio.Order;
            TotalPortfolioValue = portfolio.TotalPortfolioValue;
            Cash = portfolio.Cash;
            Holdings = portfolio.Holdings.Select(x => new PointInTimeHolding(x.Symbol, x.HoldingsValue, x.Quantity)).ToList();
            Leverage = portfolio.Leverage;
        }

        /// <summary>
        /// Filters out any empty holdings from the current <see cref="Holdings"/>
        /// </summary>
        /// <returns>Current object, but without empty holdings</returns>
        public PointInTimePortfolio NoEmptyHoldings()
        {
            Holdings = Holdings.Where(h => h.Quantity != 0).ToList();
            return this;
        }

        /// <summary>
        /// Holding of an asset at a point in time
        /// </summary>
        public class PointInTimeHolding
        {
            /// <summary>
            /// Symbol of the holding
            /// </summary>
            public Symbol Symbol { get; private set; }

            /// <summary>
            /// Value of the holdings of the asset. Can be negative if shorting an asset
            /// </summary>
            public decimal HoldingsValue { get; private set; }

            /// <summary>
            /// Quantity of the asset. Can be negative if shorting an asset
            /// </summary>
            public decimal Quantity { get; private set; }

            /// <summary>
            /// Absolute value of the holdings.
            /// </summary>
            [JsonIgnore]
            public decimal AbsoluteHoldingsValue => Math.Abs(HoldingsValue);

            /// <summary>
            /// Absolute value of the quantity
            /// </summary>
            [JsonIgnore]
            public decimal AbsoluteHoldingsQuantity => Math.Abs(Quantity);

            /// <summary>
            /// Creates an instance of PointInTimeHolding, representing a holding at a given point in time
            /// </summary>
            /// <param name="symbol">Symbol of the holding</param>
            /// <param name="holdingsValue">Value of the holding</param>
            /// <param name="holdingsQuantity">Quantity of the holding</param>
            public PointInTimeHolding(Symbol symbol, decimal holdingsValue, decimal holdingsQuantity)
            {
                Symbol = symbol;
                HoldingsValue = holdingsValue;
                Quantity = holdingsQuantity;
            }
        }
    }
}
