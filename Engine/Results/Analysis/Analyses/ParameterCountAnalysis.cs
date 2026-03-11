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
using QuantConnect.Lean.Engine.Results.Analysis.Utils;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>Warns when too many numeric parameters are detected in the algorithm.</summary>
    public class ParameterCountAnalysis : BaseBacktestAnalysis
    {
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

        public IReadOnlyList<BacktestAnalysisResult> Run(QCAlgorithm algorithm, Language language)
        { 
            var parametersCount = algorithm.GetParameters().Count;
            var result = parametersCount > 10 ? $"{parametersCount} Parameters Detected" : null;
            var potentialSolutions = result is not null ? PotentialSolutions(language) : [];
            return SingleResponse(new BacktestAnalysysContext(result), potentialSolutions);
        }

        private static List<string> PotentialSolutions(Language language) =>
        [
            "Using parameters is almost unavoidable, but a strategy trends toward being overfitted as more parameters get added or fine-tuned. " +
            "Try to remove some parameters to make the strategy more robust. " +
            $"The following table shows the criteria for parameters:{DetectedParametersTable}" +
            "The following table shows common expressions that are not parameters:" +
            GetNotParametersTable(language),
        ];

        private static string GetNotParametersTable(Language language) => $"""
| Non-Parameter Types | Example Instances |
|---------------------|-------------------|
| Common APIs | `{CodeByLanguage.SetStartDate[language]}`, `{CodeByLanguage.SetEndDate[language]}`, `{CodeByLanguage.SetCash[language]}`, etc. |
| Boolean Comparison | Testing for True or False conditions |
| String Numbers | Numbers formatted as part of `{CodeByLanguage.Log[language]}` or `{CodeByLanguage.Debug[language]}` method statements |
| Variable Names | Any variable names that use numbers as part of the name (for example, `smaIndicator200`) |
| Common Functions | Rounding, array indexing, boolean comparison using 1/0 for True/False, etc. |
""";
    }
}
