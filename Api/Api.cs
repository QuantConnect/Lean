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
using QuantConnect.Packets;

namespace QuantConnect.Api
{
    /// <summary>
    /// Cloud algorithm activity controls
    /// </summary>
    public class Api : IApi
    {
        /// <summary>
        /// Initialize the API.
        /// </summary>
        public void Initialize()
        {
            //Nothing to initialize in the local copy of the engine.
        }

        /// <summary>
        /// Calculate the remaining bytes of user log allowed based on the user's cap and daily cumulative usage.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userToken">User API token</param>
        /// <returns>int[3] iUserBacktestLimit, iUserDailyLimit, remaining</returns>
        public int[] ReadLogAllowance(int userId, string userToken) 
        {
            return new[] { Int32.MaxValue, Int32.MaxValue, Int32.MaxValue };
        }

        /// <summary>
        /// Update the daily log of allowed logging-data
        /// </summary>
        /// <param name="userId">Id of the User</param>
        /// <param name="backtestId">BacktestId</param>
        /// <param name="url">URL of the log entry</param>
        /// <param name="length">length of data</param>
        /// <param name="userToken">User access token</param>
        /// <param name="hitLimit">Boolean signifying hit log limit</param>
        /// <returns>Number of bytes remaining</returns>
        public void UpdateDailyLogUsed(int userId, string backtestId, string url, int length, string userToken, bool hitLimit = false)
        {
            //
        }

        /// <summary>
        /// Get the algorithm status from the user with this algorithm id.
        /// </summary>
        /// <param name="algorithmId">String algorithm id we're searching for.</param>
        /// <param name="userId">The user id of the algorithm</param>
        /// <returns>Algorithm status enum</returns>
        public AlgorithmControl GetAlgorithmStatus(string algorithmId, int userId)
        {
            return new AlgorithmControl();
        }

        /// <summary>
        /// Algorithm passes back its current status to the UX.
        /// </summary>
        /// <param name="status">Status of the current algorithm</param>
        /// <param name="algorithmId">String algorithm id we're setting.</param>
        /// <param name="message">Message for the algorithm status event</param>
        /// <returns>Algorithm status enum</returns>
        public void SetAlgorithmStatus(string algorithmId, AlgorithmStatus status, string message = "")
        {
            //
        }

        /// <summary>
        /// Send the statistics to storage for performance tracking.
        /// </summary>
        /// <param name="algorithmId">Identifier for algorithm</param>
        /// <param name="unrealized">Unrealized gainloss</param>
        /// <param name="fees">Total fees</param>
        /// <param name="netProfit">Net profi</param>
        /// <param name="holdings">Algorithm holdings</param>
        /// <param name="equity">Total equity</param>
        /// <param name="netReturn">Net return for the deployment</param>
        /// <param name="volume">Volume traded</param>
        /// <param name="trades">Total trades since inception</param>
        /// <param name="sharpe">Sharpe ratio since inception</param>
        public void SendStatistics(string algorithmId, decimal unrealized, decimal fees, decimal netProfit, decimal holdings, decimal equity, decimal netReturn, decimal volume, int trades, double sharpe)
        {
            // 
        }

        /// <summary>
        /// Get the calendar open hours for the date.
        /// </summary>
        public MarketToday MarketToday(DateTime time, SecurityType type)
        {
            switch (type)
            {
                // since we don't directly support these types, we'll just mark them as always open
                case SecurityType.Base:
                case SecurityType.Future:
                case SecurityType.Option:
                case SecurityType.Commodity:
                    return Packets.MarketToday.OpenAllDay(time);

                case SecurityType.Equity:
                    return Packets.MarketToday.Equity(time);

                case SecurityType.Forex:
                    return Packets.MarketToday.Forex(time);

                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        /// <summary>
        /// Store logs with these authentication type
        /// </summary>
        public void Store(string data, string location, StoragePermissions permissions, bool async = false)
        {
            //
        }

        public void Dispose()
        {
            // NOP
        }
    }
}