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
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Immutable result returned by every test. Mirrors the Python dict
    /// <c>{'name': ..., 'result': ..., 'potentialSolutions': [...]}</c>.
    /// </summary>
    public class BacktestAnalysisResult
    {
        public string Name { get; set; }

        public object Result { get; set; }
        
        public List<string> PotentialSolutions { get; set; }

        public BacktestAnalysisResult(string name, object result, List<string> potentialSolutions)
        {
            Name = name;
            Result = result;
            PotentialSolutions = potentialSolutions;
        }
    }


    /// <summary>
    /// Abstract base class for all backtest diagnostic tests.
    /// Mirrors <c>tests/base.py : BacktestResultAnalysis</c>.
    /// </summary>
    public abstract class BaseBacktestAnalysis
    {
        // ── Factory helpers ───────────────────────────────────────────────────────

        protected IReadOnlyList<BacktestAnalysisResult> SingleResponse(object result, List<string> potentialSolutions = null)
            => [CreateResponse(result, potentialSolutions)];

        protected BacktestAnalysisResult CreateResponse(object result, List<string> potentialSolutions = null)
            => new(GetType().Name, result, potentialSolutions ?? []);

        /// <summary>
        /// Filters <paramref name="responses"/> to those with solutions,
        /// prefixes the class name, and returns a flat list.
        /// </summary>
        protected IReadOnlyList<BacktestAnalysisResult> CreateAggregatedResponse(IEnumerable<BacktestAnalysisResult> responses)
            => responses
                .Where(x => x.PotentialSolutions.Count > 0)
                .Select(x => new BacktestAnalysisResult(GetType().Name + " / " + x.Name, x.Result, x.PotentialSolutions))
                .ToList();

        // ── Pretty-print ──────────────────────────────────────────────────────────

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };

        /// <summary>
        /// Renders a list of test results as Markdown, matching the Python
        /// <c>pretty_print()</c> output format exactly.
        /// </summary>
        public static string PrettyPrint(IEnumerable<BacktestAnalysisResult> results)
        {
            var sb = new StringBuilder();
            foreach (var r in results)
            {
                sb.AppendLine($"# {r.Name}");
                sb.AppendLine("### Result");
                sb.AppendLine("```");
                sb.AppendLine(JsonSerializer.Serialize(r.Result, JsonOptions));
                sb.AppendLine("```");

                if (r.PotentialSolutions.Count > 0)
                {
                    sb.AppendLine("### Potential Solutions");
                    for (int i = 0; i < r.PotentialSolutions.Count; i++)
                    {
                        sb.AppendLine($"#### Solution {i + 1}");
                        sb.AppendLine(r.PotentialSolutions[i]);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
