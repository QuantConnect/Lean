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
    /// Base command implementation
    /// </summary>
    public abstract class BaseCommand : ICommand
    {
        /// <summary>
        /// Unique command id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Runs this command against the specified algorithm instance
        /// </summary>
        /// <param name="algorithm">The algorithm to run this command against</param>
        public abstract CommandResultPacket Run(IAlgorithm algorithm);

        /// <summary>
        /// Creats symbol using symbol properties.
        /// </summary>
        /// <param name="ticker">The string ticker symbol</param>
        /// <param name="securityType">The security type of the ticker. If securityType == Option, then a canonical symbol is created</param>
        /// <param name="market">The market the ticker resides in</param>
        /// <param name="symbol">The algorithm to run this command against</param>
        /// <exception cref="ArgumentException">If symbol is null or symbol can't be created with given args</exception>
        protected Symbol GetSymbol(
            string ticker,
            SecurityType securityType,
            string market,
            Symbol symbol = null
        )
        {
            if (symbol != null)
            {
                // No need to create symbol if alrady exists
                return symbol;
            }
            if (
                ticker != null
                && (securityType != null && securityType != SecurityType.Base)
                && market != null
            )
            {
                return Symbol.Create(ticker, securityType, market);
            }
            else
            {
                throw new ArgumentException(
                    $"BaseCommand.GetSymbol(): {Messages.BaseCommand.MissingValuesToGetSymbol}"
                );
            }
        }
    }
}
