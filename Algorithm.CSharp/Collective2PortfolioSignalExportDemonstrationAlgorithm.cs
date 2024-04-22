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
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm sends a list of portfolio targets from algorithm's Portfolio 
    /// to Collective2 API every time the ema indicators crosses between themselves
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="securities and portfolio" />
    public class Collective2PortfolioSignalExportDemonstrationAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Collective2 APIv4 KEY: This value is provided by Collective2 in their webpage in your account section (See https://collective2.com/account-info)
        /// See API documentation at https://trade.collective2.com/c2-api
        /// </summary>
        private const string _collective2ApiKey = "YOUR APIV4 KEY";

        /// <summary>
        /// Collective2 System ID: This value is found beside the system's name (strategy's name) on the main system page
        /// </summary>
        private const int _collective2SystemId = 0;

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private bool _emaFastWasAbove;
        private bool _emaFastIsNotSet;
        private bool _firstCall = true;

        /// <summary>
        /// Symbols accepted by Collective2. Collective2 accepts stock,
        /// future, forex and US stock option symbols 
        /// </summary>
        private List<Symbol> _symbols = new()
        {   
            QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA, null, null),
            QuantConnect.Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda, null, null),
            QuantConnect.Symbol.CreateFuture("ES", Market.CME, new DateTime(2023, 12, 15), null),
            QuantConnect.Symbol.CreateOption("GOOG", Market.USA, OptionStyle.American, OptionRight.Call, 130, new DateTime(2023, 9, 1)),
        };

        /// <summary>
        /// Initialize the date and add all equity symbols present in _symbols list
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100 * 1000);

            foreach (var item in _symbols)
            {
                AddSecurity(item);
            }

            _fast = EMA("SPY", 10);
            _slow = EMA("SPY", 100);

            // Initialize this flag, to check when the ema indicators crosses between themselves
            _emaFastIsNotSet = true;

            // Set Collective2 signal export provider
            SignalExport.AddSignalExportProviders(new Collective2SignalExport(_collective2ApiKey, _collective2SystemId));

            SetWarmUp(100);
        }

        /// <summary>
        /// Reduce the quantity of holdings for SPY or increase it, depending the case, 
        /// when the EMA's indicators crosses between themselves, then send a signal to 
        /// Collective2 API
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
            // or reduce its holdings, and send signals to the Collective2 API from your
            // Portfolio
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
        public long DataPoints => 4155;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 11147;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "14.180%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000.00"},
            {"End Equity", "100169.68"},
            {"Net Profit", "0.170%"},
            {"Sharpe Ratio", "4.88"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "67.725%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.088"},
            {"Beta", "0.099"},
            {"Annual Standard Deviation", "0.022"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-9.315"},
            {"Tracking Error", "0.201"},
            {"Treynor Ratio", "1.086"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$260000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.00%"},
            {"OrderListHash", "006af1a065fca33ac1f1e9cd6bd02c11"}
        };
    }
}
