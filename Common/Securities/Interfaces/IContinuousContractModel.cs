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

using QuantConnect.Data;
using System;
using System.Collections.Generic;

namespace QuantConnect.Securities.Interfaces
{
    /// <summary>
    /// Enum defines types of possible price adjustments in continuous contract modeling.
    /// </summary>
    public enum AdjustmentType
    {
        /// ForwardAdjusted - new quotes are adjusted as new data comes
        ForwardAdjusted,

        /// BackAdjusted - old quotes are retrospectively adjusted as new data comes
        BackAdjusted
    };

    /// <summary>
    /// Continuous contract model interface. Interfaces is implemented by different classes
    /// realizing various methods for modeling continuous security series. Primarily, modeling of continuous futures.
    /// Continuous contracts are used in backtesting of otherwise expiring derivative contracts.
    /// Continuous contracts are not traded, and are not products traded on exchanges.
    /// </summary>
    public interface IContinuousContractModel
    {
        /// <summary>
        /// Adjustment type, implemented by the model
        /// </summary>
        AdjustmentType AdjustmentType { get; set; }

        /// <summary>
        /// List of current and historical data series for one root symbol.
        /// e.g. 6BH16, 6BM16, 6BU16, 6BZ16
        /// </summary>
        IEnumerator<BaseData> InputSeries { get; set; }

        /// <summary>
        /// Method returns continuous prices from the list of current and historical data series for one root symbol.
        /// It returns enumerator of stitched continuous quotes, produced by the model.
        /// e.g. 6BH15, 6BM15, 6BU15, 6BZ15 will result in one 6B continuous historical series for 2015
        /// </summary>
        /// <returns>Continuous prices</returns>
        IEnumerator<BaseData> GetContinuousData(DateTime dateTime);

        /// <summary>
        /// Returns the list of roll dates for the contract.
        /// </summary>
        /// <returns>The list of roll dates</returns>
        IEnumerator<DateTime> GetRollDates();

        /// <summary>
        /// Returns current symbol name that corresponds to the current continuous model,
        /// or null if none.
        /// </summary>
        /// <returns>Current symbol name</returns>
        Symbol GetCurrentSymbol(DateTime dateTime);
    }
}
