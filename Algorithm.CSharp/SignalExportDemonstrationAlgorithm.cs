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

using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm sends current portfolio targets to different 3rd party API's
    /// every time the portfolio changes.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="securities and portfolio" />
    public class SignalExportDemonstrationAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string _collective2ApiKey = ""; // Replace this value with your Colletive2 API key
        private const int _collective2SystemId = 1; // Replace this value with your system ID

        private Symbol _spy;
        private Symbol _aig;

        public int FastPeriod = 100;
        public int SlowPeriod = 200;

        public ExponentialMovingAverage Fast;
        public ExponentialMovingAverage Slow;

        /// <summary>
        /// Initialize the date and add two securities
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100 * 1000);

            _spy = AddSecurity(SecurityType.Equity, "SPY").Symbol;
            _aig = AddSecurity(SecurityType.Equity, "AIG").Symbol;

            Fast = EMA("SPY", FastPeriod);
            Slow = EMA("SPY", SlowPeriod);

            // Set the signal export providers
            SignalExport.AddSignalExportProviders(new Collective2SignalExport(_collective2ApiKey, _collective2SystemId, Portfolio));
        }

        /// <summary>
        /// Remove one security and set holdings to the another one when the EMA's cross,
        /// then send a signal to the 3rd party API's defined
        /// </summary>
        /// <param name="slice"></param>
        public override void OnData(Slice slice)
        {
            // wait for our indicators to ready
            if (!Fast.IsReady || !Slow.IsReady) return;

            // This is not actually checking whether the EMA's are crossing between themselves
            if (Fast > Slow * 1.001m)
            {
                SetHoldings("SPY", 1);
                Liquidate("AIG");
                SignalExport.SetTargetPortfolio(Portfolio);
            }
            else if (Fast < Slow * 0.999m)
            {
                Liquidate("SPY");
                SetHoldings("AIG", 1);
                SignalExport.SetTargetPortfolio(Portfolio);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 7843;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.21%"},
            {"Compounding Annual Return", "210.122%"},
            {"Drawdown", "3.700%"},
            {"Expectancy", "-1"},
            {"Net Profit", "1.458%"},
            {"Sharpe Ratio", "4.625"},
            {"Probabilistic Sharpe Ratio", "57.701%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-1.473"},
            {"Beta", "1.551"},
            {"Annual Standard Deviation", "0.346"},
            {"Annual Variance", "0.12"},
            {"Information Ratio", "-3.022"},
            {"Tracking Error", "0.126"},
            {"Treynor Ratio", "1.033"},
            {"Total Fees", "$27.62"},
            {"Estimated Strategy Capacity", "$1800000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Fitness Score", "0.748"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "27.139"},
            {"Portfolio Turnover", "0.749"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "cf7be0604d18ec938013f0c596f20950"}
        };
    }
}
