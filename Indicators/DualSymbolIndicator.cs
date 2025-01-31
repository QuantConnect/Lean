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

using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Base class for indicators that work with two different symbols and calculate an indicator based on them.
    /// </summary>
    /// <typeparam name="TInput">Indicator input data type</typeparam>
    public abstract class DualSymbolIndicator<TInput> : MultiSymbolIndicator<TInput>
        where TInput : IBaseData
    {
        /// <summary>
        /// RollingWindow to store the data points of the target symbol
        /// </summary>
        protected IReadOnlyWindow<TInput> TargetDataPoints { get; }

        /// <summary>
        /// RollingWindow to store the data points of the reference symbol
        /// </summary>
        protected IReadOnlyWindow<TInput> ReferenceDataPoints { get; }

        /// <summary>
        /// Symbol of the reference used
        /// </summary>
        protected Symbol ReferenceSymbol { get; }

        /// <summary>
        /// Symbol of the target used
        /// </summary>
        protected Symbol TargetSymbol { get; }

        /// <summary>
        /// Initializes the dual symbol indicator.
        /// <para>
        /// The constructor accepts a target symbol and a reference symbol. It also initializes
        /// the time zones for both symbols and checks if they are different.
        /// </para>
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="targetSymbol">The symbol of the target asset.</param>
        /// <param name="referenceSymbol">The symbol of the reference asset.</param>
        /// <param name="period">The period (number of data points) over which to calculate the indicator.</param>
        protected DualSymbolIndicator(string name, Symbol targetSymbol, Symbol referenceSymbol, int period)
            : base(name, [targetSymbol, referenceSymbol], period)
        {
            TargetDataPoints = DataBySymbol[targetSymbol].DataPoints;
            ReferenceDataPoints = DataBySymbol[referenceSymbol].DataPoints;
            TargetSymbol = targetSymbol;
            ReferenceSymbol = referenceSymbol;
        }
    }
}
