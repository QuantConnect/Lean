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

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Provides a marker interface allowing models to define their own names.
    /// If not specified, the framework will use the model's type name.
    /// Implementation of this is not required unless you plan on running multiple models
    /// of the same type w/ different parameters.
    /// </summary>
    public interface INamedModel
    {
        /// <summary>
        /// Defines a name for a framework model
        /// </summary>
        string Name { get; }
    }
}