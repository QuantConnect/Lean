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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Api
{
    /// <summary>
    /// Response from the compiler on a build event
    /// </summary>
    public class Compile : RestResponse
    {
        /// <summary>
        /// Compile Id for a sucessful build
        /// </summary>
        public string CompileId { get; set; }

        /// <summary>
        /// True on successful compile
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CompileState State { get; set; }

        /// <summary>
        /// Logs of the compilation request
        /// </summary>
        public List<string> Logs { get; set; }

        /// <summary>
        /// Project Id we sent for compile
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        /// Signature key of compilation
        /// </summary>
        public string Signature {  get; set; }

        /// <summary>
        /// Signature order of files to be compiled
        /// </summary>
        public List<string> SignatureOrder { get; set; }
    }
}
