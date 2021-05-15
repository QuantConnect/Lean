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

using System;
using QuantConnect.Packets;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Console setup handler to initialize and setup the Lean Engine properties for a local backtest
    /// Kept for backwards compatibility, BacktestingSetupHandler is the same implementation
    /// </summary>
    public class ConsoleSetupHandler : BacktestingSetupHandler
    {
        /// <summary>
        /// Setup the algorithm data, cash, job start end date etc:
        /// </summary>
        public ConsoleSetupHandler()
        {
            StartingPortfolioValue = 0;
            StartingDate = new DateTime(1998, 01, 01);
            Errors = new List<Exception>();
        }

        /// <summary>
        /// Resolve max orders for this algorithm
        /// </summary>
        /// <param name="job"></param>
        protected override int GetMaximumOrders(BacktestNodePacket job)
        {
            // For local backtest MaxOrders is always max int
            return int.MaxValue;
        }

        /// <summary>
        /// Get max runtime for this job
        /// </summary>
        /// <param name="parameters">Maximum runtime for this job</param>
        /// <returns></returns>
        protected override TimeSpan GetMaximumRuntime(SetupHandlerParameters parameters)
        {
            // Return a seriously long time (10 years)
            return TimeSpan.FromDays(10 * 365);
        }
    }
}
