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

using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Advance Decline Volume Ratio is a Breadth indicator calculated as ratio of 
    /// summary volume of advancing stocks to summary volume of declining stocks. 
    /// AD Volume Ratio is used in technical analysis to see where the main trading activity is focused.
    /// </summary>
    public class AdvanceDeclineVolumeRatio : AdvanceDeclineIndicator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdvanceDeclineVolumeRatio"/> class
        /// </summary>
        public AdvanceDeclineVolumeRatio(string name) : base(name, (entries) => entries.Sum(s => s.Volume)) { }
    }
}
