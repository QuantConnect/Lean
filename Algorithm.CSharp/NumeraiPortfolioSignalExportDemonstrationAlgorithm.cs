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
    /// This algorithm sends a list of portfolio targets from algorithm's Portfolio
    /// to Numerai API every time the ema indicators crosses between themselves. 
    /// See (https://docs.numer.ai/numerai-signals/signals-overview) for more information
    /// about accepted symbols, signals, etc.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="securities and portfolio" />
    public class NumeraiPortfolioSignalExportDemonstrationAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Numerai Public ID: This value is provided by Numerai Signals in their main webpage once you've logged in
        /// and created a API key. See (https://signals.numer.ai/account)
        /// </summary>
        private const string _numeraiPublicId = "";

        /// <summary>
        /// Numerai Public ID: This value is provided by Numerai Signals in their main webpage once you've logged in
        /// and created a API key. See (https://signals.numer.ai/account)
        /// </summary>
        private const string _numeraiSecretId = "";

        /// <summary>
        /// Numerai Model ID: This value is provided by Numerai Signals in their main webpage once you've logged in
        /// and created a model. See (https://signals.numer.ai/models)
        /// </summary>
        private const string _numeraiModelId = "";

        private const string _numeraiFilename = ""; // Replace this value with your submission filename (Optional)

        private bool _emaFastWasAbove;
        private bool _emaFastIsNotSet;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private bool _firstCall = true;

        private List<string> _symbols = new() // Numerai accepts minimum 10 signals
        {
            "SPY",
            "AIG",
            "GOOGL",
            "AAPL",
            "AMZN",
            "TSLA",
            "NFLX",
            "INTC",
            "MSFT",
            "KO",
            "WMT",
            "IBM",
            "AMGN",
            "CAT"
        };

        /// <summary>
        /// Initialize the date and add all equity symbols present in Symbols list
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100 * 1000);

            foreach (var ticker in _symbols)
            {
                AddEquity(ticker);
            }

            _fast = EMA("SPY", 10);
            _slow = EMA("SPY", 100);

            // Initialize this flag, to check when the ema indicators crosses between themselves
            _emaFastIsNotSet = true;

            // Set Numerai signal export provider
            SignalExport.AddSignalExportProviders(new NumeraiSignalExport(_numeraiPublicId, _numeraiSecretId, _numeraiModelId, _numeraiFilename));

            SetWarmUp(100);
        }

        /// <summary>
        /// Reduce the quantity of holdings for SPY or increase it, depending the case, 
        /// when the EMA's indicators crosses between themselves, then send a signal to
        /// Numerai API
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

            // Set the value of flag _emaFastWasAbove, to know when the ema indicators crosses between themselves.
            // Additionally, set an initial holding quantity for each symbol. This is done because Numerai only
            // accept signals between 0 and 1 (exclusive)
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

                foreach (var ticker in _symbols)
                {
                    SetHoldings(ticker, (decimal)0.05);
                }
            }

            // Check whether ema fast and ema slow crosses. If they do, set holdings to SPY
            // or reduce its holdings, and send signals to Numerai API from your Portfolio
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
        public long DataPoints => 12356;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "17"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "29.347%"},
            {"Drawdown", "0.500%"},
            {"Expectancy", "-0.853"},
            {"Net Profit", "0.330%"},
            {"Sharpe Ratio", "5.467"},
            {"Probabilistic Sharpe Ratio", "65.836%"},
            {"Loss Rate", "86%"},
            {"Win Rate", "14%"},
            {"Profit-Loss Ratio", "0.03"},
            {"Alpha", "-0.176"},
            {"Beta", "0.233"},
            {"Annual Standard Deviation", "0.052"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-9.932"},
            {"Tracking Error", "0.171"},
            {"Treynor Ratio", "1.229"},
            {"Total Fees", "$17.00"},
            {"Estimated Strategy Capacity", "$13000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "6.27%"},
            {"OrderListHash", "f63c25ed18300c8cdaf6b24295738d63"}
        };
    }
}
