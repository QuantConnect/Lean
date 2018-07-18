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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    public class OptionDelistedDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "GOOG";
        public readonly Symbol OptionSymbol = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Option, Market.USA);
        private readonly List<Symbol> _delisted = new List<Symbol>();
        private readonly List<Symbol> _toBeDelisted = new List<Symbol>();
        private bool _executed;
        public override void Initialize()
        {
            SetStartDate(2016, 01, 15);  //Set Start Date
            SetEndDate(2016, 01, 19);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            AddOption(UnderlyingTicker);
        }

        public override void OnData(Slice data)
        {
            _executed = true;
            if (Time.Minute == 0)
            {
                foreach (var d in data)
                {
                    if (_delisted.Contains(d.Key))
                    {
                        throw new Exception("We shouldn't be recieving data from an already delisted symbol");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_executed)
            {
                Debug("CUSTOM OnEndOfAlgorithm");
                if (_delisted.Count != 20)
                {
                    throw new Exception("Expecting exactly 20 delisted events");
                }
                if (_toBeDelisted.Count != 20)
                {
                    throw new Exception("Expecting exactly 20 to be delisted warning events");
                }
            }
            base.OnEndOfAlgorithm();
        }

        public void OnData(Delistings data)
        {
            foreach (var kvp in data)
            {
                var symbol = kvp.Key;
                var delisting = kvp.Value;
                if (delisting.Type == DelistingType.Warning)
                {
                    _toBeDelisted.Add(symbol);
                    Debug($"OnData(Delistings): {Time}: {symbol} will be delisted at end of day today.");
                }
                if (delisting.Type == DelistingType.Delisted)
                {
                    _delisted.Add(symbol);
                    Debug($"OnData(Delistings): {Time}: {symbol} has been delisted.");
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
            {"Total Fees", "$0.00"}
        };
    }
}