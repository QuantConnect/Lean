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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator calculates the Swing Index (SI) as defined by Welles Wilder
    /// in his book 'New Concepts in Technical Trading Systems'.
    /// <para>
    /// SIₜ = 50 * ( N / R ) * ( K / T )
    /// </para>
    /// <para>
    ///   Where:
    ///   <list type="bullet">
    ///     <item>
    ///       <term>N</term>
    ///       <description>
    ///         Equals: Cₜ - Cₜ₋₁ + 0.5 * (Cₜ - Oₜ) + 0.25 * (Cₜ₋₁ - Oₜ₋₁)
    ///         <para>
    ///           <i>See <see cref="GetNValue"/></i>
    ///         </para>
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <term>R</term>
    ///       <description>
    ///         Found by selecting the expression with the largest value and
    ///         then using the corresponding formula.
    ///         <para>
    ///           Expression =&gt; Formula
    ///           <list type="number">
    ///             <item>
    ///               <description>
    ///                 |Hₜ - Cₜ₋₁| =&gt; |Hₜ - Cₜ| - 0.5 * |Lₜ - Cₜ₋₁| + 0.25 * |Cₜ₋₁ - Oₜ₋₁|
    ///               </description>
    ///             </item>
    ///             <item>
    ///               <description>
    ///                 |Lₜ - Cₜ₋₁| =&gt; |Lₜ - Cₜ| - 0.5 * |Hₜ - Cₜ₋₁| + 0.25 * |Cₜ₋₁ - Oₜ₋₁|
    ///               </description>
    ///             </item>
    ///             <item>
    ///               <description>
    ///                 |Hₜ - Lₜ| =&gt; |Hₜ - Lₜ₋₁| + 0.25 * |Cₜ₋₁ - Oₜ₋₁|
    ///               </description>
    ///             </item>
    ///           </list>
    ///         </para>
    ///         <para>
    ///           <i>See <see cref="GetRValue"/></i>
    ///         </para>
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <term>K</term>
    ///       <description>
    ///         Found by selecting the larger of the two expressions:
    ///         |Hₜ - Cₜ₋₁|, |Lₜ - Cₜ₋₁|
    ///         <para>
    ///           <i>See <see cref="GetKValue"/></i>
    ///         </para>
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <term>T</term>
    ///       <description>
    ///         The limit move, or the maximum change in price during the time
    ///         period for the bar. Passed as limitMove via the constructor.
    ///         <para>
    ///           <i>See <see cref="T"/></i>
    ///         </para>
    ///       </description>
    ///     </item>
    ///   </list>
    /// </para>
    /// </summary>
    /// <seealso cref="WilderAccumulativeSwingIndex"/>
    public class WilderSwingIndex : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Holds the bar for the current period.
        /// </summary>
        protected IBaseDataBar _currentInput { get; private set; }

        /// <summary>
        /// Holds the bar for the previous period.
        /// </summary>
        protected IBaseDataBar _previousInput { get; private set; }

        /// <summary>
        /// Gets the value for T (the limit move value) set in the constructor.
        /// </summary>
        private readonly decimal T;

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderSwingIndex"/> class using the specified name.
        /// </summary>
        /// <param name="name">A string for the name of this indicator.</param>
        /// <param name="limitMove">A decimal representing the limit move value for the period.</param>
        public WilderSwingIndex(string name, decimal limitMove)
            : base(name)
        {
            T = Math.Abs(limitMove);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderSwingIndex"/> class using the default name.
        /// </summary>
        /// <param name="limitMove">A decimal representing the limit move value for the period.</param>
        public WilderSwingIndex(decimal limitMove)
            : this("SI", limitMove)
        {
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
        /// Computes the next value of this indicator from the given state.
        /// </summary>
        /// <param name="input">The input given to the indicator.</param>
        /// <returns>A new value for this indicator.</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            if (!IsReady)
            {
                _currentInput = input;
                return 0m;
            }

            _previousInput = _currentInput;
            _currentInput = input;

            var N = GetNValue();
            var R = GetRValue();
            var K = GetKValue();

            if (R == decimal.Zero || T == decimal.Zero)
            {
                return 0m;
            }
            return 50m * (N / R) * (K / T);
        }

        /// <summary>
        /// Gets the value for N.
        /// <para>
        /// N = Cₜ - Cₜ₋₁ + 0.5 * (Cₜ - Oₜ) + 0.25 * (Cₜ₋₁ - Oₜ₋₁)
        /// </para>
        /// </summary>
        private decimal GetNValue()
        {
            return (_currentInput.Close - _previousInput.Close)
                + (0.5m * (_currentInput.Close - _currentInput.Open))
                + (0.25m * (_previousInput.Close - _previousInput.Open));
        }

        /// <summary>
        /// Gets the value for R, determined by using the formula corresponding
        /// to the expression with the largest value.
        /// <para>
        ///   <i>Expressions:</i>
        ///   <list type="number">
        ///     <item>
        ///       <description>|Hₜ - Cₜ₋₁|</description>
        ///     </item>
        ///     <item>
        ///       <description>|Lₜ - Cₜ₋₁|</description>
        ///     </item>
        ///     <item>
        ///       <description>|Hₜ - Lₜ|</description>
        ///     </item>
        ///   </list>
        /// </para>
        /// <para>
        ///   <i>Formulas:</i>
        ///   <list type="number">
        ///     <item>
        ///       <description>|Hₜ - Cₜ₋₁| - 0.5 * |Lₜ - Cₜ₋₁| + 0.25 * |Cₜ₋₁ - Oₜ₋₁|</description>
        ///     </item>
        ///     <item>
        ///       <description>|Lₜ - Cₜ₋₁| - 0.5 * |Hₜ - Cₜ₋₁| + 0.25 * |Cₜ₋₁ - Oₜ₋₁|</description>
        ///     </item>
        ///     <item>
        ///       <description>|Hₜ - Lₜ| + 0.25 * |Cₜ₋₁ - Oₜ₋₁|</description>
        ///     </item>
        ///   </list>
        /// </para>
        /// </summary>
        private decimal GetRValue()
        {
            var expressions = new decimal[]
            {
                Math.Abs(_currentInput.High - _previousInput.Close),
                Math.Abs(_currentInput.Low - _previousInput.Close),
                Math.Abs(_currentInput.High - _currentInput.Low)
            };

            int expressionIndex = Array.IndexOf(expressions, expressions.Max());

            decimal result;
            switch (expressionIndex)
            {
                case 0:
                    result = Math.Abs(_currentInput.High - _previousInput.Close)
                        - Math.Abs(0.5m * (_currentInput.Low - _previousInput.Close))
                        + Math.Abs(0.25m * (_previousInput.Close - _previousInput.Open));
                    break;

                case 1:
                    result = Math.Abs(_currentInput.Low - _previousInput.Close)
                        - Math.Abs(0.5m * (_currentInput.High - _previousInput.Close))
                        + Math.Abs(0.25m * (_previousInput.Close - _previousInput.Open));
                    break;

                case 2:
                    result = Math.Abs(_currentInput.High - _currentInput.Low)
                        + Math.Abs(0.25m * (_previousInput.Close - _previousInput.Open));
                    break;

                default:
                    result = 0m;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Gets the value for K, which is equal to the larger of the two following expressions.
        /// <para>
        ///   <i>Expressions:</i>
        ///   <list type="number">
        ///     <item>
        ///       <description>|Hₜ - Cₜ₋₁|</description>
        ///     </item>
        ///     <item>
        ///       <description>|Lₜ - Cₜ₋₁|</description>
        ///     </item>
        ///   </list>
        /// </para>
        /// </summary>
        private decimal GetKValue()
        {
            var values = new decimal[]
            {
                _currentInput.High - _previousInput.Close,
                _currentInput.Low - _previousInput.Close
            };

            return values.Max(x => Math.Abs(x));
        }
    }
}
