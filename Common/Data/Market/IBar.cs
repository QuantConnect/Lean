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
    /// Bar interface for second, minute, hour resolution data: 
    /// An OHLC implementation of the QuantConnect BaseData class with parameters for candles.
    /// </summary>
    public interface IBar : IBaseData
    {
        /// <summary>
        /// Opening price of Bar: Defined as the price at the start of the time period.
        /// </summary>
        decimal Open 
        { 
            get;
            set;
        }

        /// <summary>
        /// High price of Bar during the time period.
        /// </summary>
        decimal High 
        { 
            get;
            set;
        }

        /// <summary>
        /// Low price of Bar during the time period.
        /// </summary>
        decimal Low 
        { 
            get;
            set;
        }
        
        /// <summary>
        /// Closing price of Bar.
        /// </summary>
        decimal Close
        {
            get;
            set;
        }

        /// <summary>
        /// The closing time of Bar, computed via the Time and Period
        /// </summary>
        DateTime EndTime
        {
            get;
            set;
        }

        /// <summary>
        /// The period of Bar (second, minute, hour, daily, etc...)
        /// </summary>
        TimeSpan Period
        { 
            get;
            set;
        }
    }
}
