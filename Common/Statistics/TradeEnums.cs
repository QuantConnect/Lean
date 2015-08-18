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

namespace QuantConnect.Statistics
{
    /// <summary>
    /// Direction of a trade
    /// </summary>
    public enum TradeDirection
    {
        /// <summary>
        /// Long direction
        /// </summary>
        Long,

        /// <summary>
        /// Short direction
        /// </summary>
        Short
    }

    /// <summary>
    /// The method used to group order fills into trades
    /// </summary>
    public enum FillGroupingMethod
    {
        /// <summary>
        /// A Trade is defined by a fill that establishes or increases a position and an offsetting fill that reduces the position size.
        /// </summary>
        FillToFill,

        /// <summary>
        /// A Trade is defined by a sequence of fills, from a flat position to a non-zero position which may increase or decrease in quantity, and back to a flat position.
        /// </summary>
        FlatToFlat,

        /// <summary>
        /// A Trade is defined by a sequence of fills, from a flat position to a non-zero position and an offsetting fill that reduces the position size.
        /// </summary>
        FlatToReduced,
    }

    /// <summary>
    /// The method used to match offsetting order fills
    /// </summary>
    public enum FillMatchingMethod
    {
        /// <summary>
        /// First In First Out fill matching method
        /// </summary>
        FIFO,

        /// <summary>
        /// Last In Last Out fill matching method
        /// </summary>
        LIFO
    }
}
