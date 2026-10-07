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

using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the proceeds of the automatic delisting liquidation of an equity held
    /// on a cash account settle, the delisted security is kept until its funds settle and then removed
    /// </summary>
    public class DelistedEquityCashAccountSettlementRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aaa;
        private bool _delisted;

        /// <summary>
        /// Whether to sell the position on the delisting warning instead of holding it through the delisting
        /// </summary>
        protected virtual bool LiquidateOnDelistingWarning => false;

        public override void Initialize()
        {
            SetStartDate(2007, 05, 15);
            SetEndDate(2007, 05, 25);
            SetCash(100000);
            SetBrokerageModel(BrokerageName.Default, AccountType.Cash);

            _aaa = AddEquity("AAA.1", Resolution.Daily).Symbol;
            AddEquity("SPY", Resolution.Daily);
        }

        public override void OnData(Slice slice)
        {
            if (Transactions.OrdersCount == 0)
            {
                SetHoldings(_aaa, 0.5);
            }

            foreach (var delisting in slice.Delistings.Values)
            {
                if (delisting.Type == DelistingType.Warning)
                {
                    if (LiquidateOnDelistingWarning)
                    {
                        Liquidate(_aaa);
                    }
                }
                else
                {
                    _delisted = true;
                    if (Portfolio.UnsettledCash == 0)
                    {
                        throw new RegressionTestException("Expected the sale proceeds to be unsettled when the security is delisted");
                    }
                    if (!Securities.Values.Contains(Securities[_aaa]))
                    {
                        throw new RegressionTestException("Expected the delisted security to be kept until its funds settle");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_delisted)
            {
                throw new RegressionTestException("Expected the security to be delisted");
            }
            if (Portfolio.Invested)
            {
                throw new RegressionTestException("Expected the delisted position to be liquidated");
            }
            if (Portfolio.UnsettledCash != 0)
            {
                throw new RegressionTestException($"Expected the sale proceeds to be settled, but {Portfolio.UnsettledCash} is still unsettled");
            }
            if (Securities.Values.Contains(Securities[_aaa]))
            {
                throw new RegressionTestException("Expected the delisted security to be removed once its funds settled");
            }
            if (UniverseManager.ActiveSecurities.ContainsKey(_aaa))
            {
                throw new RegressionTestException("Expected the delisted security to be removed from its universes once its funds settled");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 87;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-2.24%"},
            {"Compounding Annual Return", "-54.066%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "97752.12"},
            {"Net Profit", "-2.248%"},
            {"Sharpe Ratio", "-8.493"},
            {"Sortino Ratio", "-25.704"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.411"},
            {"Beta", "-0.391"},
            {"Annual Standard Deviation", "0.059"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-5.95"},
            {"Tracking Error", "0.121"},
            {"Treynor Ratio", "1.272"},
            {"Total Fees", "$9.18"},
            {"Estimated Strategy Capacity", "$83000.00"},
            {"Lowest Capacity Asset", "AAA SEVKGI6HF885"},
            {"Portfolio Turnover", "9.02%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "a9e576cc3349a56bd16be89564d0ce05"}
        };
    }
}
