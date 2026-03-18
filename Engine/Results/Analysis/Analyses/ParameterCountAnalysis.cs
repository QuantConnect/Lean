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
 *
*/
using System.Collections.Generic;
using QuantConnect.Algorithm;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Warns when too many numeric parameters are detected in the algorithm.
    /// </summary>
    public class ParameterCountAnalysis : BaseResultsAnalysis
    {
        public override string Issue { get; } = "The algorithm has so many numeric parameters it's at risk of overfitting.";

        public override int Weight { get; } = 12;
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Algorithm, parameters.Language);

        private const string DetectedParametersTable = """
| Parameter Types | Example Instances |
|-|-|
| Numeric Comparison | Numeric operators used to compare numeric arguments: <= < > >= |
| Time Span | Setting the interval of `TimeSpan` or `timedelta` |
| Order Event | Inputting numeric arguments when placing orders |
| Scheduled Event | Inputting numeric arguments when scheduling an algorithm event to occur |
| Variable Assignment | Assigning numeric values to variables |
| Mathematical Operation | Any mathematical operation involving explicit numbers |
| Lean API | Numeric arguments passed to Indicators, Consolidators, Rolling Windows, etc. |
""";

        /// <summary>
        /// Counts the algorithm's parameters and flags the backtest when more than 10 are detected.
        /// </summary>
        /// <param name="algorithm">The algorithm instance whose parameters are inspected.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>Analysis results when the parameter count exceeds the threshold.</returns>
        public IReadOnlyList<AnalysisResult> Run(QCAlgorithm algorithm, Language language)
        {
            var parametersCount = algorithm.GetParameters().Count;
            var result = parametersCount > 10 ? $"{parametersCount} Parameters Detected" : null;
            var potentialSolutions = result is not null ? Solutions(language) : [];
            return SingleResponse(new ResultsAnalysisContext(result), potentialSolutions);
        }

        private static List<string> Solutions(Language language) =>
        [
            "Try to remove some parameters to make the strategy more robust. " +
            $"The following table shows the criteria for parameters:{DetectedParametersTable}" +
            "The following table shows common expressions that are not parameters:" +
            GetNotParametersTable(language),
        ];

        private static string GetNotParametersTable(Language language) => $"""
| Non-Parameter Types | Example Instances |
|---------------------|-------------------|
| Common APIs | `{FormatCode(nameof(QCAlgorithm.SetStartDate), language)}`, `{FormatCode(nameof(QCAlgorithm.SetEndDate), language)}`, `{FormatCode(nameof(QCAlgorithm.SetCash), language)}`, etc. |
| Boolean Comparison | Testing for True or False conditions |
| String Numbers | Numbers formatted as part of `{FormatCode(nameof(QCAlgorithm.Log), language)}` or `{FormatCode(nameof(QCAlgorithm.Debug), language)}` method statements |
| Variable Names | Any variable names that use numbers as part of the name (for example, `smaIndicator200`) |
| Common Functions | Rounding, array indexing, boolean comparison using 1/0 for True/False, etc. |
""";
    }
}
