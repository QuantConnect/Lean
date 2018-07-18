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
using QuantConnect.Data.Custom.Intrinio;
using QuantConnect.Indicators;
using QuantConnect.Parameters;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    ///     Basic template algorithm simply initializes the date range and cash. This is a skeleton
    ///     framework you can use for designing an algorithm.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class BasicTemplateIntrinioEconomicData : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        // Get the intrinio credentials from the parameters.
        [Parameter("intrinio-username")]
        public string _user;
        [Parameter("intrinio-password")]
        public string _password;

        private Symbol _uso; // United States Oil Fund LP
        private Symbol _bno; // United States Brent Oil Fund LP

        private readonly Identity _brent = new Identity("Brent");
        private readonly Identity _wti = new Identity("WTI");

        private CompositeIndicator<IndicatorDataPoint> _spread;

        /// <summary>
        ///     Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All
        ///     algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(year: 2010, month: 01, day: 01); //Set Start Date
            SetEndDate(year: 2013, month: 12, day: 31); //Set End Date
            SetCash(startingCash: 100000); //Set Strategy Cash

            // Set your Intrinino user and password.
            IntrinioConfig.SetUserAndPassword(_user, _password);

            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.
            _uso = AddEquity("USO", Resolution.Daily, leverage: 2m).Symbol;
            _bno = AddEquity("BNO", Resolution.Daily, leverage: 2m).Symbol;

            AddData<IntrinioEconomicData>(IntrinioEconomicDataSources.Commodities.CrudeOilWTI, Resolution.Daily);
            AddData<IntrinioEconomicData>(IntrinioEconomicDataSources.Commodities.CrudeOilBrent, Resolution.Daily);
            _spread = _brent.Minus(_wti);
        }

        /// <summary>
        ///     OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            var customData = data.Get<IntrinioEconomicData>();
            if (customData.Count == 0) return;

            foreach (var economicData in customData.Values)
            {
                if (economicData.Symbol.Value == IntrinioEconomicDataSources.Commodities.CrudeOilWTI)
                {
                    _wti.Update(economicData.Time, economicData.Price);
                }
                else
                {
                    _brent.Update(economicData.Time, economicData.Price);
                }
            }

            if (_spread > 0 && !Portfolio[_bno].IsLong ||
                _spread < 0 && !Portfolio[_uso].IsShort)
            {
                var logText = _spread > 0 ?
                    new[] {"higher", "long", "short"} :
                    new[] {"lower", "short", "long"};

                Log($"Brent Price is {logText[0]} than West Texas. Go {logText[1]} BNO and {logText[2]} USO.");
                SetHoldings(_bno, 0.25 * Math.Sign(_spread));
                SetHoldings(_uso, -0.25 * Math.Sign(_spread));
            }
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
            {"Total Trades", "90"},
            {"Average Win", "0.09%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "5.743%"},
            {"Drawdown", "21.500%"},
            {"Expectancy", "1.847"},
            {"Net Profit", "25.045%"},
            {"Sharpe Ratio", "0.416"},
            {"Loss Rate", "68%"},
            {"Win Rate", "32%"},
            {"Profit-Loss Ratio", "7.98"},
            {"Alpha", "0.098"},
            {"Beta", "-1.614"},
            {"Annual Standard Deviation", "0.16"},
            {"Annual Variance", "0.026"},
            {"Information Ratio", "0.295"},
            {"Tracking Error", "0.16"},
            {"Treynor Ratio", "-0.041"},
            {"Total Fees", "$101.67"}
        };
    }
}
