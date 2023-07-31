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
using System.IO;
using System.Linq;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class ParametersReportElement : ReportElement
    {
        private IReadOnlyDictionary<string, string> _parameters;
        private readonly string _template;

        /// <summary>
        /// Creates a two column table for the Algorithm's Parameters
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtestConfiguration">The configuration of the backtest algorithm</param>
        /// <param name="liveConfiguration">The configuration of the live algorithm</param>
        /// <param name="template">HTML template to use</param>
        public ParametersReportElement(string name, string key, AlgorithmConfiguration backtestConfiguration, AlgorithmConfiguration liveConfiguration, string template)
        {
            Name = name;
            Key = key;
            _template = template;

            if (liveConfiguration != null)
            {
                _parameters = liveConfiguration.Parameters;
            }
            else if (backtestConfiguration != null)
            {
                _parameters = backtestConfiguration.Parameters;
            }
            else
            {
                // This case only happens for the unit tests, then we just create an empty dictionary
                _parameters = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Generates a HTML two column table for the Algorithm's Parameters
        /// </summary>
        /// <returns>Returns a string representing a HTML two column table</returns>
        public override string Render()
        {
            var items = new List<string>();

            for (int index = 0; index < _parameters.Count; index += 2)
            {
                var template = _template;
                var firstKVP = _parameters.ElementAt(index);
                var firstKey = string.Join(" ", (firstKVP.Key).Split("-").Select(x => x[0].ToString().ToUpper() + x.Substring(1)));
                template = template.Replace("{{$FIRST-KPI-NAME}}", firstKey);
                template = template.Replace("{{$FIRST-KPI-VALUE}}", firstKVP.Value);
                if ((index + 1) < _parameters.Count)
                {
                    var secondKVP = _parameters.ElementAt(index + 1);
                    var secondKey = string.Join(" ", (secondKVP.Key).Split("-").Select(x => x[0].ToString().ToUpper() + x.Substring(1)));
                    template = template.Replace("{{$SECOND-KPI-NAME}}", secondKey);
                    template = template.Replace("{{$SECOND-KPI-VALUE}}", secondKVP.Value);
                }
                items.Add(template);
            }

            if (Key == ReportKey.ParametersPageStyle)
            {
                if (items.Count == 0)
                {
                    return "display: none;";
                }

                return string.Empty;
            }

            var parameters= string.Join("\n", items);
            return parameters;
        }
    }
}
