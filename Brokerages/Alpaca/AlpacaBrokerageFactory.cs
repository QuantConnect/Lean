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
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Alpaca
{
    /// <summary>
    /// Provides an implementations of <see cref="IBrokerageFactory"/> that produces a <see cref="AlpacaBrokerage"/>
    /// </summary>
    public class AlpacaBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlpacaBrokerageFactory"/> class.
        /// </summary>
        public AlpacaBrokerageFactory()
            : base(typeof(AlpacaBrokerage))
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// Gets the brokerage data required to run the brokerage from configuration/disk
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>
        {
            { "alpaca-key-id", Config.Get("alpaca-key-id") },
            { "alpaca-secret-key", Config.Get("alpaca-secret-key") },
            { "alpaca-trading-mode", Config.Get("alpaca-trading-mode") }
        };

        /// <summary>
        /// Gets a new instance of the <see cref="AlpacaBrokerageModel"/>
        /// </summary>
        public override IBrokerageModel BrokerageModel => new AlpacaBrokerageModel();

        /// <summary>
        /// Creates a new <see cref="IBrokerage"/> instance
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();

            // read values from the brokerage data
            var keyId = Read<string>(job.BrokerageData, "alpaca-key-id", errors);
            var secretKey = Read<string>(job.BrokerageData, "alpaca-secret-key", errors);
            var tradingMode = Read<string>(job.BrokerageData, "alpaca-trading-mode", errors);

            if (errors.Count != 0)
            {
                // if we had errors then we can't create the instance
                throw new Exception(string.Join(Environment.NewLine, errors));
            }

            tradingMode = tradingMode.ToLowerInvariant();
            if (!tradingMode.Equals("live") && !tradingMode.Equals("paper"))
            {
                // if the trading mode is invalid, do not proceed further
                throw new Exception("Available trading mode: paper/live");
            }

            var brokerage = new AlpacaBrokerage(algorithm.Transactions, algorithm.Portfolio, keyId, secretKey, tradingMode);
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }

    }
}
