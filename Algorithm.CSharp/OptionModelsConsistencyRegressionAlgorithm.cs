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

using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Orders.Fills;
using System;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities.Volatility;
using QuantConnect.Logging;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that when setting custom models for canonical options, a one-time warning is sent
    /// informing the user that the contracts models are different (not the custom ones).
    /// </summary>
    public class OptionModelsConsistencyRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private ILogHandler _originalLogHandler;

        protected bool WarningSent { get; set; }

        public override void Initialize()
        {
            // Set a functional log handler in order to be able to assert on the warning message
            _originalLogHandler = Logging.Log.LogHandler;
            Logging.Log.LogHandler = new CompositeLogHandler(new ILogHandler[]
            {
                Logging.Log.LogHandler,
                new FunctionalLogHandler(
                    (debugMessage) => { },
                    (traceMessage) =>
                    {
                        if (traceMessage.Contains("Debug: Warning: Security ") &&
                            traceMessage.EndsWith("To avoid this, consider using a security initializer to set the right models to each security type according to your algorithm's requirements."))
                        {
                            WarningSent = true;
                        }
                    },
                    (errorMessage) => { })
            });

            var security = InitializeAlgorithm();
            SetModels(security);

            SetBenchmark(x => 0);
        }

        protected virtual Security InitializeAlgorithm()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);

            var equity = AddEquity("GOOG", leverage: 4);
            var option = AddOption(equity.Symbol);
            option.SetFilter(u => u.Strikes(-2, +2).Expiration(0, 180));

            return option;
        }

        protected virtual void SetModels(Security security)
        {
            security.SetFillModel(new CustomFillModel());
            security.SetFeeModel(new CustomFeeModel());
            security.SetBuyingPowerModel(new CustomBuyingPowerModel());
            security.SetSlippageModel(new CustomSlippageModel());
            security.SetVolatilityModel(new CustomVolatilityModel());
            security.SettlementModel = new CustomSettlementModel();
        }

        public override void OnEndOfAlgorithm()
        {
            Logging.Log.LogHandler = _originalLogHandler;

            if (!WarningSent)
            {
                throw new Exception("On-time warning about canonical models mismatch was not sent.");
            }
        }

        public class CustomFillModel : FillModel
        {
        }

        public class CustomFeeModel : FeeModel
        {
        }

        public class CustomBuyingPowerModel : BuyingPowerModel
        {
        }

        public class CustomSlippageModel : ConstantSlippageModel
        {
            public CustomSlippageModel() : base(0)
            {
            }
        }

        public class CustomVolatilityModel : BaseVolatilityModel
        {
        }

        public class CustomSettlementModel : ImmediateSettlementModel
        {
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 475777;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
