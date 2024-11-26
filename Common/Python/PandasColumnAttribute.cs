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

namespace QuantConnect.Python
{
    /// <summary>
    /// Attribute to rename a property or field when converting an instance to a pandas DataFrame row.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PandasColumnAttribute : Attribute
    {
        /// <summary>
        /// The name of the column in the pandas DataFrame.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PandasColumnAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the column in the pandas DataFrame</param>
        public PandasColumnAttribute(string name)
        {
            Name = name;
        }
    }
}
