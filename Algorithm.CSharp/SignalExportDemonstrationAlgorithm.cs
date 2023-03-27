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

using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm sends an array of current portfolio targets to different 3rd party API's
    /// every time the ema indicators crosses between themselves
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="securities and portfolio" />
    public class SignalExportDemonstrationAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string _collective2ApiKey = ""; // Replace this value with your Collective2 API key
        private const int _collective2SystemId = 0; // Replace this value with your system ID given by Collective2 API

        private const string _crunchDAOApiKey = ""; // Replace this value with your CrunchDAO API key
        private const string _crunchDAOModel = ""; // Replace this value with your model's name

        private const string _numeraiPublicId = ""; // Replace this value with your Numerai Signals Public ID
        private const string _numeraiSecretId = ""; // Replace this value with your Numerai Signals Secret ID
        private const string _numeraiModelId = ""; // Replace this value with your Numerai Signals Model ID

        private PortfolioTarget[] _targets = new PortfolioTarget[14];

        private bool _emaFastWasAbove;

        private bool _emaFastIsNotSet;

        private readonly int _fastPeriod = 100;
        private readonly int _slowPeriod = 200;

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        protected List<string> Symbols = new List<string>
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

            foreach (var symbol in Symbols)
            {
                AddEquity(symbol);
            }

            _fast = EMA("SPY", _fastPeriod);
            _slow = EMA("SPY", _slowPeriod);

            // Initialize this flag, to check when the ema indicators crosses between themselves
            _emaFastIsNotSet = true;

            // Set the signal export providers
            SignalExport.AddSignalExportProviders(new Collective2SignalExport(_collective2ApiKey, _collective2SystemId));
            SignalExport.AddSignalExportProviders(new CrunchDAOSignalExport(_crunchDAOApiKey, _crunchDAOModel));
            SignalExport.AddSignalExportProviders(new NumeraiSignalExport(_numeraiPublicId, _numeraiSecretId, _numeraiModelId));
        }

        /// <summary>
        /// Reduce the quantity of holdings for SPY or increase it, depending the case, 
        /// when the EMA's indicators crosses between themselves, then send a signal to the 3rd party
        /// API's already defined
        /// </summary>
        /// <param name="slice"></param>
        public override void OnData(Slice slice)
        {
            // Wait for our indicators to be ready
            if (!_fast.IsReady || !_slow.IsReady) return;

            // Set the value of flag _emaFastWasAbove, to know when the ema indicators crosses between themselves
            // Additionally, set an initial amount for each target
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

                SetInitialSignalValueForTargets();
            }

            // Check whether ema fast and ema slow crosses. If they do, set holdings to SPY
            // or reduce its holdings, and send signals to the 3rd party API's defined above
            if ((_fast > _slow * 1.001m) && (!_emaFastWasAbove))
            {
                SetHoldingsToSpyAndSendSignals((decimal)0.1);
            }
            else if ((_fast < _slow * 0.999m) && (_emaFastWasAbove))
            {
                SetHoldingsToSpyAndSendSignals((decimal)0.01);
            }
        }

        /// <summary>
        /// Set Holdings to SPY and sends signals to the different 3rd party API's already defined
        /// </summary>
        /// <param name="quantity">Percentage of holdings to set to SPY</param>
        public virtual void SetHoldingsToSpyAndSendSignals(decimal quantity)
        {
            SetHoldings("SPY", quantity);
            _targets[0] = new PortfolioTarget(Portfolio["SPY"].Symbol, quantity);
            SignalExport.SetTargetPortfolio(this, _targets);
        }
        
        /// <summary>
        /// Set initial signal value for each portfolio target in _targets array
        /// </summary>
        public virtual void SetInitialSignalValueForTargets()
        {
            int index = 0;
            foreach (var symbol in Symbols)
            {
                _targets[index] = new PortfolioTarget(Portfolio[symbol].Symbol, (decimal)0.05);
                index++;
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
        public long DataPoints => 11743;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "13.574%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.163%"},
            {"Sharpe Ratio", "13.636"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.043"},
            {"Beta", "0.033"},
            {"Annual Standard Deviation", "0.008"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.71"},
            {"Tracking Error", "0.215"},
            {"Treynor Ratio", "3.295"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$130000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.00%"},
            {"OrderListHash", "c275d939b91d3a24b4af6746fe3764c1"}
        };
    }
}
