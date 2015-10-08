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

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Represents a candlestick bar
    /// </summary>
    public class LeanBar
    {
        /// <summary>
        /// The starting time of the bar
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The open value of the bar
        /// </summary>
        public double Open { get; set; }

        /// <summary>
        /// The high value of the bar
        /// </summary>
        public double High { get; set; }

        /// <summary>
        /// The low value of the bar
        /// </summary>
        public double Low { get; set; }

        /// <summary>
        /// The close value of the bar
        /// </summary>
        public double Close { get; set; }

        /// <summary>
        /// The number of ticks in the bar
        /// </summary>
        public int TickVolume { get; set; }
    }
}