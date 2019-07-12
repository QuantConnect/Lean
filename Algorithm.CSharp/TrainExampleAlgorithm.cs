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

using QuantConnect.Data;
using System;
using System.Collections.Generic;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example shows how we can execute a method that takes longer than Lean timeout limit
    /// This feature is useful for algorithms that train models
    /// </summary>
    /// <meta name="tag" content="using quantconnect" />
    public class TrainExampleAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private string _piString;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date

            AddEquity("SPY");

            Schedule.On(
                DateRules.EveryDay("SPY"),
                TimeRules.AfterMarketOpen("SPY", 10),
                () => {
                    Train(
                        () => _piString = CalculatePi(10000),
                        TimeSpan.FromSeconds(20),
                        () => { Debug($"{Time:O} :: Pi: {_piString}"); }
                    );
                }
            );
        }

        public static string CalculatePi(int digits)
        {
            // Credit to https://stackoverflow.com/a/11679007
            digits++;

            var pi = new uint[digits];
            var x = new uint[digits * 10 / 3 + 2];
            var r = new uint[digits * 10 / 3 + 2];

            for (var j = 0; j < x.Length; j++)
            {
                x[j] = 20;
            }

            for (var i = 0; i < digits; i++)
            {
                uint carry = 0;
                for (var j = 0; j < x.Length; j++)
                {
                    var num = (uint)(x.Length - j - 1);
                    var dem = num * 2 + 1;

                    x[j] += carry;

                    var q = x[j] / dem;
                    r[j] = x[j] % dem;

                    carry = q * num;
                }


                pi[i] = x[x.Length - 1] / 10;

                r[x.Length - 1] = x[x.Length - 1] % 10;

                for (var j = 0; j < x.Length; j++)
                {
                    x[j] = r[j] * 10;
                }
            }

            var result = "";

            uint c = 0;

            for (var i = pi.Length - 1; i >= 0; i--)
            {
                pi[i] += c;
                c = pi[i] / 10;

                result = (pi[i] % 10) + result;
            }

            return result;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (Portfolio.Invested) return;

            SetHoldings("SPY", 1);
        }


        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "263.153%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.663%"},
            {"Sharpe Ratio", "4.41"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.007"},
            {"Beta", "76.118"},
            {"Annual Standard Deviation", "0.192"},
            {"Annual Variance", "0.037"},
            {"Information Ratio", "4.354"},
            {"Tracking Error", "0.192"},
            {"Treynor Ratio", "0.011"},
            {"Total Fees", "$3.26"}
        };
    }
}