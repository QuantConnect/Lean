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
    /// This indicator is capable of wiring up two separate indicators into a single indicator
    /// such that the output of each will be sent to a user specified function.
    /// </summary>
    /// <typeparam name="T">The type of data input into this indicator</typeparam>
    public class CompositeIndicator<T> : IndicatorBase<T>
        where T : BaseData
    {
        /// <summary>
        /// Delegate type used to compose the output of two indicators into a new value.
        /// </summary>
        /// <remarks>
        /// A simple example would be to compute the difference between the two indicators (such as with MACD)
        /// (left, right) => left - right
        /// </remarks>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public delegate decimal IndicatorComposer(IndicatorBase<T> left, IndicatorBase<T> right);

        /// <summary>function used to compose the individual indicators</summary>
        private readonly IndicatorComposer _composer;

        /// <summary>
        /// Gets the 'left' indicator for the delegate
        /// </summary>
        public IndicatorBase<T> Left { get; private set; }

        /// <summary>
        /// Gets the 'right' indicator for the delegate
        /// </summary>
        public IndicatorBase<T> Right { get; private set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return Left.IsReady && Right.IsReady; }
        }

        /// <summary>
        /// Creates a new CompositeIndicator capable of taking the output from the left and right indicators
        /// and producing a new value via the composer delegate specified
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="left">The left indicator for the 'composer'</param>
        /// <param name="right">The right indidcator for the 'composoer'</param>
        /// <param name="composer">Function used to compose the left and right indicators</param>
        public CompositeIndicator(string name, IndicatorBase<T> left, IndicatorBase<T> right, IndicatorComposer composer)
            : base(name)
        {
            _composer = composer;
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Creates a new CompositeIndicator capable of taking the output from the left and right indicators
        /// and producing a new value via the composer delegate specified
        /// </summary>
        /// <param name="left">The left indicator for the 'composer'</param>
        /// <param name="right">The right indidcator for the 'composoer'</param>
        /// <param name="composer">Function used to compose the left and right indicators</param>
        public CompositeIndicator(IndicatorBase<T> left, IndicatorBase<T> right, IndicatorComposer composer)
            : base(string.Format("COMPOSE({0},{1})", left.Name, right.Name))
        {
            _composer = composer;
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(T input)
        {
            Left.Update(input);
            Right.Update(input);
            return _composer.Invoke(Left, Right);
        }
    }
}