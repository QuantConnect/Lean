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
using System.Linq;
using System.Text.RegularExpressions;

namespace QuantConnect.Report.ReportElements
{
    public class ParametersReportElement : ReportElement
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
            int parameterIndex;
            var columns = (new Regex(@"{{\$KEY(\d+?)}}")).Matches(_template).Count;

            for (parameterIndex = 0; parameterIndex < _parameters.Count;)
            {
                var template = _template;
                int column;
                for (column = 0; column < columns; column++)
                {
                    var currTemplateKey = "{{$KEY" + column.ToString() + "}}";
                    var currTemplateValue = "{{$VALUE" + column.ToString() + "}}";

                    if (parameterIndex < _parameters.Count)
                    {
                        var parameter = _parameters.ElementAt(parameterIndex);
                        template = template.Replace(currTemplateKey, parameter.Key);
                        template = template.Replace(currTemplateValue, parameter.Value);
                    }
                    else
                    {
                        template = template.Replace(currTemplateKey, string.Empty);
                        template = template.Replace(currTemplateValue, string.Empty);
                    }

                    parameterIndex++;
                }

                if (column == 0)
                {
                    parameterIndex++;
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
