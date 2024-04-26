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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests the behavior of SetHoldings for futures, see GH issue 4027
    /// </summary>
    public class SetHoldingsFutureRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contractSymbol;
        private bool _invertedPosition;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);
            SetCash(1000000);

            var future = AddFuture(Futures.Indices.SP500EMini);

            // set our expiry filter for this futures chain
            future.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && !_invertedPosition)
            {
                foreach (var chain in slice.FutureChains)
                {
                    // find the front contract expiring no earlier than in 90 days
                    var contract = (
                        from futuresContract in chain.Value.OrderBy(x => x.Expiry)
                        where futuresContract.Expiry > Time.Date.AddDays(90)
                        select futuresContract
                    ).FirstOrDefault();

                    // if found, trade it
                    if (contract != null)
                    {
                        _contractSymbol = contract.Symbol;

                        try
                        {
                            SetHoldings(_contractSymbol, 1.1);
                            throw new Exception("We expect invalid target for futures to throw an exception");
                        }
                        catch (InvalidOperationException)
                        {
                            // expected
                        }

                        try
                        {
                            SetHoldings(_contractSymbol, -1.1);
                            throw new Exception("We expect invalid target for futures to throw an exception");
                        }
                        catch (InvalidOperationException)
                        {
                            // expected
                        }

                        SetHoldings(_contractSymbol, 1);
                    }
                }
            }
            else
            {
                if (!_invertedPosition)
                {
                    // lets reverse our position now
                    SetHoldings(_contractSymbol, -1);
                    _invertedPosition = true;
                }
                else
                {
                    Liquidate();
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled && Portfolio.Invested)
            {
                Log($"{orderEvent} - Portfolio.MarginRemaining {Portfolio.MarginRemaining}");

                if (Portfolio.TotalHoldingsValue / Portfolio.TotalPortfolioValue < 10)
                {
                    throw new Exception("Expected to be trading using the futures margin leverage");
                }

                var security = Securities[_contractSymbol];
                var model = security.BuyingPowerModel as FutureMarginModel;
                var marginUsed = model.MaintenanceOvernightMarginRequirement * security.Holdings.AbsoluteQuantity;

                if ((Portfolio.TotalMarginUsed - marginUsed) != 0)
                {
                    throw new Exception($"We expect TotalMarginUsed to be {marginUsed}, but was {Portfolio.TotalMarginUsed}");
                }

                var initialMarginRequired = model.InitialOvernightMarginRequirement * security.Holdings.AbsoluteQuantity;

                if (Portfolio.TotalPortfolioValue - initialMarginRequired > model.InitialOvernightMarginRequirement * security.SymbolProperties.LotSize)
                {
                    throw new Exception("We expect to be trading using the biggest position we can, there seems to be room for another contract");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 21665;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-1.34%"},
            {"Compounding Annual Return", "-97.000%"},
            {"Drawdown", "2.600%"},
            {"Expectancy", "-1"},
            {"Start Equity", "1000000"},
            {"End Equity", "974316.1"},
            {"Net Profit", "-2.568%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-66.775"},
            {"Tracking Error", "0.243"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2033.90"},
            {"Estimated Strategy Capacity", "$530000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "2690.71%"},
            {"OrderListHash", "f33db020caac94864efec448e79bce97"}
        };
    }
}
