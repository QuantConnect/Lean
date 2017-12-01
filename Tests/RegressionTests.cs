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

            if (parameters.Algorithm == "OptionChainConsistencyRegressionAlgorithm")
            {
                // special arrangement for consistency test - we check if limits work fine
                QuantConnect.Configuration.Config.Set("symbol-minute-limit", "100");
                QuantConnect.Configuration.Config.Set("symbol-second-limit", "100");
                QuantConnect.Configuration.Config.Set("symbol-tick-limit", "100");
            }

            AlgorithmRunner.RunLocalBacktest(parameters.Algorithm, parameters.Statistics, parameters.Language);
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
                {"Alpha", "0.002"},
                {"Beta", "1"},
                {"Annual Standard Deviation", "0.193"},
                {"Annual Variance", "0.037"},
                {"Information Ratio", "6.816"},
                {"Tracking Error", "0"},
                {"Treynor Ratio", "0.851"},
                {"Total Fees", "$3.09"}
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
                {"Compounding Annual Return", "8.350%"},
                {"Drawdown", "0.400%"},
                {"Expectancy", "0.447"},
                {"Net Profit", "0.103%"},
                {"Sharpe Ratio", "1.747"},
                {"Loss Rate", "31%"},
                {"Win Rate", "69%"},
                {"Profit-Loss Ratio", "1.10"},
                {"Alpha", "-0.077"},
                {"Beta", "0.152"},
                {"Annual Standard Deviation", "0.03"},
                {"Annual Variance", "0.001"},
                {"Information Ratio", "-4.87"},
                {"Tracking Error", "0.164"},
                {"Treynor Ratio", "0.343"},
                {"Total Fees", "$34.00"}
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
                {"Sharpe Ratio", "-1.225"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.011"},
                {"Beta", "-0.469"},
                {"Annual Standard Deviation", "0.056"},
                {"Annual Variance", "0.003"},
                {"Information Ratio", "-1.573"},
                {"Tracking Error", "0.152"},
                {"Treynor Ratio", "0.147"},
                {"Total Fees", "$21.00"}
            };

            var regressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "5433"},
                {"Average Win", "0.00%"},
                {"Average Loss", "0.00%"},
                {"Compounding Annual Return", "-3.886%"},
                {"Drawdown", "0.100%"},
                {"Expectancy", "-0.991"},
                {"Net Profit", "-0.054%"},
                {"Sharpe Ratio", "-30.336"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "2.40"},
                {"Alpha", "-0.022"},
                {"Beta", "-0.001"},
                {"Annual Standard Deviation", "0.001"},
                {"Annual Variance", "0"},
                {"Information Ratio", "-4.198"},
                {"Tracking Error", "0.174"},
                {"Treynor Ratio", "35.023"},
                {"Total Fees", "$5433.00"}
            };

            var universeSelectionRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "5"},
                {"Average Win", "0.70%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "-73.872%"},
                {"Drawdown", "6.600%"},
                {"Expectancy", "0"},
                {"Net Profit", "-6.060%"},
                {"Sharpe Ratio", "-3.562"},
                {"Loss Rate", "0%"},
                {"Win Rate", "100%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.681"},
                {"Beta", "2.014"},
                {"Annual Standard Deviation", "0.284"},
                {"Annual Variance", "0.08"},
                {"Information Ratio", "-3.67"},
                {"Tracking Error", "0.231"},
                {"Treynor Ratio", "-0.502"},
                {"Total Fees", "$5.00"}
            };

            var customDataRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "155.210%"},
                {"Drawdown", "84.800%"},
                {"Expectancy", "0"},
                {"Net Profit", "5123.170%"},
                {"Sharpe Ratio", "1.199"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.99"},
                {"Beta", "0.168"},
                {"Annual Standard Deviation", "0.84"},
                {"Annual Variance", "0.706"},
                {"Information Ratio", "1.072"},
                {"Tracking Error", "0.845"},
                {"Treynor Ratio", "5.997"},
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
                {"Alpha", "0.306"},
                {"Beta", "0.718"},
                {"Annual Standard Deviation", "0.141"},
                {"Annual Variance", "0.02"},
                {"Information Ratio", "1.077"},
                {"Tracking Error", "0.062"},
                {"Treynor Ratio", "1.275"},
                {"Total Fees", "$25.20"}
            };

            var dropboxBaseDataUniverseSelectionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "67"},
                {"Average Win", "1.13%"},
                {"Average Loss", "-0.69%"},
                {"Compounding Annual Return", "17.718%"},
                {"Drawdown", "5.100%"},
                {"Expectancy", "0.813"},
                {"Net Profit", "17.718%"},
                {"Sharpe Ratio", "1.38"},
                {"Loss Rate", "31%"},
                {"Win Rate", "69%"},
                {"Profit-Loss Ratio", "1.64"},
                {"Alpha", "0.055"},
                {"Beta", "0.379"},
                {"Annual Standard Deviation", "0.099"},
                {"Annual Variance", "0.01"},
                {"Information Ratio", "-0.703"},
                {"Tracking Error", "0.11"},
                {"Treynor Ratio", "0.359"},
                {"Total Fees", "$300.15"}
            };

            var dropboxUniverseSelectionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "49"},
                {"Average Win", "1.58%"},
                {"Average Loss", "-1.03%"},
                {"Compounding Annual Return", "21.281%"},
                {"Drawdown", "8.200%"},
                {"Expectancy", "0.646"},
                {"Net Profit", "21.281%"},
                {"Sharpe Ratio", "1.362"},
                {"Loss Rate", "35%"},
                {"Win Rate", "65%"},
                {"Profit-Loss Ratio", "1.52"},
                {"Alpha", "0.012"},
                {"Beta", "0.705"},
                {"Annual Standard Deviation", "0.12"},
                {"Annual Variance", "0.014"},
                {"Information Ratio", "-0.51"},
                {"Tracking Error", "0.101"},
                {"Treynor Ratio", "0.232"},
                {"Total Fees", "$232.92"}
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
                {"Alpha", "0.553"},
                {"Beta", "0.364"},
                {"Annual Standard Deviation", "0.078"},
                {"Annual Variance", "0.006"},
                {"Information Ratio", "0.101"},
                {"Tracking Error", "0.127"},
                {"Treynor Ratio", "2.367"},
                {"Total Fees", "$3.09"},
            };

            var historyAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "372.677%"},
                {"Drawdown", "1.100%"},
                {"Expectancy", "0"},
                {"Net Profit", "1.717%"},
                {"Sharpe Ratio", "4.521"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.006"},
                {"Beta", "0.997"},
                {"Annual Standard Deviation", "0.193"},
                {"Annual Variance", "0.037"},
                {"Information Ratio", "6.231"},
                {"Tracking Error", "0.001"},
                {"Treynor Ratio", "0.876"},
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
                {"Sharpe Ratio", "-0.242"},
                {"Loss Rate", "80%"},
                {"Win Rate", "20%"},
                {"Profit-Loss Ratio", "2.44"},
                {"Alpha", "-0.01"},
                {"Beta", "0.044"},
                {"Annual Standard Deviation", "0.024"},
                {"Annual Variance", "0.001"},
                {"Information Ratio", "-0.973"},
                {"Tracking Error", "0.1"},
                {"Treynor Ratio", "-0.13"},
                {"Total Fees", "$10.61"},
            };

            var coarseFineFundamentalRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "6"},
                {"Average Win", "0%"},
                {"Average Loss", "-0.84%"},
                {"Compounding Annual Return", "-57.345%"},
                {"Drawdown", "9.100%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-6.763%"},
                {"Sharpe Ratio", "-3.025"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.754"},
                {"Beta", "1.258"},
                {"Annual Standard Deviation", "0.217"},
                {"Annual Variance", "0.047"},
                {"Information Ratio", "-4.525"},
                {"Tracking Error", "0.162"},
                {"Treynor Ratio", "-0.521"},
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
                {"Sharpe Ratio", "0.27"},
                {"Loss Rate", "43%"},
                {"Win Rate", "57%"},
                {"Profit-Loss Ratio", "1.15"},
                {"Alpha", "-0.002"},
                {"Beta", "0.411"},
                {"Annual Standard Deviation", "0.112"},
                {"Annual Variance", "0.013"},
                {"Information Ratio", "-0.352"},
                {"Tracking Error", "0.134"},
                {"Treynor Ratio", "0.073"},
                {"Total Fees", "$420.57"},
            };

            var optionSplitRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "2"},
                {"Average Win", "0.00%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "0.198%"},
                {"Drawdown", "0.500%"},
                {"Expectancy", "0"},
                {"Net Profit", "0.002%"},
                {"Sharpe Ratio", "0.609"},
                {"Loss Rate", "0%"},
                {"Win Rate", "100%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.013"},
                {"Beta", "0"},
                {"Annual Standard Deviation", "0.002"},
                {"Annual Variance", "0"},
                {"Information Ratio", "7.935"},
                {"Tracking Error", "6.787"},
                {"Treynor Ratio", "-4.913"},
                {"Total Fees", "$1.25"},
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
                {"Average Win", "1.68%"},
                {"Average Loss", "-0.77%"},
                {"Compounding Annual Return", "23.389%"},
                {"Drawdown", "1.900%"},
                {"Expectancy", "0.597"},
                {"Net Profit", "1.801%"},
                {"Sharpe Ratio", "1.884"},
                {"Loss Rate", "50%"},
                {"Win Rate", "50%"},
                {"Profit-Loss Ratio", "2.19"},
                {"Alpha", "-0.003"},
                {"Beta", "0.421"},
                {"Annual Standard Deviation", "0.087"},
                {"Annual Variance", "0.008"},
                {"Information Ratio", "-2.459"},
                {"Tracking Error", "0.094"},
                {"Treynor Ratio", "0.391"},
                {"Total Fees", "$23.05"},
            };

            var optionExerciseAssignRegressionAlgorithmStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "4"},
                {"Average Win", "0.30%"},
                {"Average Loss", "-0.32%"},
                {"Compounding Annual Return", "-85.023%"},
                {"Drawdown", "0.400%"},
                {"Expectancy", "-0.359"},
                {"Net Profit", "-0.350%"},
                {"Sharpe Ratio", "0"},
                {"Loss Rate", "67%"},
                {"Win Rate", "33%"},
                {"Profit-Loss Ratio", "0.92"},
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
                {"Compounding Annual Return", "244.780%"},
                {"Drawdown", "1.100%"},
                {"Expectancy", "0"},
                {"Net Profit", "4.153%"},
                {"Sharpe Ratio", "6.165"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.254"},
                {"Beta", "0.898"},
                {"Annual Standard Deviation", "0.14"},
                {"Annual Variance", "0.02"},
                {"Information Ratio", "4.625"},
                {"Tracking Error", "0.04"},
                {"Treynor Ratio", "0.963"},
                {"Total Fees", "$3.09"}
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
                {"Average Loss", "-2.01%"},
                {"Compounding Annual Return", "255.854%"},
                {"Drawdown", "6.600%"},
                {"Expectancy", "-0.016"},
                {"Net Profit", "1.401%"},
                {"Sharpe Ratio", "1.18"},
                {"Loss Rate", "33%"},
                {"Win Rate", "67%"},
                {"Profit-Loss Ratio", "0.48"},
                {"Alpha", "-1.135"},
                {"Beta", "1.08"},
                {"Annual Standard Deviation", "0.812"},
                {"Annual Variance", "0.66"},
                {"Information Ratio", "-8.445"},
                {"Tracking Error", "0.116"},
                {"Treynor Ratio", "0.888"},
                {"Total Fees", "$2039.69"}
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

            return new List<AlgorithmStatisticsTestParameters>
            {
                // CSharp
                new AlgorithmStatisticsTestParameters("BasicTemplateFuturesAlgorithmDaily", basicTemplateFuturesAlgorithmDailyStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("AddRemoveSecurityRegressionAlgorithm", addRemoveSecurityRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("BasicTemplateAlgorithm", basicTemplateStatistics, Language.CSharp),
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
