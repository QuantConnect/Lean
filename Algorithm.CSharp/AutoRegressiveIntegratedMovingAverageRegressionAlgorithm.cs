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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test the behaviour of ARMA versus AR models at the same order of differencing.
    /// In particular, an ARIMA(1,1,1) and ARIMA(1,1,0) are instantiated while orders are placed if their difference
    /// is sufficiently large (which would be due to the inclusion of the MA(1) term).
    /// </summary>
    public class AutoRegressiveIntegratedMovingAverageRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private AutoRegressiveIntegratedMovingAverage _arima;
        private AutoRegressiveIntegratedMovingAverage _ar;
        private decimal _last;

        public override void Initialize()
        {
            SetStartDate(2013, 1, 07);
            SetEndDate(2013, 12, 11);

            EnableAutomaticIndicatorWarmUp = true;
            AddEquity("SPY", Resolution.Daily);
            _arima = ARIMA("SPY", 1, 1, 1, 50);
            _ar = ARIMA("SPY", 1, 1, 0, 50);
        }

        public override void OnData(Slice slice)
        {
            if (_arima.IsReady)
            {
                if (Math.Abs(_ar.Current.Value - _arima.Current.Value) > 1) // Difference due to MA(1) being included.
                {
                    if (_arima.Current.Value > _last)
                    {
                        MarketOrder("SPY", 1);
                    }
                    else
                    {
                        MarketOrder("SPY", -1);
                    }
                }

                _last = _arima.Current.Value;
            }
        }

        public bool CanRunLocally { get; } = true;

        public Language[] Languages { get; } = { Language.Python, Language.CSharp };

        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades","65"},
            {"Average Win","0"},
            {"Average Loss","0"},
            {"Compounding Annual Return","0.0015"},
            {"Drawdown","0.001"},
            {"Expectancy","2.19"},
            {"Net Profit","0.0013"},
            {"Sharpe Ratio","0.993"},
            {"Probabilistic Sharpe Ratio","0.4967"},
            {"Loss Rate","0.29"},
            {"Win Rate","0.71"},
            {"Profit-Loss Ratio","3.5"},
            {"Alpha","0.001"},
            {"Beta","0"},
            {"Annual Standard Deviation","0.001"},
            {"Annual Variance","0"},
            {"Information Ratio","-2.168"},
            {"Tracking Error","0.099"},
            {"Treynor Ratio","-5.187"},
            {"Total Fees","65"},
            {"Fitness Score","0"},
            {"Kelly Criterion Estimate","0"},
            {"Kelly Criterion Probability Value","0"},
            {"Sortino Ratio","1.51"},
            {"Return Over Maximum Drawdown","1.819"},
            {"Portfolio Turnover","0"},
            {"Total Insights Generated","0"},
            {"Total Insights Closed","0"},
            {"Total Insights Analysis Completed","0"},
            {"Long Insight Count","0"},
            {"Short Insight Count","0"},
            {"Long/Short Ratio","1"},
            {"Estimated Monthly Alpha Value","0"},
            {"Total Accumulated Estimated Alpha Value","0"},
            {"Mean Population Estimated Insight Value","0"},
            {"Mean Population Direction","0"},
            {"Mean Population Magnitude","0"},
            {"Rolling Averaged Population Direction","0"},
            {"Rolling Averaged Population Magnitude","0"},
            {"OrderListHash","852801548"}
        };
    }
}