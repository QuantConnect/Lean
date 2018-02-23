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

using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;

namespace QuantConnect.Tests
{
    [TestFixture, Category("TravisExclude")]
    public class RegressionTests
    {
        [Test, TestCaseSource("GetRegressionTestParameters")]
        public void AlgorithmStatisticsRegression(AlgorithmStatisticsTestParameters parameters)
        {
            QuantConnect.Configuration.Config.Set("quandl-auth-token", "WyAazVXnq7ATy_fefTqm");
            QuantConnect.Configuration.Config.Set("forward-console-messages", "false");

            if (parameters.Algorithm == "OptionChainConsistencyRegressionAlgorithm")
            {
                // special arrangement for consistency test - we check if limits work fine
                QuantConnect.Configuration.Config.Set("symbol-minute-limit", "100");
                QuantConnect.Configuration.Config.Set("symbol-second-limit", "100");
                QuantConnect.Configuration.Config.Set("symbol-tick-limit", "100");
            }

            AlgorithmRunner.RunLocalBacktest(parameters.Algorithm, parameters.Statistics, parameters.AlphaStatistics, parameters.Language);
        }

        private static TestCaseData[] GetRegressionTestParameters()
        {
            var basicTemplateStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "264.956%"},
                {"Drawdown", "2.200%"},
                {"Expectancy", "0"},
                {"Net Profit", "1.669%"},
                {"Sharpe Ratio", "4.411"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.007"},
                {"Beta", "76.375"},
                {"Annual Standard Deviation", "0.193"},
                {"Annual Variance", "0.037"},
                {"Information Ratio", "4.355"},
                {"Tracking Error", "0.193"},
                {"Treynor Ratio", "0.011"},
                {"Total Fees", "$3.09"}
            };

            var basicTemplateFrameworkStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "264.956%"},
                {"Drawdown", "2.200%"},
                {"Expectancy", "0"},
                {"Net Profit", "1.669%"},
                {"Sharpe Ratio", "4.411"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.007"},
                {"Beta", "76.375"},
                {"Annual Standard Deviation", "0.193"},
                {"Annual Variance", "0.037"},
                {"Information Ratio", "4.355"},
                {"Tracking Error", "0.193"},
                {"Treynor Ratio", "0.011"},
                {"Total Fees", "$3.09"},
                {"Total Alphas Generated", "100"},
                {"Total Alphas Closed", "99"},
                {"Total Alphas Analysis Completed", "86"},
                {"Long Alpha Count", "100"},
                {"Short Alpha Count", "0"},
                {"Long/Short Ratio", "100%"},
                {"Total Estimated Alpha Value", "$24404.2897"},
                {"Mean Population Estimated Alpha Value", "$246.508"},
                {"Mean Population Direction", "48.8372%"},
                {"Mean Population Magnitude", "48.8372%"},
                {"Rolling Averaged Population Direction", "68.2411%"},
                {"Rolling Averaged Population Magnitude", "68.2411%"}
            };

            var basicTemplateOptionsStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "2"},
                {"Average Win", "0%"},
                {"Average Loss", "-0.28%"},
                {"Compounding Annual Return", "-78.105%"},
                {"Drawdown", "0.300%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-0.280%"},
                {"Sharpe Ratio", "0"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0"},
                {"Beta", "0"},
                {"Annual Standard Deviation", "0"},
                {"Annual Variance", "0"},
                {"Information Ratio", "0"},
                {"Tracking Error", "0"},
                {"Treynor Ratio", "0"},
                {"Total Fees", "$0.50"},
            };

            var limitFillRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "34"},
                {"Average Win", "0.02%"},
                {"Average Loss", "-0.02%"},
                {"Compounding Annual Return", "9.733%"},
                {"Drawdown", "0.400%"},
                {"Expectancy", "0.513"},
                {"Net Profit", "0.119%"},
                {"Sharpe Ratio", "1.954"},
                {"Loss Rate", "25%"},
                {"Win Rate", "75%"},
                {"Profit-Loss Ratio", "1.02"},
                {"Alpha", "-0.107"},
                {"Beta", "15.186"},
                {"Annual Standard Deviation", "0.031"},
                {"Annual Variance", "0.001"},
                {"Information Ratio", "1.6"},
                {"Tracking Error", "0.031"},
                {"Treynor Ratio", "0.004"},
                {"Total Fees", "$34.00"},
            };

            var updateOrderRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "21"},
                {"Average Win", "0%"},
                {"Average Loss", "-1.71%"},
                {"Compounding Annual Return", "-8.289%"},
                {"Drawdown", "16.700%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-15.892%"},
                {"Sharpe Ratio", "-1.358"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.065"},
                {"Beta", "-0.998"},
                {"Annual Standard Deviation", "0.062"},
                {"Annual Variance", "0.004"},
                {"Information Ratio", "-1.679"},
                {"Tracking Error", "0.062"},
                {"Treynor Ratio", "0.085"},
                {"Total Fees", "$21.00"},
            };

            var regressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "5431"},
                {"Average Win", "0.00%"},
                {"Average Loss", "0.00%"},
                {"Compounding Annual Return", "-4.156%"},
                {"Drawdown", "0.100%"},
                {"Expectancy", "-0.991"},
                {"Net Profit", "-0.054%"},
                {"Sharpe Ratio", "-91.188"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "2.40"},
                {"Alpha", "-0.019"},
                {"Beta", "-0.766"},
                {"Annual Standard Deviation", "0"},
                {"Annual Variance", "0"},
                {"Information Ratio", "-55.85"},
                {"Tracking Error", "0.001"},
                {"Treynor Ratio", "0.036"},
                {"Total Fees", "$5431.00"}
            };

            var universeSelectionRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "5"},
                {"Average Win", "0.70%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "-65.990%"},
                {"Drawdown", "5.500%"},
                {"Expectancy", "0"},
                {"Net Profit", "-4.899%"},
                {"Sharpe Ratio", "-3.181"},
                {"Loss Rate", "0%"},
                {"Win Rate", "100%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-1.055"},
                {"Beta", "2.525"},
                {"Annual Standard Deviation", "0.316"},
                {"Annual Variance", "0.1"},
                {"Information Ratio", "-3.243"},
                {"Tracking Error", "0.316"},
                {"Treynor Ratio", "-0.398"},
                {"Total Fees", "$5.00"},
            };

            var customDataRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "155.365%"},
                {"Drawdown", "84.800%"},
                {"Expectancy", "0"},
                {"Net Profit", "5123.170%"},
                {"Sharpe Ratio", "1.2"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.008"},
                {"Beta", "73.725"},
                {"Annual Standard Deviation", "0.84"},
                {"Annual Variance", "0.706"},
                {"Information Ratio", "1.183"},
                {"Tracking Error", "0.84"},
                {"Treynor Ratio", "0.014"},
                {"Total Fees", "$0.00"}
            };

            var addRemoveSecurityRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "5"},
                {"Average Win", "0.49%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "307.853%"},
                {"Drawdown", "1.400%"},
                {"Expectancy", "0"},
                {"Net Profit", "1.814%"},
                {"Sharpe Ratio", "6.474"},
                {"Loss Rate", "0%"},
                {"Win Rate", "100%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.004"},
                {"Beta", "82.594"},
                {"Annual Standard Deviation", "0.141"},
                {"Annual Variance", "0.02"},
                {"Information Ratio", "6.4"},
                {"Tracking Error", "0.141"},
                {"Treynor Ratio", "0.011"},
                {"Total Fees", "$25.20"}
            };

            var dropboxBaseDataUniverseSelectionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "90"},
                {"Average Win", "0.78%"},
                {"Average Loss", "-0.40%"},
                {"Compounding Annual Return", "18.691%"},
                {"Drawdown", "4.700%"},
                {"Expectancy", "1.071"},
                {"Net Profit", "18.635%"},
                {"Sharpe Ratio", "2.001"},
                {"Loss Rate", "30%"},
                {"Win Rate", "70%"},
                {"Profit-Loss Ratio", "1.97"},
                {"Alpha", "0.114"},
                {"Beta", "2.968"},
                {"Annual Standard Deviation", "0.086"},
                {"Annual Variance", "0.007"},
                {"Information Ratio", "1.773"},
                {"Tracking Error", "0.086"},
                {"Treynor Ratio", "0.058"},
                {"Total Fees", "$240.17"},
            };

            var dropboxUniverseSelectionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "66"},
                {"Average Win", "1.06%"},
                {"Average Loss", "-0.50%"},
                {"Compounding Annual Return", "18.292%"},
                {"Drawdown", "7.100%"},
                {"Expectancy", "0.815"},
                {"Net Profit", "18.237%"},
                {"Sharpe Ratio", "1.419"},
                {"Loss Rate", "42%"},
                {"Win Rate", "58%"},
                {"Profit-Loss Ratio", "2.13"},
                {"Alpha", "0.305"},
                {"Beta", "-9.973"},
                {"Annual Standard Deviation", "0.1"},
                {"Annual Variance", "0.01"},
                {"Information Ratio", "1.256"},
                {"Tracking Error", "0.1"},
                {"Treynor Ratio", "-0.014"},
                {"Total Fees", "$185.37"},
            };

            var parameterizedStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "278.616%"},
                {"Drawdown", "0.300%"},
                {"Expectancy", "0"},
                {"Net Profit", "1.717%"},
                {"Sharpe Ratio", "11.017"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0"},
                {"Beta", "78.067"},
                {"Annual Standard Deviation", "0.078"},
                {"Annual Variance", "0.006"},
                {"Information Ratio", "10.897"},
                {"Tracking Error", "0.078"},
                {"Treynor Ratio", "0.011"},
                {"Total Fees", "$3.09"},
            };

            var historyAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "265.867%"},
                {"Drawdown", "1.100%"},
                {"Expectancy", "0"},
                {"Net Profit", "1.072%"},
                {"Sharpe Ratio", "3.122"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0"},
                {"Beta", "66.746"},
                {"Annual Standard Deviation", "0.221"},
                {"Annual Variance", "0.049"},
                {"Information Ratio", "3.076"},
                {"Tracking Error", "0.221"},
                {"Treynor Ratio", "0.01"},
                {"Total Fees", "$3.09"},
            };

            var coarseFundamentalTop5AlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "10"},
                {"Average Win", "1.15%"},
                {"Average Loss", "-0.47%"},
                {"Compounding Annual Return", "-0.746%"},
                {"Drawdown", "3.000%"},
                {"Expectancy", "-0.313"},
                {"Net Profit", "-0.746%"},
                {"Sharpe Ratio", "-0.267"},
                {"Loss Rate", "80%"},
                {"Win Rate", "20%"},
                {"Profit-Loss Ratio", "2.44"},
                {"Alpha", "-0.008"},
                {"Beta", "0.032"},
                {"Annual Standard Deviation", "0.027"},
                {"Annual Variance", "0.001"},
                {"Information Ratio", "-1.014"},
                {"Tracking Error", "0.027"},
                {"Treynor Ratio", "-0.222"},
                {"Total Fees", "$10.61"},
            };

            var coarseFineFundamentalRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "6"},
                {"Average Win", "0%"},
                {"Average Loss", "-0.84%"},
                {"Compounding Annual Return", "-55.627%"},
                {"Drawdown", "9.100%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-6.460%"},
                {"Sharpe Ratio", "-3.127"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.163"},
                {"Beta", "-47.857"},
                {"Annual Standard Deviation", "0.236"},
                {"Annual Variance", "0.056"},
                {"Information Ratio", "-3.205"},
                {"Tracking Error", "0.236"},
                {"Treynor Ratio", "0.015"},
                {"Total Fees", "$13.92"},
            };

            var macdTrendAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "84"},
                {"Average Win", "4.79%"},
                {"Average Loss", "-4.17%"},
                {"Compounding Annual Return", "2.967%"},
                {"Drawdown", "34.800%"},
                {"Expectancy", "0.228"},
                {"Net Profit", "37.970%"},
                {"Sharpe Ratio", "0.299"},
                {"Loss Rate", "43%"},
                {"Win Rate", "57%"},
                {"Profit-Loss Ratio", "1.15"},
                {"Alpha", "0.111"},
                {"Beta", "-3.721"},
                {"Annual Standard Deviation", "0.124"},
                {"Annual Variance", "0.015"},
                {"Information Ratio", "0.137"},
                {"Tracking Error", "0.124"},
                {"Treynor Ratio", "-0.01"},
                {"Total Fees", "$420.57"},
            };

            var optionSplitRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades","2"},
                {"Average Win","0%"},
                {"Average Loss","-0.02%"},
                {"Compounding Annual Return","-1.242%"},
                {"Drawdown","0.000%"},
                {"Expectancy","-1"},
                {"Net Profit","-0.017%"},
                {"Sharpe Ratio","-7.099"},
                {"Loss Rate","100%"},
                {"Win Rate","0%"},
                {"Profit-Loss Ratio","0"},
                {"Alpha","-0.01"},
                {"Beta","0"},
                {"Annual Standard Deviation","0.001"},
                {"Annual Variance","0"},
                {"Information Ratio","7.126"},
                {"Tracking Error","6.064"},
                {"Treynor Ratio","174.306"},
                {"Total Fees","$0.50"},
            };

            var optionRenameRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "4"},
                {"Average Win", "0%"},
                {"Average Loss", "-0.02%"},
                {"Compounding Annual Return", "-0.472%"},
                {"Drawdown", "0.000%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-0.006%"},
                {"Sharpe Ratio", "-3.403"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.016"},
                {"Beta", "-0.001"},
                {"Annual Standard Deviation", "0.001"},
                {"Annual Variance", "0"},
                {"Information Ratio", "10.014"},
                {"Tracking Error", "0.877"},
                {"Treynor Ratio", "4.203"},
                {"Total Fees", "$2.50"},
            };

            var optionOpenInterestRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "2"},
                {"Average Win", "0%"},
                {"Average Loss", "-0.01%"},
                {"Compounding Annual Return", "-2.042%"},
                {"Drawdown", "0.000%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-0.010%"},
                {"Sharpe Ratio", "-11.225"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0"},
                {"Beta", "-0.036"},
                {"Annual Standard Deviation", "0.001"},
                {"Annual Variance", "0"},
                {"Information Ratio", "-11.225"},
                {"Tracking Error", "0.033"},
                {"Treynor Ratio", "0.355"},
                {"Total Fees", "$0.50"},
            };

            var optionChainConsistencyRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "2"},
                {"Average Win", "0%"},
                {"Average Loss", "-3.86%"},
                {"Compounding Annual Return", "-100.000%"},
                {"Drawdown", "3.900%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-3.855%"},
                {"Sharpe Ratio", "0"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0"},
                {"Beta", "0"},
                {"Annual Standard Deviation", "0"},
                {"Annual Variance", "0"},
                {"Information Ratio", "0"},
                {"Tracking Error", "0"},
                {"Treynor Ratio", "0"},
                {"Total Fees", "$0.50"},
            };

            var weeklyUniverseSelectionRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "8"},
                {"Average Win", "0.28%"},
                {"Average Loss", "-0.33%"},
                {"Compounding Annual Return", "-1.247%"},
                {"Drawdown", "1.300%"},
                {"Expectancy", "-0.078"},
                {"Net Profit", "-0.105%"},
                {"Sharpe Ratio", "-0.27"},
                {"Loss Rate", "50%"},
                {"Win Rate", "50%"},
                {"Profit-Loss Ratio", "0.84"},
                {"Alpha", "-0.239"},
                {"Beta", "12.675"},
                {"Annual Standard Deviation", "0.04"},
                {"Annual Variance", "0.002"},
                {"Information Ratio", "-0.723"},
                {"Tracking Error", "0.04"},
                {"Treynor Ratio", "-0.001"},
                {"Total Fees", "$23.23"},
            };

            var optionExerciseAssignRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "4"},
                {"Average Win", "0.30%"},
                {"Average Loss", "-0.33%"},
                {"Compounding Annual Return", "-85.023%"},
                {"Drawdown", "0.400%"},
                {"Expectancy", "-0.358"},
                {"Net Profit", "-0.350%"},
                {"Sharpe Ratio", "0"},
                {"Loss Rate", "67%"},
                {"Win Rate", "33%"},
                {"Profit-Loss Ratio", "0.93"},
                {"Alpha", "0"},
                {"Beta", "0"},
                {"Annual Standard Deviation", "0"},
                {"Annual Variance", "0"},
                {"Information Ratio", "0"},
                {"Tracking Error", "0"},
                {"Treynor Ratio", "0"},
                {"Total Fees", "$0.50"},
            };

            var basicTemplateDailyStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "210.391%"},
                {"Drawdown", "1.100%"},
                {"Expectancy", "0"},
                {"Net Profit", "3.472%"},
                {"Sharpe Ratio", "5.695"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.616"},
                {"Beta", "16.856"},
                {"Annual Standard Deviation", "0.153"},
                {"Annual Variance", "0.023"},
                {"Information Ratio", "5.597"},
                {"Tracking Error", "0.153"},
                {"Treynor Ratio", "0.052"},
                {"Total Fees", "$3.09"},
            };

            var hourSplitStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "-0.096%"},
                {"Drawdown", "0.000%"},
                {"Expectancy", "0"},
                {"Net Profit", "-0.001%"},
                {"Sharpe Ratio", "-11.225"},
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
                {"Total Fees", "$1.00"}
            };

            var hourReverseSplitStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "-1.444%"},
                {"Drawdown", "0.000%"},
                {"Expectancy", "0"},
                {"Net Profit", "-0.007%"},
                {"Sharpe Ratio", "-11.225"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0"},
                {"Beta", "0"},
                {"Annual Standard Deviation", "0.001"},
                {"Annual Variance", "0"},
                {"Information Ratio", "0"},
                {"Tracking Error", "0"},
                {"Treynor Ratio", "0"},
                {"Total Fees", "$1.00"}
            };

            var fractionalQuantityRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "6"},
                {"Average Win", "0.95%"},
                {"Average Loss", "-2.02%"},
                {"Compounding Annual Return", "254.081%"},
                {"Drawdown", "6.600%"},
                {"Expectancy", "-0.018"},
                {"Net Profit", "1.395%"},
                {"Sharpe Ratio", "1.176"},
                {"Loss Rate", "33%"},
                {"Win Rate", "67%"},
                {"Profit-Loss Ratio", "0.47"},
                {"Alpha", "-1.18"},
                {"Beta", "1.249"},
                {"Annual Standard Deviation", "0.813"},
                {"Annual Variance", "0.66"},
                {"Information Ratio", "-4.245"},
                {"Tracking Error", "0.178"},
                {"Treynor Ratio", "0.765"},
                {"Total Fees", "$2045.20"}
            };

            var basicTemplateFuturesAlgorithmDailyStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "8"},
                {"Average Win", "0%"},
                {"Average Loss", "0.00%"},
                {"Compounding Annual Return", "-1.655%"},
                {"Drawdown", "0.000%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-0.018%"},
                {"Sharpe Ratio", "-23.092"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0"},
                {"Beta", "0"},
                {"Annual Standard Deviation", "0"},
                {"Annual Variance", "0"},
                {"Information Ratio", "0"},
                {"Tracking Error", "0"},
                {"Treynor Ratio", "0"},
                {"Total Fees", "$14.80"}
            };

            var basicTemplateCryptoAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "24"},
                {"Average Win", "0%"},
                {"Average Loss", "-0.46%"},
                {"Compounding Annual Return", "-100.000%"},
                {"Drawdown", "5.200%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-5.411%"},
                {"Sharpe Ratio", "-17.988"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-11.165"},
                {"Beta", "621.767"},
                {"Annual Standard Deviation", "0.382"},
                {"Annual Variance", "0.146"},
                {"Information Ratio", "-18.035"},
                {"Tracking Error", "0.382"},
                {"Treynor Ratio", "-0.011"},
                {"Total Fees", "$5840.44"}
            };

            var forexDailyDataPointsRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "61.020%"},
                {"Drawdown", "0%"},
                {"Expectancy", "0"},
                {"Net Profit", "0.261%"},
                {"Sharpe Ratio", "10.128"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.002"},
                {"Beta", "24.058"},
                {"Annual Standard Deviation", "0.022"},
                {"Annual Variance", "0"},
                {"Information Ratio", "9.829"},
                {"Tracking Error", "0.021"},
                {"Treynor Ratio", "0.009"},
                {"Total Fees", "$0.00"}
            };

            return new List<AlgorithmStatisticsTestParameters>
            {
                // CSharp
                new AlgorithmStatisticsTestParameters("BasicTemplateFuturesAlgorithmDaily", basicTemplateFuturesAlgorithmDailyStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("AddRemoveSecurityRegressionAlgorithm", addRemoveSecurityRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("BasicTemplateAlgorithm", basicTemplateStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("BasicTemplateFrameworkAlgorithm", basicTemplateFrameworkStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("BasicTemplateOptionsAlgorithm", basicTemplateOptionsStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("CustomDataRegressionAlgorithm", customDataRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("DropboxBaseDataUniverseSelectionAlgorithm", dropboxBaseDataUniverseSelectionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("DropboxUniverseSelectionAlgorithm", dropboxUniverseSelectionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("LimitFillRegressionAlgorithm", limitFillRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("ParameterizedAlgorithm", parameterizedStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("RegressionAlgorithm", regressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("UniverseSelectionRegressionAlgorithm", universeSelectionRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("UpdateOrderRegressionAlgorithm", updateOrderRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("HistoryAlgorithm", historyAlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("CoarseFundamentalTop5Algorithm", coarseFundamentalTop5AlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("CoarseFineFundamentalRegressionAlgorithm", coarseFineFundamentalRegressionAlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("MACDTrendAlgorithm", macdTrendAlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("OptionSplitRegressionAlgorithm", optionSplitRegressionAlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("OptionRenameRegressionAlgorithm", optionRenameRegressionAlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("OptionOpenInterestRegressionAlgorithm", optionOpenInterestRegressionAlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("OptionChainConsistencyRegressionAlgorithm", optionChainConsistencyRegressionAlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("WeeklyUniverseSelectionRegressionAlgorithm", weeklyUniverseSelectionRegressionAlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("OptionExerciseAssignRegressionAlgorithm",optionExerciseAssignRegressionAlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("BasicTemplateDailyAlgorithm", basicTemplateDailyStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("HourSplitRegressionAlgorithm", hourSplitStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("HourReverseSplitRegressionAlgorithm", hourReverseSplitStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("FractionalQuantityRegressionAlgorithm", fractionalQuantityRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("BasicTemplateCryptoAlgorithm", basicTemplateCryptoAlgorithmStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("ForexDailyDataPointsRegressionAlgorithm", forexDailyDataPointsRegressionAlgorithmStatistics, Language.CSharp),

                // Python
                // new AlgorithmStatisticsTestParameters("BasicTemplateFuturesAlgorithmDaily", basicTemplateFuturesAlgorithmDailyStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("AddRemoveSecurityRegressionAlgorithm", addRemoveSecurityRegressionStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("BasicTemplateAlgorithm", basicTemplateStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("BasicTemplateOptionsAlgorithm", basicTemplateOptionsStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("CustomDataRegressionAlgorithm", customDataRegressionStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("DropboxBaseDataUniverseSelectionAlgorithm", dropboxBaseDataUniverseSelectionStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("DropboxUniverseSelectionAlgorithm", dropboxUniverseSelectionStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("LimitFillRegressionAlgorithm", limitFillRegressionStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("ParameterizedAlgorithm", parameterizedStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("RegressionAlgorithm", regressionStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("UniverseSelectionRegressionAlgorithm", universeSelectionRegressionStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("UpdateOrderRegressionAlgorithm", updateOrderRegressionStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("HistoryAlgorithm", historyAlgorithmStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("CoarseFundamentalTop5Algorithm", coarseFundamentalTop5AlgorithmStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("CoarseFineFundamentalRegressionAlgorithm", coarseFineFundamentalRegressionAlgorithmStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("MACDTrendAlgorithm", macdTrendAlgorithmStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("OptionSplitRegressionAlgorithm", optionSplitRegressionAlgorithmStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("OptionRenameRegressionAlgorithm", optionRenameRegressionAlgorithmStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("OptionOpenInterestRegressionAlgorithm", optionOpenInterestRegressionAlgorithmStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("OptionChainConsistencyRegressionAlgorithm", optionChainConsistencyRegressionAlgorithmStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("WeeklyUniverseSelectionRegressionAlgorithm", weeklyUniverseSelectionRegressionAlgorithmStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("OptionExerciseAssignRegressionAlgorithm",optionExerciseAssignRegressionAlgorithmStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("BasicTemplateDailyAlgorithm", basicTemplateDailyStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("HourSplitRegressionAlgorithm", hourSplitStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("HourReverseSplitRegressionAlgorithm", hourReverseSplitStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("FractionalQuantityRegressionAlgorithm", fractionalQuantityRegressionStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("CustomIndicatorAlgorithm", basicTemplateStatistics, Language.Python),
                new AlgorithmStatisticsTestParameters("BasicTemplateCryptoAlgorithm", basicTemplateCryptoAlgorithmStatistics, Language.Python)

                // FSharp
                // new AlgorithmStatisticsTestParameters("BasicTemplateAlgorithm", basicTemplateStatistics, Language.FSharp),

                // VisualBasic
                // new AlgorithmStatisticsTestParameters("BasicTemplateAlgorithm", basicTemplateStatistics, Language.VisualBasic),
            }.Select(x => new TestCaseData(x).SetName(x.Language + "/" + x.Algorithm)).ToArray();
        }

        public class AlgorithmStatisticsTestParameters
        {
            public readonly string Algorithm;
            public readonly Dictionary<string, string> Statistics;
            public readonly AlphaRuntimeStatistics AlphaStatistics;
            public readonly Language Language;

            public AlgorithmStatisticsTestParameters(string algorithm, Dictionary<string, string> statistics, Language language)
            {
                Algorithm = algorithm;
                Statistics = statistics;
                Language = language;
            }
        }
    }

}
