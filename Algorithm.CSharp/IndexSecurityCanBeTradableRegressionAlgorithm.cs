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

using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test we can manually set index securities to be tradable without breaking
    /// SignalExportManager
    /// </summary>
    public class IndexSecurityCanBeTradableRegressionAlgorithm: QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private SignalExportManagerTest _signalExportManagerTest;
        private Symbol _equity;
        private Symbol _index;

        public virtual bool IsTradable { get; set; } = true;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 7);

            _index = AddIndex("SPX").Symbol;
            _equity = AddEquity("SPY").Symbol;
            SignalExport.AutomaticExportTimeSpan = null;
            _signalExportManagerTest = new SignalExportManagerTest(this);
            Securities[_index].IsTradable = IsTradable;
        }

        public override void OnData(Slice slice)
        {
            if (IsTradable != Securities[_index].IsTradable)
            {
                throw new RegressionTestException($"Index.IsTradable should be {IsTradable}, but was {Securities[_index].IsTradable}");
            }

            _signalExportManagerTest.GetPortfolioTargetsFromPortfolio(out PortfolioTarget[] targets);
            if (IsTradable)
            {
                if (!targets.Where(x => x.Symbol.SecurityType == SecurityType.Index).Any())
                {
                    throw new RegressionTestException($"Index {_index} is marked as tradable security, but no portfolio target with index security type was created");
                }
            }
            else
            {
                if (targets.Where(x => x.Symbol.SecurityType == SecurityType.Index).Any())
                {
                    throw new RegressionTestException($"Index is not a tradable security, so no portfolio target with index security type should have been created");
                }
            }

            if (!Portfolio.Invested)
            {
                SetHoldings(_equity, 1);
                RemoveSecurity(_index);

                AssertIndexIsNotTradable();

                AddSecurity(_index);
                IsTradable = false;
            }

            AssertIndexIsNotTradable();
        }

        private void AssertIndexIsNotTradable()
        {
            if (Securities[_index].IsTradable)
            {
                throw new RegressionTestException($"Index {_index} has already been removed and should be tradable no more");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 796;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99978.71"},
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
            {"Total Fees", "$3.44"},
            {"Estimated Strategy Capacity", "$56000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "99.63%"},
            {"OrderListHash", "3da9fa60bf95b9ed148b95e02e0cfc9e"}
        };

        private class SignalExportManagerTest: SignalExportManager
        {
            public SignalExportManagerTest(IAlgorithm algorithm) : base(algorithm)
            {
            }

            public void GetPortfolioTargetsFromPortfolio(out PortfolioTarget[] portfolioTargets)
            {
                base.GetPortfolioTargets(out portfolioTargets);
            }
        }
    }
}
