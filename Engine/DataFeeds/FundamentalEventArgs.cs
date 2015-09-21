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
using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Specifes a type of fundamental data
    /// </summary>
    public enum FundamentalType
    {
        /// <summary>
        /// Coarse fundamental data is used for inital filtering of a larger data set.
        /// This data includes things like daily close, volume, and dollar volume.
        /// </summary>
        Coarse
    }

    /// <summary>
    /// Event arguments for the <see cref="IDataFeed.Fundamental"/> event
    /// </summary>
    public class FundamentalEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of fundamental data in this event
        /// </summary>
        public readonly FundamentalType FundamentalType;
        /// <summary>
        /// Gets the configuration for the subscription that produced this data
        /// </summary>
        public readonly SubscriptionDataConfig Configuration;
        /// <summary>
        /// Gets the utc date time this event was fired
        /// </summary>
        public readonly DateTime DateTimeUtc;
        /// <summary>
        /// Gets the data contained in the event
        /// </summary>
        public readonly IReadOnlyList<BaseData> Data;
        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalEventArgs"/> class
        /// </summary>
        /// <param name="fundamentalType">The type of fundamental data</param>
        /// <param name="configuration">Theconfiguration for the data</param>
        /// <param name="dateTimeUtc">The date time this event was fired in UTC</param>
        /// <param name="data">The data contained within this event</param>
        public FundamentalEventArgs(FundamentalType fundamentalType, SubscriptionDataConfig configuration, DateTime dateTimeUtc, IReadOnlyList<BaseData> data)
        {
            FundamentalType = fundamentalType;
            Configuration = configuration;
            DateTimeUtc = dateTimeUtc;
            Data = data;
        }
    }
}