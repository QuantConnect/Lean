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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Defines different types of option pricing model
    /// </summary>
    public enum OptionPricingModelType
    {
        /// <summary>
        /// Vanilla Black Scholes Model
        /// </summary>
        /// <remarks>Preferred on calculating greeks for European options, and IV for all options</remarks>
        BlackScholes,
        /// <summary>
        /// The Cox-Ross-Rubinstein binomial tree model (CRR model)
        /// </summary>
        /// <remarks>Preferred on calculating greeks for American options</remarks>
        BinomialCoxRossRubinstein,
        /// <summary>
        /// The forward binomial tree model, or Cox-Ross-Rubinstein with drift model
        /// </summary>
        /// <remarks>Preferred on replicating IB IV for American options</remarks>
        ForwardTree
    }
}
