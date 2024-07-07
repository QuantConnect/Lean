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

namespace QuantConnect.Securities
{
    /// <summary>
    /// The responsability of this model is to apply margin interest rate cash flows to the portfolio
    /// </summary>
    public interface IMarginInterestRateModel
    {
        /// <summary>
        /// Apply margin interest rates to the portfolio
        /// </summary>
        /// <param name="marginInterestRateParameters">The parameters to use</param>
        void ApplyMarginInterestRate(MarginInterestRateParameters marginInterestRateParameters);
    }

    /// <summary>
    /// Provides access to a null implementation for <see cref="IMarginInterestRateModel"/>
    /// </summary>
    public static class MarginInterestRateModel
    {
        /// <summary>
        /// The null margin interest rate model
        /// </summary>
        public static readonly IMarginInterestRateModel Null = new NullMarginInterestRateModel();

        private sealed class NullMarginInterestRateModel : IMarginInterestRateModel
        {
            public void ApplyMarginInterestRate(
                MarginInterestRateParameters marginInterestRateParameters
            ) { }
        }
    }
}
