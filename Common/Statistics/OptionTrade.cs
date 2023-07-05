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

namespace QuantConnect.Statistics
{
    /// <summary>
    /// Represents a closed option trade
    /// </summary>
    public class OptionTrade : Trade
    {
        /// <summary>
        /// Whether the option is in the money.
        /// </summary>
        public bool IsInTheMoney { get; set; }

        /// <summary>
        /// Checks whether the trade is a win or not.
        /// An option trade is a win if it is profitable or is In-The-Money (the option assignment is a win).
        /// </summary>
        /// <returns>True if the option trade is a win</returns>
        public override bool IsWin()
        {
            // TODO: How can we access the underlying price in order to check if the premium is less than the ITM amount?
            //var isWin = ProfitLoss > 0;
            //if (!isWin && IsInTheMoney)
            //{
            //    var option = security as Option.Option;
            //    var itmAmount = option.Holdings.GetQuantityValue(absoluteQuantityClosed, option.GetPayOff(option.Underlying.Price)).Amount;
            //    isWin = Math.Abs(lastTradeProfit) < itmAmount;
            //}

            return ProfitLoss > 0 || IsInTheMoney;
        }
    }
}
