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
/**********************************************************
* USING NAMESPACES
**********************************************************/

using System.ComponentModel.Composition;
using QuantConnect.Packets;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Messaging Interface with Cloud System
    /// </summary>
    [InheritedExport(typeof(IApi))]
    public interface IApi
    {
        /// <summary>
        /// Initialize the control system
        /// </summary>
        void Initialize();

        /// <summary>
        /// Read the maximum log allowance
        /// </summary>
        int[] ReadLogAllowance(int userId, string userToken);

        /// <summary>
        /// Update running total of log usage
        /// </summary>
        void UpdateDailyLogUsed(int userId, string backtestId, string url, int length, string userToken, bool hitLimit = false);

        /// <summary>
        /// Get the algorithm current status, active or cancelled from the user
        /// </summary>
        /// <param name="algorithmId"></param>
        /// <returns></returns>
        AlgorithmControl GetAlgorithmStatus(string algorithmId);

        /// <summary>
        /// Set the algorithm status from the worker to update the UX e.g. if there was an error.
        /// </summary>
        /// <param name="algorithmId">Algorithm id we're setting.</param>
        /// <param name="status">Status enum of the current worker</param>
        /// <param name="message">Message for the algorithm status event</param>
        void SetAlgorithmStatus(string algorithmId, AlgorithmStatus status, string message = "");

        /// <summary>
        /// Market Status Today: REST call.
        /// </summary>
        /// <param name="type">Security asset</param>
        /// <returns>Market open hours.</returns>
        MarketToday MarketToday(SecurityType type);

        /// <summary>
        /// Store the algorithm logs.
        /// </summary>
        void Store(string data, string location, StoragePermissions permissions, bool async = false);
    }
}
