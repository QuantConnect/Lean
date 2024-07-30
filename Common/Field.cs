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
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect
{
    /// <summary>
    /// Provides static properties to be used as selectors with the indicator system
    /// </summary>
    public static partial class Field
    {
        private readonly static Func<IBaseData, decimal> _high = DataTypePropertyOrValue<IBaseDataBar>(x => x.High);
        private readonly static Func<IBaseData, decimal> _low = DataTypePropertyOrValue<IBaseDataBar>(x => x.Low);
        private readonly static Func<IBaseData, decimal> _open = DataTypePropertyOrValue<IBaseDataBar>(x => x.Open);
        private readonly static Func<IBaseData, decimal> _bidClose = DataTypePropertyOrValue<QuoteBar>(x => ((QuoteBar)x).Bid.Close);
        private readonly static Func<IBaseData, decimal> _bidOpen = DataTypePropertyOrValue<QuoteBar>(x => ((QuoteBar)x).Bid.Open);
        private readonly static Func<IBaseData, decimal> _bidLow = DataTypePropertyOrValue<QuoteBar>(x => ((QuoteBar)x).Bid.Low);
        private readonly static Func<IBaseData, decimal> _bidHigh = DataTypePropertyOrValue<QuoteBar>(x => ((QuoteBar)x).Bid.High);
        private readonly static Func<IBaseData, decimal> _askClose = DataTypePropertyOrValue<QuoteBar>(x => ((QuoteBar)x).Ask.Close);
        private readonly static Func<IBaseData, decimal> _askOpen = DataTypePropertyOrValue<QuoteBar>(x => ((QuoteBar)x).Ask.Open);
        private readonly static Func<IBaseData, decimal> _askLow = DataTypePropertyOrValue<QuoteBar>(x => ((QuoteBar)x).Ask.Low);
        private readonly static Func<IBaseData, decimal> _askHigh = DataTypePropertyOrValue<QuoteBar>(x => ((QuoteBar)x).Ask.High);
        private readonly static Func<IBaseData, decimal> _bidPrice = DataTypePropertyOrValue<Tick>(x => ((Tick)x).BidPrice);
        private readonly static Func<IBaseData, decimal> _askPrice = DataTypePropertyOrValue<Tick>(x => ((Tick)x).AskPrice);
        private readonly static Func<IBaseData, decimal> _volume = DataTypePropertyOrValue<TradeBar>(x => ((TradeBar)x).Volume);
        private readonly static Func<IBaseData, decimal> _average = DataTypePropertyOrValue<IBaseDataBar>(x => (x.Open + x.High + x.Low + x.Close) / 4m);
        private readonly static Func<IBaseData, decimal> _median = DataTypePropertyOrValue<IBaseDataBar>(x => (x.High + x.Low) / 2m);
        private readonly static Func<IBaseData, decimal> _typical = DataTypePropertyOrValue<IBaseDataBar>(x => (x.High + x.Low + x.Close) / 3m);
        private readonly static Func<IBaseData, decimal> _weighted = DataTypePropertyOrValue<IBaseDataBar>(x => (x.High + x.Low + 2 * x.Close) / 4m);
        private readonly static Func<IBaseData, decimal> _sevenBar = DataTypePropertyOrValue<IBaseDataBar>(x => (2 * x.Open + x.High + x.Low + 3 * x.Close) / 7m);

        /// <summary>
        /// Gets a selector that selectes the Bid close price
        /// </summary>
        public static Func<IBaseData, decimal> BidClose
        {
            get { return _bidClose; }
        }

        /// <summary>
        /// Gets a selector that selectes the Bid open price
        /// </summary>
        public static Func<IBaseData, decimal> BidOpen
        {
            get { return _bidOpen; }
        }

        /// <summary>
        /// Gets a selector that selectes the Bid low price
        /// </summary>
        public static Func<IBaseData, decimal> BidLow
        {
            get { return _bidLow; }
        }

        /// <summary>
        /// Gets a selector that selectes the Bid high price
        /// </summary>
        public static Func<IBaseData, decimal> BidHigh
        {
            get { return _bidHigh; }
        }

        /// <summary>
        /// Gets a selector that selectes the Ask close price
        /// </summary>
        public static Func<IBaseData, decimal> AskClose
        {
            get { return _askClose; }
        }

        /// <summary>
        /// Gets a selector that selectes the Ask open price
        /// </summary>
        public static Func<IBaseData, decimal> AskOpen
        {
            get { return _askOpen; }
        }

        /// <summary>
        /// Gets a selector that selectes the Ask low price
        /// </summary>
        public static Func<IBaseData, decimal> AskLow
        {
            get { return _askLow; }
        }

        /// <summary>
        /// Gets a selector that selectes the Ask high price
        /// </summary>
        public static Func<IBaseData, decimal> AskHigh
        {
            get { return _askHigh; }
        }

        /// <summary>
        /// Gets a selector that selectes the Ask price
        /// </summary>
        public static Func<IBaseData, decimal> AskPrice
        {
            get { return _askPrice; }
        }

        /// <summary>
        /// Gets a selector that selectes the Bid price
        /// </summary>
        public static Func<IBaseData, decimal> BidPrice
        {
            get { return _bidPrice; }
        }

        /// <summary>
        /// Gets a selector that selects the Open value
        /// </summary>
        public static Func<IBaseData, decimal> Open
        {
            get { return _open; }
        }

        /// <summary>
        /// Gets a selector that selects the High value
        /// </summary>
        public static Func<IBaseData, decimal> High
        {
            get { return _high; }
        }

        /// <summary>
        /// Gets a selector that selects the Low value
        /// </summary>
        public static Func<IBaseData, decimal> Low
        {
            get { return _low; }
        }

        /// <summary>
        /// Gets a selector that selects the Close value
        /// </summary>
        public static Func<IBaseData, decimal> Close
        {
            get { return x => x.Value; }
        }

        /// <summary>
        /// Defines an average price that is equal to (O + H + L + C) / 4
        /// </summary>
        public static Func<IBaseData, decimal> Average
        {
            get { return _average; }
        }

        /// <summary>
        /// Defines an average price that is equal to (H + L) / 2
        /// </summary>
        public static Func<IBaseData, decimal> Median
        {
            get { return _median; }
        }

        /// <summary>
        /// Defines an average price that is equal to (H + L + C) / 3
        /// </summary>
        public static Func<IBaseData, decimal> Typical
        {
            get { return _typical; }
        }

        /// <summary>
        /// Defines an average price that is equal to (H + L + 2*C) / 4
        /// </summary>
        public static Func<IBaseData, decimal> Weighted
        {
            get { return _weighted; }
        }

        /// <summary>
        /// Defines an average price that is equal to (2*O + H + L + 3*C)/7
        /// </summary>
        public static Func<IBaseData, decimal> SevenBar
        {
            get { return _sevenBar; }
        }

        /// <summary>
        /// Gets a selector that selectors the Volume value
        /// </summary>
        public static Func<IBaseData, decimal> Volume
        {
            get { return _volume; }
        }

        private static Func<IBaseData, decimal> DataTypePropertyOrValue<T>(Func<T, decimal> selector, Func<IBaseData, decimal> defaultSelector = null)
            where T: class, IBaseData
        {
            return x =>
            {
                var bar = x as T;
                if (bar != null)
                {
                    return selector(bar);
                }

                defaultSelector ??= (data => data.Value);
                return defaultSelector(x);
            };
        }
    }
}
