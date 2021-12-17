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

using System;
using Newtonsoft.Json;

namespace QuantConnect.Packets
{
    /// <summary>
    /// A debugging breakpoint
    /// </summary>
    [Obsolete("Breakpoint is Deprecated, we have moved to using VSCode + NetCoreDbg for C# and PTVSD for Python so no longer need to track breakpoints in packets")]
    public class Breakpoint
    {
        /// <summary>
        /// The file name
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// The line number
        /// </summary>
        [JsonProperty(PropertyName = "lineNumber")]
        public int LineNumber { get; set; }
    }
}
