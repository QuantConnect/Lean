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


namespace QuantConnect.Indicators {

    /// <summary>
    ///     The Money Flow Index (MFI) is an oscillator that uses both price and volume to 
    ///     measure buying and selling pressure
    ///     
    ///     Typical Price = (High + Low + Close)/3
    ///     Money Flow = Typical Price x Volume
    ///     Positve Money Flow = Sum of the money flows of all days where the typical 
    ///         price is greater than the previous day's typical price
    ///     Negative Money Flow = Sum of the money flows of all days where the typical 
    ///         price is less than the previous day's typical price
    ///     Money Flow Ratio = (14-period Positive Money Flow)/(14-period Negative Money Flow)
    ///     
    ///     Money Flow Index = 100 x  Positve Money Flow / ( Positve Money Flow + Negative Money Flow)
    /// </summary>
    public class MoneyFlowIndex : TradeBarIndicator {

        /// <summary>Keep track of postive money flow for postive money flow sum</summary>
        public RollingWindow<decimal> _positiveMoneyFlowWindow { get; private set; }

        /// <summary>Keep track of negative money flow for negative money flow sum</summary>
        public RollingWindow<decimal> _negativeMoneyFlowWindow { get; private set; }

        /// <summary>The sum of positive money flow to compute money flow ratio</summary>
        public decimal _positiveMoneyFlowSum { get; private set; }

        /// <summary>The sum of negative money flow to compute money flow ratio</summary>
        public decimal _negativeMoneyFlowSum { get; private set; }

        /// <summary>The current and previous typical price is used to determine postive or negative money flow</summary>
        public decimal _previousTypicalPrice { get; private set; }


        /// <summary>
        /// Initializes a new instance of the MoneyFlowIndex class
        /// </summary>
        /// <param name="period">The period of the negative and postive money flow</param>
        public MoneyFlowIndex(int period)
            : this("MFI" + period, period) 
        {
        }

        /// <summary>
        /// Initializes a new instance of the MoneyFlowIndex class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the negative and postive money flow</param>
        public MoneyFlowIndex(string name, int period)
            : base(name) {
            _positiveMoneyFlowWindow = new RollingWindow<decimal>(period);
            _negativeMoneyFlowWindow = new RollingWindow<decimal>(period);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady {
            get { return _positiveMoneyFlowWindow.IsReady && _negativeMoneyFlowWindow.IsReady; }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input) {
            if (!IsReady) {
                return 50.0m;
            }

            // Compute typical price and raw money flow
            decimal typicalPrice = (input.High + input.Low + input.Close) / 3.0m;
            decimal moneyFlow = typicalPrice * input.Volume;

            // Compute positive money flow sum
            decimal positiveMoneyFlow = typicalPrice > _previousTypicalPrice ? moneyFlow : 0.0m;
            _positiveMoneyFlowSum += positiveMoneyFlow;
            _positiveMoneyFlowWindow.Add(positiveMoneyFlow);
            _positiveMoneyFlowSum -= _positiveMoneyFlowWindow.MostRecentlyRemoved;

            // Compute negative money flow sum
            decimal negativeMoneyFlow = typicalPrice < _previousTypicalPrice ? moneyFlow : 0.0m;
            _negativeMoneyFlowSum += negativeMoneyFlow;
            _negativeMoneyFlowWindow.Add(negativeMoneyFlow);
            _negativeMoneyFlowSum -= _negativeMoneyFlowWindow.MostRecentlyRemoved;

            // Update previous typical price
            _previousTypicalPrice = typicalPrice;

            return 100m * _positiveMoneyFlowSum / (_positiveMoneyFlowSum + _negativeMoneyFlowSum);
        }

    }
}

