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
                {"Net Profit", "0%"},
                {"Sharpe Ratio", "4.411"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.752"},
                {"Beta", "0.186"},
                {"Annual Standard Deviation", "0.193"},
                {"Annual Variance", "0.037"},
                {"Information Ratio", "1.316"},
                {"Tracking Error", "0.246"},
                {"Treynor Ratio", "4.572"},
                {"Total Fees", "$3.09"}
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
                {"Alpha", "0.051"},
                {"Beta", "0.002"},
                {"Annual Standard Deviation", "0.03"},
                {"Annual Variance", "0.001"},
                {"Information Ratio", "-2.451"},
                {"Tracking Error", "0.194"},
                {"Treynor Ratio", "29.506"},
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
                {"Alpha", "-0.076"},
                {"Beta", "0.039"},
                {"Annual Standard Deviation", "0.056"},
                {"Annual Variance", "0.003"},
                {"Information Ratio", "-2.167"},
                {"Tracking Error", "0.112"},
                {"Treynor Ratio", "-1.755"},
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
                {"Alpha", "-0.023"},
                {"Beta", "0.001"},
                {"Annual Standard Deviation", "0.001"},
                {"Annual Variance", "0"},
                {"Information Ratio", "-4.203"},
                {"Tracking Error", "0.174"},
                {"Treynor Ratio", "-33.666"},
                {"Total Fees", "$5433.00"}
            };

            var universeSelectionRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "4"},
                {"Average Win", "0.70%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "-56.034%"},
                {"Drawdown", "3.800%"},
                {"Expectancy", "0"},
                {"Net Profit", "-3.755%"},
                {"Sharpe Ratio", "-3.629"},
                {"Loss Rate", "0%"},
                {"Win Rate", "100%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.643"},
                {"Beta", "0.684"},
                {"Annual Standard Deviation", "0.173"},
                {"Annual Variance", "0.03"},
                {"Information Ratio", "-3.927"},
                {"Tracking Error", "0.166"},
                {"Treynor Ratio", "-0.918"},
                {"Total Fees", "$2.00"}
            };

            var customDataRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "155.210%"},
                {"Drawdown", "99.900%"},
                {"Expectancy", "0"},
                {"Net Profit", "0%"},
                {"Sharpe Ratio", "0.453"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "48.714"},
                {"Beta", "50.259"},
                {"Annual Standard Deviation", "118.922"},
                {"Annual Variance", "14142.47"},
                {"Information Ratio", "0.452"},
                {"Tracking Error", "118.917"},
                {"Treynor Ratio", "1.072"},
                {"Total Fees", "$0.00"}
            };

            var addRemoveSecurityRegressionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "5"},
                {"Average Win", "0.49%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "307.953%"},
                {"Drawdown", "1.400%"},
                {"Expectancy", "0"},
                {"Net Profit", "1.814%"},
                {"Sharpe Ratio", "6.475"},
                {"Loss Rate", "0%"},
                {"Win Rate", "100%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.906"},
                {"Beta", "0.018"},
                {"Annual Standard Deviation", "0.141"},
                {"Annual Variance", "0.02"},
                {"Information Ratio", "1.649"},
                {"Tracking Error", "0.236"},
                {"Treynor Ratio", "50.468"},
                {"Total Fees", "$25.21"}
            };

            var dropboxBaseDataUniverseSelectionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "67"},
                {"Average Win", "1.07%"},
                {"Average Loss", "-0.69%"},
                {"Compounding Annual Return", "17.697%"},
                {"Drawdown", "5.100%"},
                {"Expectancy", "0.776"},
                {"Net Profit", "17.697%"},
                {"Sharpe Ratio", "1.379"},
                {"Loss Rate", "30%"},
                {"Win Rate", "70%"},
                {"Profit-Loss Ratio", "1.55"},
                {"Alpha", "0.151"},
                {"Beta", "-0.073"},
                {"Annual Standard Deviation", "0.099"},
                {"Annual Variance", "0.01"},
                {"Information Ratio", "-0.507"},
                {"Tracking Error", "0.146"},
                {"Treynor Ratio", "-1.871"},
                {"Total Fees", "$300.29"}
            };

            var dropboxUniverseSelectionStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "49"},
                {"Average Win", "1.58%"},
                {"Average Loss", "-1.03%"},
                {"Compounding Annual Return", "21.280%"},
                {"Drawdown", "8.200%"},
                {"Expectancy", "0.646"},
                {"Net Profit", "21.280%"},
                {"Sharpe Ratio", "1.363"},
                {"Loss Rate", "35%"},
                {"Win Rate", "65%"},
                {"Profit-Loss Ratio", "1.52"},
                {"Alpha", "0.178"},
                {"Beta", "-0.071"},
                {"Annual Standard Deviation", "0.12"},
                {"Annual Variance", "0.014"},
                {"Information Ratio", "-0.297"},
                {"Tracking Error", "0.161"},
                {"Treynor Ratio", "-2.319"},
                {"Total Fees", "$233.07"}
            };

            var parameterizedStatistics = new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "278.616%"},
                {"Drawdown", "0.300%"},
                {"Expectancy", "0"},
                {"Net Profit", "0%"},
                {"Sharpe Ratio", "11.017"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.764"},
                {"Beta", "0.186"},
                {"Annual Standard Deviation", "0.078"},
                {"Annual Variance", "0.006"},
                {"Information Ratio", "1.957"},
                {"Tracking Error", "0.171"},
                {"Treynor Ratio", "4.634"},
                {"Total Fees", "$3.09"},
            };

            return new List<AlgorithmStatisticsTestParameters>
            {
                // CSharp
                new AlgorithmStatisticsTestParameters("AddRemoveSecurityRegressionAlgorithm", addRemoveSecurityRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("BasicTemplateAlgorithm", basicTemplateStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("CustomDataRegressionAlgorithm", customDataRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("DropboxBaseDataUniverseSelectionAlgorithm", dropboxBaseDataUniverseSelectionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("DropboxUniverseSelectionAlgorithm", dropboxUniverseSelectionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("LimitFillRegressionAlgorithm", limitFillRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("ParameterizedAlgorithm", parameterizedStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("RegressionAlgorithm", regressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("UniverseSelectionRegressionAlgorithm", universeSelectionRegressionStatistics, Language.CSharp),
                new AlgorithmStatisticsTestParameters("UpdateOrderRegressionAlgorithm", updateOrderRegressionStatistics, Language.CSharp),

                // FSharp
                new AlgorithmStatisticsTestParameters("BasicTemplateAlgorithm", basicTemplateStatistics, Language.FSharp),

                // VisualBasic
                new AlgorithmStatisticsTestParameters("BasicTemplateAlgorithm", basicTemplateStatistics, Language.VisualBasic),
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