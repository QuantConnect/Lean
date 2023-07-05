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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents a bar sectioned not by time, but by some amount of movement in a set field,
    /// where:
    /// - Open : Gets the opening value that started this bar
    /// - Close : Gets the closing value or the current value if the bar has not yet closed.
    /// - High : Gets the highest value encountered during this bar
    /// - Low : Gets the lowest value encountered during this bar
    /// </summary>
    public abstract class BaseRenkoBar : TradeBar, IBaseDataBar
    {
        /// <summary>
        /// Gets the kind of the bar
        /// </summary>
        public RenkoType Type { get; protected set; }

        /// <summary>
        /// The preset size of the consolidated bar
        /// </summary>
        public decimal BrickSize  { get; protected set; }

        /// <summary>
        /// Gets the end time of this renko bar or the most recent update time if it <see cref="IsClosed"/>
        /// </summary>
        public override DateTime EndTime { get; set; }

        /// <summary>
        /// Gets the time this bar started
        /// </summary>
        public DateTime Start
        {
            get { return Time; }
            protected set { Time = value; }
        }

        /// <summary>
        /// Gets whether or not this bar is considered closed.
        /// </summary>
        public virtual bool IsClosed { get; protected set; }

        /// <summary>
        /// Reader Method :: using set of arguements we specify read out type. Enumerate
        /// until the end of the data stream or file. E.g. Read CSV file line by line and convert
        /// into data types.
        /// </summary>
        /// <returns>BaseData type set by Subscription Method.</returns>
        /// <param name="config">Config.</param>
        /// <param name="line">Line.</param>
        /// <param name="date">Date.</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            throw new NotSupportedException("RenkoBar does not support the Reader function. This function should never be called on this type.");
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String URL of source file.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            throw new NotSupportedException("RenkoBar does not support the GetSource function. This function should never be called on this type.");
        }
    }
}