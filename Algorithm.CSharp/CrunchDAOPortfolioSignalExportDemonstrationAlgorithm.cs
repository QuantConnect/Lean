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
    /// This algorithm sends a current portfolio target from algorithm's Portfolio
    /// to CrunchDAO API every time the ema indicators crosses between themselves
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="securities and portfolio" />
    public class CrunchDAOPortfolioSignalExportDemonstrationAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// CrunchDAO API key: This value is provided by CrunchDAO when you sign up
        /// </summary>
        private const string _crunchDAOApiKey = "";

        /// <summary>
        /// CrunchDAO Model ID: When your email is verified, you can find this value in your CrunchDAO profile main page:
        /// See (https://tournament.crunchdao.com/profile/alpha)
        /// </summary>
        private const string _crunchDAOModel = "";

        private const string _crunchDAOSubmissionName = ""; // Replace this value with the name for your submission (Optional)
        private const string _crunchDAOComment = ""; // Replace this value with a comment for your submission (Optional)

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private bool _emaFastWasAbove;
        private bool _emaFastIsNotSet;
        private bool _firstCall = true;

        /// <summary>
        /// Initialize the date and add one equity symbol, as CrunchDAO
        /// only accepts stock and index symbols
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100 * 1000);

            AddEquity("SPY");

            _fast = EMA("SPY", 10);
            _slow = EMA("SPY", 100);

            // Initialize this flag, to check when the ema indicators crosses between themselves
            _emaFastIsNotSet = true;

            // Set CrunchDAO signal export provider
            SignalExport.AddSignalExportProviders(new CrunchDAOSignalExport(_crunchDAOApiKey, _crunchDAOModel, _crunchDAOSubmissionName, _crunchDAOComment));

            SetWarmUp(100);
        }

        /// <summary>
        /// Reduce the quantity of holdings for SPY or increase it, depending the case, 
        /// when the EMA's indicators crosses between themselves, then send a signal to 
        /// CrunchDAO API
        /// </summary>
        /// <param name="slice"></param>
        public override void OnData(Slice slice)
        {
            if (IsWarmingUp) return;

            // Place an order as soon as possible to send a signal.
            if (_firstCall)
            {
                SetHoldings("SPY", 0.1);
                SignalExport.SetTargetPortfolioFromPortfolio();
                _firstCall = false;
            }

            // Set the value of flag _emaFastWasAbove, to know when the ema indicators crosses between themselves
            if (_emaFastIsNotSet)
            {
                if (_fast > _slow * 1.001m)
                {
                    _emaFastWasAbove = true;
                }
                else
                {
                    _emaFastWasAbove = false;
                }
                _emaFastIsNotSet = false;
            }

            // Check whether ema fast and ema slow crosses. If they do, set holdings to SPY
            // or reduce its holdings, and send signals to the CrunchDAO API from your Portfolio
            if ((_fast > _slow * 1.001m) && (!_emaFastWasAbove))
            {
                SetHoldings("SPY", 0.1);
                SignalExport.SetTargetPortfolioFromPortfolio();
            }
            else if ((_fast < _slow * 0.999m) && (_emaFastWasAbove))
            {
                SetHoldings("SPY", 0.01);
                SignalExport.SetTargetPortfolioFromPortfolio();
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
        public long DataPoints => 4152;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "13.695%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "-0.463"},
            {"Net Profit", "0.164%"},
            {"Sharpe Ratio", "5.073"},
            {"Probabilistic Sharpe Ratio", "67.051%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.07"},
            {"Alpha", "-0.084"},
            {"Beta", "0.098"},
            {"Annual Standard Deviation", "0.022"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-9.329"},
            {"Tracking Error", "0.201"},
            {"Treynor Ratio", "1.129"},
            {"Total Fees", "$4.00"},
            {"Estimated Strategy Capacity", "$71000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.06%"},
            {"OrderListHash", "dc329f765a22f1fa98d5e87c53c11ef2"}
        };
    }
}
