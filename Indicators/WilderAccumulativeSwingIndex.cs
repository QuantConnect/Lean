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

using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator calculates the Accumulative Swing Index (ASI) as defined by
    /// Welles Wilder in his book 'New Concepts in Technical Trading Systems'.
    /// <para>
    /// ASIₜ = ASIₜ₋₁ + SIₜ
    /// </para>
    /// <para>
    ///   Where:
    ///   <list type="bullet">
    ///     <item>
    ///       <term>ASIₜ₋₁</term>
    ///       <description>
    ///         The <see cref="WilderAccumulativeSwingIndex"/> for the previous period.
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <term>SIₜ</term>
    ///       <description>
    ///         The <see cref="WilderSwingIndex"/> calculated for the current period.
    ///       </description>
    ///     </item>
    ///   </list>
    /// </para>
    /// </summary>
    /// <seealso cref="WilderSwingIndex"/>
    public class WilderAccumulativeSwingIndex : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// The Swing Index (SI) used in calculating the Accumulative Swing Index.
        /// </summary>
        private readonly WilderSwingIndex _si;

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderAccumulativeSwingIndex"/> class using the specified name.
        /// </summary>
        /// <param name="limitMove">A decimal representing the limit move value for the period.</param>
        public WilderAccumulativeSwingIndex(decimal limitMove)
            : this("ASI", limitMove) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderAccumulativeSwingIndex"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="limitMove">A decimal representing the limit move value for the period.</param>
        public WilderAccumulativeSwingIndex(string name, decimal limitMove)
            : base(name)
        {
            _si = new WilderSwingIndex(limitMove);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized.
        /// </summary>
        public override bool IsReady => Samples > 1;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => 2;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            var isReady = _si.Update(input);

            if (isReady)
            {
                return IsReady ? Current.Value + _si.Current.Value : _si.Current.Value;
            }
            else
            {
                return 0m;
            }
        }

        /// <summary>
        /// Resets this indicator to its initial state.
        /// </summary>
        public override void Reset()
        {
            _si.Reset();
            base.Reset();
        }
    }
}
