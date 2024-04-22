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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of specifying a null position group allowing us to fill orders which would be invalid if not
    /// </summary>
    public class NullMarginMultipleOrdersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _placedTrades;
        protected Symbol OptionSymbol { get; private set; }

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(10000);

            OverrideMarginModels();

            var equity = AddEquity("GOOG", leverage: 4);
            var option = AddOption(equity.Symbol);
            OptionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-2, +2).Expiration(0, 180));
        }

        protected virtual void OverrideMarginModels()
        {
            Portfolio.SetPositions(SecurityPositionGroupModel.Null);
            SetSecurityInitializer(security =>
            {
                security.SetBuyingPowerModel(new ConstantBuyingPowerModel(1));
            });
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                OptionChain chain;
                if (IsMarketOpen(OptionSymbol) && slice.OptionChains.TryGetValue(OptionSymbol, out chain))
                {
                    // we find at the money (ATM) call contract with farthest expiration
                    var atmContracts = chain
                        .Where(contract => contract.Right == OptionRight.Call)
                        .OrderByDescending(x => x.Expiry)
                        .ThenBy(x => x.Strike)
                        .First();

                    if(!_placedTrades)
                    {
                        _placedTrades = true;
                        PlaceTrades(atmContracts);
                    }
                }
            }
        }

        protected virtual void PlaceTrades(OptionContract optionContract)
        {
            AssertState(MarketOrder(optionContract.Symbol.Underlying, 1000), 1, 1000);
            AssertState(MarketOrder(optionContract.Symbol, -10), 2, 1010);
        }

        protected virtual void AssertState(OrderTicket ticket, int expectedGroupCount, int expectedMarginUsed)
        {
            if (ticket.Status != OrderStatus.Filled)
            {
                throw new Exception($"Unexpected order status {ticket.Status} for symbol {ticket.Symbol} and quantity {ticket.Quantity}");
            }
            if (Portfolio.Positions.Groups.Count != expectedGroupCount)
            {
                throw new Exception($"Unexpected position group count {Portfolio.Positions.Groups.Count} for symbol {ticket.Symbol} and quantity {ticket.Quantity}");
            }
            if(Portfolio.TotalMarginUsed != expectedMarginUsed)
            {
                throw new Exception($"Unexpected margin used {expectedMarginUsed}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 471135;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "10000"},
            {"End Equity", "10658.5"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$11.50"},
            {"Estimated Strategy Capacity", "$8800000.00"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "7580.62%"},
            {"OrderListHash", "ea13456d0c97785f9f2fc12842831990"}
        };
    }
}
