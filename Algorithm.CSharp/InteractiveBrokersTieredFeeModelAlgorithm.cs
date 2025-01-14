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
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm using <see cref="InteractiveBrokersTieredFeeModel"/>
    /// </summary>
    public class InteractiveBrokersTieredFeeModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy, _aig, _bac;
        private InteractiveBrokersTieredFeeModel _feeModel = new InteractiveBrokersTieredFeeModel();
        private decimal _monthlyTradedVolume = 0m;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);   //Set Start Date
            SetEndDate(2013, 10, 10);     //Set End Date
            SetCash(1000000000);            //Set Strategy Cash

            // Set the fee model to be shared by all securities to accurately track the volume/value traded to select the correct tiered fee structure.
            SetSecurityInitializer((security) => security.SetFeeModel(_feeModel));

            _spy = AddEquity("SPY", Resolution.Minute, extendedMarketHours: true).Symbol;
            _aig = AddEquity("AIG", Resolution.Minute, extendedMarketHours: true).Symbol;
            _bac = AddEquity("BAC", Resolution.Minute, extendedMarketHours: true).Symbol;
        }

        public override void OnData(Slice slice)
        {
            // Order at different time for various order type to elicit different fee structure.
            if (slice.Time.Hour == 9 && slice.Time.Minute == 0)
            {
                SetHoldings(_spy, 0.1m);
                MarketOnOpenOrder(_aig, 30000);
                MarketOnOpenOrder(_bac, 30000);
            }
            else if (slice.Time.Hour == 10 && slice.Time.Minute == 0)
            {
                SetHoldings(_spy, 0.2m);
                MarketOrder(_aig, 30000);
                MarketOrder(_bac, 30000);
            }
            else if (slice.Time.Hour == 15 && slice.Time.Minute == 30)
            {
                SetHoldings(_spy, 0m);
                MarketOnCloseOrder(_aig, -60000);
                MarketOnCloseOrder(_bac, -60000);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                // Assert if the monthly traded volume is correct in the fee model.
                _monthlyTradedVolume += orderEvent.AbsoluteFillQuantity;
                var modelTradedVolume = _feeModel.MonthTradedVolume[SecurityType.Equity];
                if (_monthlyTradedVolume != modelTradedVolume)
                {
                    throw new Exception($"Monthly traded volume is incorrect - Actual: {_monthlyTradedVolume} - Model: {modelTradedVolume}");
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
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 23076;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "36"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-2.237%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-0.486"},
            {"Start Equity", "1000000000"},
            {"End Equity", "999762433.94"},
            {"Net Profit", "-0.024%"},
            {"Sharpe Ratio", "-8.397"},
            {"Sortino Ratio", "-11.384"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "75%"},
            {"Win Rate", "25%"},
            {"Profit-Loss Ratio", "1.06"},
            {"Alpha", "-0.035"},
            {"Beta", "0.009"},
            {"Annual Standard Deviation", "0.003"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-5.78"},
            {"Tracking Error", "0.269"},
            {"Treynor Ratio", "-2.319"},
            {"Total Fees", "$185772.29"},
            {"Estimated Strategy Capacity", "$11000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.37%"},
            {"OrderListHash", "d35a4e91c145a100d4bffb7c0fc0ff35"}
        };
    }
}
