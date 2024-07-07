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

using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Represents a command to add a security to the algorithm
    /// </summary>
    public class AddSecurityCommand : BaseCommand
    {
        /// <summary>
        /// The security type of the security
        /// </summary>
        public SecurityType SecurityType { get; set; }

        /// <summary>
        /// The security's ticker symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// The requested resolution, defaults to Resolution.Minute
        /// </summary>
        public Resolution Resolution { get; set; }

        /// <summary>
        /// The security's market, defaults to <see cref="QuantConnect.Market.USA"/> except for Forex, defaults to <see cref="QuantConnect.Market.FXCM"/>
        /// </summary>
        public string Market { get; set; }

        /// <summary>
        /// The fill forward behavior, true to fill forward, false otherwise - defaults to true
        /// </summary>
        public bool FillDataForward { get; set; }

        /// <summary>
        /// The leverage for the security, defaults to 2 for equity, 50 for forex, and 1 for everything else
        /// </summary>
        public decimal Leverage { get; set; }

        /// <summary>
        /// The extended market hours flag, true to allow pre/post market data, false for only in market data
        /// </summary>
        public bool ExtendedMarketHours { get; set; }

        /// <summary>
        /// Default construct that applies default values
        /// </summary>
        public AddSecurityCommand()
        {
            Resolution = Resolution.Minute;
            Market = null;
            FillDataForward = true;
            Leverage = Security.NullLeverage;
            ExtendedMarketHours = false;
        }

        /// <summary>
        /// Runs this command against the specified algorithm instance
        /// </summary>
        /// <param name="algorithm">The algorithm to run this command against</param>
        public override CommandResultPacket Run(IAlgorithm algorithm)
        {
            var security = algorithm.AddSecurity(
                SecurityType,
                Symbol,
                Resolution,
                Market,
                FillDataForward,
                Leverage,
                ExtendedMarketHours
            );
            return new Result(this, true, security.Symbol);
        }

        /// <summary>
        /// Result packet type for the <see cref="AddSecurityCommand"/> command
        /// </summary>
        public class Result : CommandResultPacket
        {
            /// <summary>
            /// The symbol result from the add security command
            /// </summary>
            public Symbol Symbol { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Result"/> class
            /// </summary>
            public Result(AddSecurityCommand command, bool success, Symbol symbol)
                : base(command, success)
            {
                Symbol = symbol;
            }
        }
    }
}
