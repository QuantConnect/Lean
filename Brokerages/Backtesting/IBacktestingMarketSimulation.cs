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

using System.Collections.Generic;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Brokerages.Backtesting
{
    /// <summary>
    /// Backtesting Market Simulation interface, that must be implemented by all simulators of market conditions run during backtest
    /// </summary>
    public interface IBacktestingMarketSimulation
    {
        /// <summary>
        /// Method is called by backtesting brokerage to simulate market conditions. 
        /// </summary>
        /// <param name="brokerage">Backtesting brokerage instance</param>
        /// <param name="algorithm">Algorithm instance</param>
        void SimulateMarketConditions(IBrokerage brokerage, IAlgorithm algorithm);
    }
}
