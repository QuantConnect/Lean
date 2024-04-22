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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm tarding an equity Covered Call option strategy using a combo limit order
    /// </summary>
    public class CoveredCallComboLimitOrderAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _submittionTime;
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(200000);

            var equity = AddEquity("GOOG", leverage: 4);
            var option = AddOption(equity.Symbol);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-1, +1).Expiration(0, 30));
        }
        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && Transactions.OrdersCount == 0)
            {
                OptionChain chain;
                if (IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    // we find at the money (ATM) call contract with closest expiration
                    var atmContract = chain
                        .OrderBy(x => x.Expiry)
                        .Where(contract => contract.Right == OptionRight.Call && chain.Underlying.Price > contract.Strike - 10)
                        .OrderBy(x => x.Strike)
                        .First();

                    var optionPrice = Securities[atmContract.Symbol].AskPrice;
                    var underlyingPrice = Securities["GOOG"].AskPrice;

                    // covered call
                    var legs = new List<Leg> { Leg.Create(atmContract.Symbol, -1), Leg.Create(atmContract.Symbol.Underlying, 100) };

                    var comboPrice = underlyingPrice - optionPrice;
                    if(comboPrice < 734m)
                    {
                        // just to make sure the price makes sense
                        throw new Exception($"Unexpected combo price {comboPrice}");
                    }
                    // place order slightly bellow price
                    ComboLimitOrder(legs, 6, comboPrice - 0.5m);

                    _submittionTime = Time;
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(orderEvent.ToString());
            if (orderEvent.Status.IsFill() && (Time - _submittionTime) < TimeSpan.FromMinutes(10))
            {
                // we want to make sure we fill because the price moved and hit our limit price
                throw new Exception($"Unexpected fill time {Time} submittion time {_submittionTime}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 463141;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "200671.1"},
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
            {"Total Fees", "$6.90"},
            {"Estimated Strategy Capacity", "$8000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZFMEBBB2E|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "227.27%"},
            {"OrderListHash", "94a9ae926f68c23d06d32af2b5a25fea"}
        };
    }
}
