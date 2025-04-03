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
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that options are automatically exercised on expiry regardless on whether
    /// the day after expiration is tradable or not.
    /// This specific algorithm works with contracts added manually.
    /// </summary>
    public class OptionExerciseOnExpiryAndNonTradableDateRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spxOption1;
        private Symbol _spxOption2;
        private bool _tradedOptions;
        private bool _exercisedOption1;
        private bool _exercisedOption2;

        public override void Initialize()
        {
            SetStartDate(2023, 6, 25);
            SetEndDate(2023, 7, 10);

            var spx = AddIndex("SPX").Symbol;

            _spxOption1 = QuantConnect.Symbol.CreateOption(
                spx,
                "SPXW",
                Market.USA,
                OptionStyle.European,
                OptionRight.Call,
                4445m,
                // Next day is tradable
                new DateTime(2023, 6, 30));

            _spxOption2 = QuantConnect.Symbol.CreateOption(
                spx,
                "SPXW",
                Market.USA,
                OptionStyle.European,
                OptionRight.Call,
                4445m,
                // Next day is a holiday
                new DateTime(2023, 7, 3));

            InitializeOptions(spx, [_spxOption1, _spxOption2]);
        }

        protected virtual void InitializeOptions(Symbol underlying, Symbol[] options)
        {
            AddIndexOptionContract(_spxOption1);
            AddIndexOptionContract(_spxOption2);
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && !_tradedOptions)
            {
                Buy(_spxOption1, 1);
                Buy(_spxOption2, 1);
                _tradedOptions = true;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
            if (Transactions.GetOrderById(orderEvent.OrderId) is OptionExerciseOrder order)
            {
                _exercisedOption1 |= order.Symbol == _spxOption1;
                _exercisedOption2 |= order.Symbol == _spxOption2;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_exercisedOption1 || !_exercisedOption2)
            {
                throw new RegressionTestException("Expected both options to be exercised");
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
        public virtual long DataPoints => 16638;

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
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "4"},
            {"Average Win", "0.58%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "31.165%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101172"},
            {"Net Profit", "1.172%"},
            {"Sharpe Ratio", "4.049"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "94.902%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.041"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "5.34"},
            {"Tracking Error", "0.041"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$8000.00"},
            {"Lowest Capacity Asset", "SPXW Y9T7LPL1X0TQ|SPX 31"},
            {"Portfolio Turnover", "0.02%"},
            {"OrderListHash", "764432f8c2753cb2d5120a98997da47a"}
        };
    }
}
