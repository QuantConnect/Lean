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

using System;
using Newtonsoft.Json;

namespace QuantConnect
{
    /// <summary>
    /// Custom attribute used for documentation
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class DocumentationAttribute : Attribute
    {
        /// <summary>
        /// The documentation tag
        /// </summary>
        [JsonProperty(PropertyName = "tag")]
        public string Tag { get; }

        /// <summary>
        ///  The associated weight of this attribute and tag
        /// </summary>
        [JsonProperty(PropertyName = "weight")]
        public int Weight { get; }

        /// <summary>
        /// The attributes type id, we override it to ignore it when serializing
        /// </summary>
        [JsonIgnore]
        public override object TypeId => base.TypeId;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public DocumentationAttribute(string tag, int weight)
        {
            Tag = tag;
            Weight = weight;
        }
    }
}
