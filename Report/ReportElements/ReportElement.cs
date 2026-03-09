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

namespace QuantConnect.Report.ReportElements
{
    /// <summary>
    /// Common interface for template elements of the report
    /// </summary>
    public abstract class ReportElement : IReportElement
    {
        /// <summary>
        /// Name of this report element
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// Template key code.
        /// </summary>
        public virtual string Key { get; protected set; }

        /// <summary>
        /// Normalizes the key into a JSON-friendly key
        /// </summary>
        public string JsonKey => Key.Replace("KPI-", "").Replace("$", "").Replace("{", "").Replace("}", "") .ToLowerInvariant();

        /// <summary>
        /// Result of the render as an object for serialization to JSON
        /// </summary>
        public virtual object Result { get; protected set; }

        /// <summary>
        /// The generated output string to be injected
        /// </summary>
        public abstract string Render();
    }
}
