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
using QuantConnect.Interfaces;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Represents a command that will liquidate the entire algorithm
    /// </summary>
    public class LiquidateCommand : BaseCommand
    {
        /// <summary>
        /// Gets or sets the string ticker symbol
        /// </summary>
        public string Ticker { get; set; }

        /// <summary>
        /// Gets or sets the security type of the ticker.
        /// </summary>
        public SecurityType SecurityType { get; set; }

        /// <summary>
        /// Gets or sets the market the ticker resides in
        /// </summary>
        public string Market { get; set; }

        /// <summary>
        /// Submits orders to liquidate all current holdings in the algorithm
        /// </summary>
        /// <param name="algorithm">The algorithm to be liquidated</param>
        public override CommandResultPacket Run(IAlgorithm algorithm)
        {
            if (Ticker != null || SecurityType != SecurityType.Base || Market != null)
            {
                var symbol = GetSymbol(Ticker, SecurityType, Market);
                algorithm.Liquidate(symbol);
            }
            else
            {
                algorithm.Liquidate();
            }
            return new CommandResultPacket(this, true);
        }
    }
}
