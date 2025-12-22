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

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Optional interface for consolidators that can express the maximum input data period they can accept
    /// (for example, a 1-hour consolidator requires input data with period less than or equal to 1 hour).
    /// </summary>
    public interface IConsolidatorInputDataRequirement
    {
        /// <summary>
        /// Gets the maximum period of input data that this consolidator can accept, or null if it is not applicable/known.
        /// </summary>
        TimeSpan? MaxInputDataPeriod { get; }
    }
}

