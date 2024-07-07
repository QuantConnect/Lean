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
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace QuantConnect
{
    /// <summary>
    /// Custom attribute used for documentation
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [DocumentationAttribute("Reference")]
    public sealed class DocumentationAttribute : Attribute
    {
        private static readonly DocumentationAttribute Attribute = typeof(DocumentationAttribute)
            .GetCustomAttributes<DocumentationAttribute>()
            .Single();
        private static readonly string BasePath = Attribute.FileName.Substring(
            0,
            Attribute.FileName.LastIndexOf("Common", StringComparison.Ordinal)
        );

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
        ///  The associated line of this attribute
        /// </summary>
        [JsonProperty(PropertyName = "line")]
        public int Line { get; }

        /// <summary>
        ///  The associated file name of this attribute
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; }

        /// <summary>
        /// The attributes type id, we override it to ignore it when serializing
        /// </summary>
        [JsonIgnore]
        public override object TypeId => base.TypeId;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public DocumentationAttribute(
            string tag,
            int weight = 0,
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
            [System.Runtime.CompilerServices.CallerFilePath] string fileName = ""
        )
        {
            Tag = tag;
            Line = line;
            Weight = weight;
            // will be null for the attribute of DocumentationAttribute itself
            if (BasePath != null)
            {
                FileName = fileName.Replace(
                    BasePath,
                    string.Empty,
                    StringComparison.InvariantCultureIgnoreCase
                );
            }
            else
            {
                FileName = fileName;
            }
        }
    }
}
