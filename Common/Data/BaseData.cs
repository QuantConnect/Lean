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
using NodaTime;
using ProtoBuf;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Util;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Python;

namespace QuantConnect.Data
{
    /// <summary>
    /// Abstract base data class of QuantConnect. It is intended to be extended to define
    /// generic user customizable data types while at the same time implementing the basics of data where possible
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    [ProtoInclude(8, typeof(Tick))]
    [ProtoInclude(100, typeof(TradeBar))]
    [ProtoInclude(200, typeof(QuoteBar))]
    [ProtoInclude(300, typeof(Dividend))]
    [ProtoInclude(400, typeof(Split))]
    [PandasIgnoreMembers]
    public abstract class BaseData : IBaseData
    {
        private decimal _value;

        /// <summary>
        /// A list of all <see cref="Resolution"/>
        /// </summary>
        protected static readonly List<Resolution> AllResolutions =
            Enum.GetValues(typeof(Resolution)).Cast<Resolution>().ToList();

        /// <summary>
        /// A list of <see cref="Resolution.Daily"/>
        /// </summary>
        protected static readonly List<Resolution> DailyResolution = new List<Resolution> { Resolution.Daily };

        /// <summary>
        /// A list of <see cref="Resolution.Minute"/>
        /// </summary>
        protected static readonly List<Resolution> MinuteResolution = new List<Resolution> { Resolution.Minute };

        /// <summary>
        /// A list of high <see cref="Resolution"/>, including minute, second, and tick.
        /// </summary>
        protected static readonly List<Resolution> HighResolution = new List<Resolution> { Resolution.Minute, Resolution.Second, Resolution.Tick };

        /// <summary>
        /// A list of resolutions support by Options
        /// </summary>
        protected static readonly List<Resolution> OptionResolutions = new List<Resolution> { Resolution.Daily, Resolution.Hour, Resolution.Minute };

        /// <summary>
        /// Market Data Type of this data - does it come in individual price packets or is it grouped into OHLC.
        /// </summary>
        /// <remarks>Data is classed into two categories - streams of instantaneous prices and groups of OHLC data.</remarks>
        [ProtoMember(1)]
        public MarketDataType DataType { get; set; } = MarketDataType.Base;

        /// <summary>
        /// True if this is a fill forward piece of data
        /// </summary>
        public bool IsFillForward { get; private set; }

        /// <summary>
        /// Current time marker of this data packet.
        /// </summary>
        /// <remarks>All data is timeseries based.</remarks>
        [ProtoMember(2)]
        public DateTime Time { get; set; }

        /// <summary>
        /// The end time of this data. Some data covers spans (trade bars) and as such we want
        /// to know the entire time span covered
        /// </summary>
        // NOTE: This is needed event though the class is marked with [PandasIgnoreMembers] because the property is virtual.
        // If a derived class overrides it, without [PandasIgnore], the property will not be ignored.
        [PandasIgnore]
        public virtual DateTime EndTime
        {
            get { return Time; }
            set { Time = value; }
        }

        /// <summary>
        /// Symbol representation for underlying Security
        /// </summary>
        public Symbol Symbol { get; set; } = Symbol.Empty;

        /// <summary>
        /// Value representation of this data packet. All data requires a representative value for this moment in time.
        /// For streams of data this is the price now, for OHLC packets this is the closing price.
        /// </summary>
        [ProtoMember(4)]
        [PandasIgnore]
        public virtual decimal Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        /// <summary>
        /// As this is a backtesting platform we'll provide an alias of value as price.
        /// </summary>
        [PandasIgnore]
        public virtual decimal Price => Value;

        /// <summary>
        /// Constructor for initialising the dase data class
        /// </summary>
        public BaseData()
        {
            //Empty constructor required for fast-reflection initialization
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called. The returned object is assumed to be time stamped in the config.ExchangeTimeZone.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Line of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Instance of the T:BaseData object generated by this line of the CSV</returns>
        public virtual BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            // stub implementation to prevent compile errors in user algorithms
            var dataFeed = isLiveMode ? DataFeedEndpoint.LiveTrading : DataFeedEndpoint.Backtesting;
#pragma warning disable 618 // This implementation is left here for backwards compatibility of the BaseData API
            return Reader(config, line, date, dataFeed);
#pragma warning restore 618
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called. The returned object is assumed to be time stamped in the config.ExchangeTimeZone.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="stream">The data stream</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Instance of the T:BaseData object generated by this line of the CSV</returns>
        [StubsIgnore]
        public virtual BaseData Reader(SubscriptionDataConfig config, StreamReader stream, DateTime date, bool isLiveMode)
        {
            throw new NotImplementedException("Each data types has to implement is own Stream reader");
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String URL of source file.</returns>
        public virtual SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            // stub implementation to prevent compile errors in user algorithms
            var dataFeed = isLiveMode ? DataFeedEndpoint.LiveTrading : DataFeedEndpoint.Backtesting;
#pragma warning disable 618 // This implementation is left here for backwards compatibility of the BaseData API
            var source = GetSource(config, date, dataFeed);
#pragma warning restore 618

            if (isLiveMode)
            {
                // live trading by default always gets a rest endpoint
                return new SubscriptionDataSource(source, SubscriptionTransportMedium.Rest);
            }

            // construct a uri to determine if we have a local or remote file
            var uri = new Uri(source, UriKind.RelativeOrAbsolute);

            if (uri.IsAbsoluteUri && !uri.IsLoopback)
            {
                return new SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile);
            }

            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
        }

        /// <summary>
        /// Indicates if there is support for mapping
        /// </summary>
        /// <remarks>Relies on the <see cref="Symbol"/> property value</remarks>
        /// <returns>True indicates mapping should be used</returns>
        public virtual bool RequiresMapping()
        {
            return Symbol.RequiresMapping();
        }

        /// <summary>
        /// Indicates that the data set is expected to be sparse
        /// </summary>
        /// <remarks>Relies on the <see cref="Symbol"/> property value</remarks>
        /// <remarks>This is a method and not a property so that python
        /// custom data types can override it</remarks>
        /// <returns>True if the data set represented by this type is expected to be sparse</returns>
        public virtual bool IsSparseData()
        {
            // by default, we'll assume all custom data is sparse data
            return Symbol.SecurityType == SecurityType.Base;
        }

        /// <summary>
        /// Indicates whether this contains data that should be stored in the security cache
        /// </summary>
        /// <returns>Whether this contains data that should be stored in the security cache</returns>
        public virtual bool ShouldCacheToSecurity()
        {
            return true;
        }

        /// <summary>
        /// Gets the default resolution for this data and security type
        /// </summary>
        /// <remarks>This is a method and not a property so that python
        /// custom data types can override it</remarks>
        public virtual Resolution DefaultResolution()
        {
            return Resolution.Minute;
        }

        /// <summary>
        /// Gets the supported resolution for this data and security type
        /// </summary>
        /// <remarks>Relies on the <see cref="Symbol"/> property value</remarks>
        /// <remarks>This is a method and not a property so that python
        /// custom data types can override it</remarks>
        public virtual List<Resolution> SupportedResolutions()
        {
            if (Symbol.SecurityType.IsOption())
            {
                return OptionResolutions;
            }

            return AllResolutions;
        }

        /// <summary>
        /// Specifies the data time zone for this data type. This is useful for custom data types
        /// </summary>
        /// <remarks>Will throw <see cref="InvalidOperationException"/> for security types
        /// other than <see cref="SecurityType.Base"/></remarks>
        /// <returns>The <see cref="DateTimeZone"/> of this data type</returns>
        public virtual DateTimeZone DataTimeZone()
        {
            if (Symbol.SecurityType != SecurityType.Base)
            {
                throw new InvalidOperationException("BaseData.DataTimeZone(): is only valid for base data types");
            }
            return TimeZones.NewYork;
        }

        /// <summary>
        /// Updates this base data with a new trade
        /// </summary>
        /// <param name="lastTrade">The price of the last trade</param>
        /// <param name="tradeSize">The quantity traded</param>
        public void UpdateTrade(decimal lastTrade, decimal tradeSize)
        {
            Update(lastTrade, 0, 0, tradeSize, 0, 0);
        }

        /// <summary>
        /// Updates this base data with new quote information
        /// </summary>
        /// <param name="bidPrice">The current bid price</param>
        /// <param name="bidSize">The current bid size</param>
        /// <param name="askPrice">The current ask price</param>
        /// <param name="askSize">The current ask size</param>
        public void UpdateQuote(decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            Update(0, bidPrice, askPrice, 0, bidSize, askSize);
        }

        /// <summary>
        /// Updates this base data with the new quote bid information
        /// </summary>
        /// <param name="bidPrice">The current bid price</param>
        /// <param name="bidSize">The current bid size</param>
        public void UpdateBid(decimal bidPrice, decimal bidSize)
        {
            Update(0, bidPrice, 0, 0, bidSize, 0);
        }

        /// <summary>
        /// Updates this base data with the new quote ask information
        /// </summary>
        /// <param name="askPrice">The current ask price</param>
        /// <param name="askSize">The current ask size</param>
        public void UpdateAsk(decimal askPrice, decimal askSize)
        {
            Update(0, 0, askPrice, 0, 0, askSize);
        }

        /// <summary>
        /// Update routine to build a bar/tick from a data update.
        /// </summary>
        /// <param name="lastTrade">The last trade price</param>
        /// <param name="bidPrice">Current bid price</param>
        /// <param name="askPrice">Current asking price</param>
        /// <param name="volume">Volume of this trade</param>
        /// <param name="bidSize">The size of the current bid, if available</param>
        /// <param name="askSize">The size of the current ask, if available</param>
        public virtual void Update(decimal lastTrade, decimal bidPrice, decimal askPrice, decimal volume, decimal bidSize, decimal askSize)
        {
            Value = lastTrade;
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <remarks>
        /// This base implementation uses reflection to copy all public fields and properties
        /// </remarks>
        /// <param name="fillForward">True if this is a fill forward clone</param>
        /// <returns>A clone of the current object</returns>
        public virtual BaseData Clone(bool fillForward)
        {
            var clone = Clone();
            clone.IsFillForward = fillForward;
            return clone;
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <remarks>
        /// This base implementation uses reflection to copy all public fields and properties
        /// </remarks>
        /// <returns>A clone of the current object</returns>
        public virtual BaseData Clone()
        {
            return (BaseData) ObjectActivator.Clone((object)this);
        }

        /// <summary>
        /// Formats a string with the symbol and value.
        /// </summary>
        /// <returns>string - a string formatted as SPY: 167.753</returns>
        public override string ToString()
        {
            return $"{Symbol}: {Value.ToStringInvariant("C")}";
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called.
        /// </summary>
        /// <remarks>OBSOLETE:: This implementation is added for backward/forward compatibility purposes. This function is no longer called by the LEAN engine.</remarks>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Line of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="dataFeed">Type of datafeed we're requesting - a live or backtest feed.</param>
        /// <returns>Instance of the T:BaseData object generated by this line of the CSV</returns>
        [Obsolete("Reader(SubscriptionDataConfig, string, DateTime, DataFeedEndpoint) method has been made obsolete, use Reader(SubscriptionDataConfig, string, DateTime, bool) instead.")]
        [StubsIgnore]
        public virtual BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint dataFeed)
        {
            throw new InvalidOperationException(
                $"Please implement Reader(SubscriptionDataConfig, string, DateTime, bool) on your custom data type: {GetType().Name}"
            );
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <remarks>OBSOLETE:: This implementation is added for backward/forward compatibility purposes. This function is no longer called by the LEAN engine.</remarks>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="datafeed">Type of datafeed we're reqesting - backtest or live</param>
        /// <returns>String URL of source file.</returns>
        [Obsolete("GetSource(SubscriptionDataConfig, DateTime, DataFeedEndpoint) method has been made obsolete, use GetSource(SubscriptionDataConfig, DateTime, bool) instead.")]
        [StubsIgnore]
        public virtual string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed)
        {
            throw new InvalidOperationException(
                $"Please implement GetSource(SubscriptionDataConfig, DateTime, bool) on your custom data type: {GetType().Name}"
            );
        }

        /// <summary>
        /// Deserialize the message from the data server
        /// </summary>
        /// <param name="serialized">The data server's message</param>
        /// <returns>An enumerable of base data, if unsuccessful, returns an empty enumerable</returns>
        public static IEnumerable<BaseData> DeserializeMessage(string serialized)
        {
            var deserialized = JsonConvert.DeserializeObject(serialized, JsonSerializerSettings);

            var enumerable = deserialized as IEnumerable<BaseData>;
            if (enumerable != null)
            {
                return enumerable;
            }

            var data = deserialized as BaseData;
            if (data != null)
            {
                return new[] { data };
            }

            return Enumerable.Empty<BaseData>();
        }

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
    }
}
