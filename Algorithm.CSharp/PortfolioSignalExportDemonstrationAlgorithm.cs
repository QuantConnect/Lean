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
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm sends current portfolio targets from its Portfolio to different 3rd party API's
    /// every time the ema indiicators crosses between themselves.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="securities and portfolio" />
    public class PortfolioSignalExportDemonstrationAlgorithm : QCAlgorithm
    {
        private const string _collective2ApiKey = ""; // Replace this value with your Collective2 API key
        private const int _collective2SystemId = 0; // Replace this value with your system ID provided by Collective2 API

        private const string _crunchDAOApiKey = ""; // Replace this value with your CrunchDAO API key
        private const string _crunchDAOModel = ""; // Replace this value with your model's name

        private const string _numeraiPublicId = ""; // Replace this value with your Numerai Signals Public ID
        private const string _numeraiSecretId = ""; // Replace this value with your Numerai Signals Secret ID
        private const string _numeraiModelId = ""; // Replace this value with your Numerai Signals Model ID

        private List<string> _symbols = new List<string>
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

        private bool _emaFastWasAbove;

        private bool _emaFastIsNotSet;

        public int FastPeriod = 100;
        public int SlowPeriod = 200;

        public ExponentialMovingAverage Fast;
        public ExponentialMovingAverage Slow;

        /// <summary>
        /// Initialize the date and add all equity symbols present in list _symbols
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100 * 1000);

            foreach (var symbol in _symbols)
            {
                AddEquity(symbol);
            }

            Fast = EMA("SPY", FastPeriod);
            Slow = EMA("SPY", SlowPeriod);

            // Initialize this flag, to check when the ema indicators crosses between themselves
            _emaFastIsNotSet = true;

            // Set the signal export providers
            SignalExport.AddSignalExportProviders(new Collective2SignalExport(_collective2ApiKey, _collective2SystemId));
            SignalExport.AddSignalExportProviders(new CrunchDAOSignalExport(_crunchDAOApiKey, _crunchDAOModel));
            SignalExport.AddSignalExportProviders(new NumeraiSignalExport(_numeraiPublicId, _numeraiSecretId, _numeraiModelId));
        }

        /// <summary>
        /// Reduce the quantity of holdings for one security and increase the holdings to the another
        /// one when the EMA's indicators crosses between themselves, then send a signal to the 3rd party
        /// API's defined and quit the algorithm
        /// </summary>
        /// <param name="slice"></param>
        public override void OnData(Slice slice)
        {
            // Wait for our indicators to be ready
            if (!Fast.IsReady || !Slow.IsReady) return;

            // Set flag _emaFastWasAbove to know when the ema indicators crosses between themselves
            if (_emaFastIsNotSet)
            {
                if (Fast > Slow * 1.001m)
                {
                    _emaFastWasAbove = true;
                }
                else
                {
                    _emaFastWasAbove = false;
                }
                _emaFastIsNotSet = false;
            }

            // Check whether ema fast and ema slow crosses. If they do, set holdings to one
            // of SPY or AIG, and send signals to the 3rd party API's defined above
            if ((Fast > Slow * 1.001m) && (!_emaFastWasAbove))
            {
                SetHoldings("SPY", 0.1);
                foreach (var symbol in _symbols)
                {
                    if (symbol != "SPY")
                    {
                        SetHoldings(symbol, 0.01);
                    }
                }
                SignalExport.SetTargetPortfolio(this, Portfolio);
                Quit();
            }
            else if ((Fast < Slow * 0.999m) && (_emaFastWasAbove))
            {
                SetHoldings("AIG", 0.1);
                foreach (var symbol in _symbols)
                {
                    if (symbol != "AIG")
                    {
                        SetHoldings(symbol, 0.01);
                    }
                }
                SignalExport.SetTargetPortfolio(this, Portfolio);
                Quit();
            }
        }
    }
}
