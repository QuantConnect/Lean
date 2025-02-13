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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using QuantConnect.Securities;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

namespace QuantConnect
{
    /// <summary>
    /// Shortcut date format strings
    /// </summary>
    public static class DateFormat
    {
        /// Year-Month-Date 6 Character Date Representation
        public const string SixCharacter = "yyMMdd";
        /// YYYY-MM-DD Eight Character Date Representation
        public const string EightCharacter = "yyyyMMdd";
        /// Daily and hourly time format
        public const string TwelveCharacter = "yyyyMMdd HH:mm";
        /// JSON Format Date Representation
        public static string JsonFormat { get; } = "yyyy-MM-ddTHH:mm:ss";
        /// MySQL Format Date Representation
        public const string DB = "yyyy-MM-dd HH:mm:ss";
        /// QuantConnect UX Date Representation
        public const string UI = "yyyy-MM-dd HH:mm:ss";
        /// en-US Short Date and Time Pattern
        public const string USShort = "M/d/yy h:mm tt";
        /// en-US Short Date Pattern
        public const string USShortDateOnly = "M/d/yy";
        /// en-US format
        public const string US = "M/d/yyyy h:mm:ss tt";
        /// en-US Date format
        public const string USDateOnly = "M/d/yyyy";
        /// Date format of QC forex data
        public const string Forex = "yyyyMMdd HH:mm:ss.ffff";
        /// Date format of FIX Protocol UTC Timestamp without milliseconds
        public const string FIX = "yyyyMMdd-HH:mm:ss";
        /// Date format of FIX Protocol UTC Timestamp with milliseconds
        public const string FIXWithMillisecond = "yyyyMMdd-HH:mm:ss.fff";
        /// YYYYMM Year and Month Character Date Representation (used for futures)
        public const string YearMonth = "yyyyMM";
    }

    /// <summary>
    /// Singular holding of assets from backend live nodes:
    /// </summary>
    [JsonConverter(typeof(HoldingJsonConverter))]
    public class Holding
    {
        private decimal? _conversionRate;
        private decimal _marketValue;
        private decimal _unrealizedPnl;
        private decimal _unrealizedPnLPercent;

        /// Symbol of the Holding:
        [JsonIgnore]
        public Symbol Symbol { get; set; } = Symbol.Empty;

        /// Type of the security
        [JsonIgnore]
        public SecurityType Type => Symbol.SecurityType;

        /// The currency symbol of the holding, such as $
        [DefaultValue("$")]
        [JsonProperty(PropertyName = "c", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CurrencySymbol { get; set; }

        /// Average Price of our Holding in the currency the symbol is traded in
        [JsonConverter(typeof(DecimalJsonConverter))]
        [JsonProperty(PropertyName = "a", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal AveragePrice { get; set; }

        /// Quantity of Symbol We Hold.
        [JsonConverter(typeof(DecimalJsonConverter))]
        [JsonProperty(PropertyName = "q", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal Quantity { get; set; }

        /// Current Market Price of the Asset in the currency the symbol is traded in
        [JsonConverter(typeof(DecimalJsonConverter))]
        [JsonProperty(PropertyName = "p", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal MarketPrice { get; set; }

        /// Current market conversion rate into the account currency
        [JsonConverter(typeof(DecimalJsonConverter))]
        [JsonProperty(PropertyName = "r", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal? ConversionRate
        {
            get
            {
                return _conversionRate;
            }
            set
            {
                if (value != 1)
                {
                    _conversionRate = value;
                }
            }
        }

        /// Current market value of the holding
        [JsonConverter(typeof(DecimalJsonConverter))]
        [JsonProperty(PropertyName = "v", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal MarketValue
        {
            get
            {
                return _marketValue;
            }
            set
            {
                _marketValue = value.SmartRoundingShort();
            }
        }

        /// Current unrealized P/L of the holding
        [JsonConverter(typeof(DecimalJsonConverter))]
        [JsonProperty(PropertyName = "u", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal UnrealizedPnL
        {
            get
            {
                return _unrealizedPnl;
            }
            set
            {
                _unrealizedPnl = value.SmartRoundingShort();
            }
        }

        /// Current unrealized P/L % of the holding
        [JsonConverter(typeof(DecimalJsonConverter))]
        [JsonProperty(PropertyName = "up", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal UnrealizedPnLPercent
        {
            get
            {
                return _unrealizedPnLPercent;
            }
            set
            {
                _unrealizedPnLPercent = value.SmartRoundingShort();
            }
        }

        /// Create a new default holding:
        public Holding()
        {
            CurrencySymbol = "$";
        }

        /// <summary>
        /// Create a simple JSON holdings from a Security holding class.
        /// </summary>
        /// <param name="security">The security instance</param>
        public Holding(Security security)
            : this()
        {
            var holding = security.Holdings;

            Symbol = holding.Symbol;
            Quantity = holding.Quantity;
            MarketValue = holding.HoldingsValue;
            CurrencySymbol = Currencies.GetCurrencySymbol(security.QuoteCurrency.Symbol);
            ConversionRate = security.QuoteCurrency.ConversionRate;

            var rounding = security.SymbolProperties.MinimumPriceVariation.GetDecimalPlaces();

            AveragePrice = Math.Round(holding.AveragePrice, rounding);
            MarketPrice = Math.Round(holding.Price, rounding);
            UnrealizedPnL = Math.Round(holding.UnrealizedProfit, 2);
            UnrealizedPnLPercent = Math.Round(holding.UnrealizedProfitPercent * 100, 2);
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns>A new Holding object with the same values as this one</returns>
        public Holding Clone()
        {
            return new Holding
            {
                AveragePrice = AveragePrice,
                Symbol = Symbol,
                Quantity = Quantity,
                MarketPrice = MarketPrice,
                MarketValue = MarketValue,
                UnrealizedPnL = UnrealizedPnL,
                UnrealizedPnLPercent = UnrealizedPnLPercent,
                ConversionRate = ConversionRate,
                CurrencySymbol = CurrencySymbol
            };
        }

        /// <summary>
        /// Writes out the properties of this instance to string
        /// </summary>
        public override string ToString()
        {
            return Messages.Holding.ToString(this);
        }

        private class DecimalJsonConverter : JsonConverter
        {
            public override bool CanRead => false;
            public override bool CanConvert(Type objectType) => typeof(decimal) == objectType;
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteRawValue(((decimal)value).NormalizeToStr());
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
        private class HoldingJsonConverter : JsonConverter
        {
            public override bool CanWrite => false;
            public override bool CanConvert(Type objectType) => typeof(Holding) == objectType;
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var jObject = JObject.Load(reader);
                var result = new Holding
                {
                    Symbol = jObject["symbol"]?.ToObject<Symbol>() ?? jObject["Symbol"]?.ToObject<Symbol>() ?? Symbol.Empty,
                    CurrencySymbol = jObject["c"]?.Value<string>() ?? jObject["currencySymbol"]?.Value<string>() ?? jObject["CurrencySymbol"]?.Value<string>() ?? string.Empty,
                    AveragePrice = jObject["a"]?.Value<decimal>() ?? jObject["averagePrice"]?.Value<decimal>() ?? jObject["AveragePrice"]?.Value<decimal>() ?? 0,
                    Quantity = jObject["q"]?.Value<decimal>() ?? jObject["quantity"]?.Value<decimal>() ?? jObject["Quantity"]?.Value<decimal>() ?? 0,
                    MarketPrice = jObject["p"]?.Value<decimal>() ?? jObject["marketPrice"]?.Value<decimal>() ?? jObject["MarketPrice"]?.Value<decimal>() ?? 0,
                    ConversionRate = jObject["r"]?.Value<decimal>() ?? jObject["conversionRate"]?.Value<decimal>() ?? jObject["ConversionRate"]?.Value<decimal>() ?? null,
                    MarketValue = jObject["v"]?.Value<decimal>() ?? jObject["marketValue"]?.Value<decimal>() ?? jObject["MarketValue"]?.Value<decimal>() ?? 0,
                    UnrealizedPnL = jObject["u"]?.Value<decimal>() ?? jObject["unrealizedPnl"]?.Value<decimal>() ?? jObject["UnrealizedPnl"]?.Value<decimal>() ?? 0,
                    UnrealizedPnLPercent = jObject["up"]?.Value<decimal>() ?? jObject["unrealizedPnLPercent"]?.Value<decimal>() ?? jObject["UnrealizedPnLPercent"]?.Value<decimal>() ?? 0,
                };
                if (!result.ConversionRate.HasValue)
                {
                    result.ConversionRate = 1;
                }
                if (string.IsNullOrEmpty(result.CurrencySymbol))
                {
                    result.CurrencySymbol = "$";
                }
                return result;
            }
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// Represents the types of environments supported by brokerages for trading
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BrokerageEnvironment
    {
        /// <summary>
        /// Live trading (0)
        /// </summary>
        [EnumMember(Value = "live")]
        Live,

        /// <summary>
        /// Paper trading (1)
        /// </summary>
        [EnumMember(Value = "paper")]
        Paper
    }

    /// <summary>
    /// Multilanguage support enum: which language is this project for the interop bridge converter.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Language
    {
        /// <summary>
        /// C# Language Project (0)
        /// </summary>
        [EnumMember(Value = "C#")]
        CSharp,

        /// <summary>
        /// FSharp Project (1)
        /// </summary>
        [EnumMember(Value = "F#")]
        FSharp,

        /// <summary>
        /// Visual Basic Project (2)
        /// </summary>
        [EnumMember(Value = "VB")]
        VisualBasic,

        /// <summary>
        /// Java Language Project (3)
        /// </summary>
        [EnumMember(Value = "Ja")]
        Java,

        /// <summary>
        /// Python Language Project (4)
        /// </summary>
        [EnumMember(Value = "Py")]
        Python
    }

    /// <summary>
    /// Live server types available through the web IDE. / QC deployment.
    /// </summary>
    public enum ServerType
    {
        /// <summary>
        /// Additional server (0)
        /// </summary>
        Server512,

        /// <summary>
        /// Upgraded server (1)
        /// </summary>
        Server1024,

        /// <summary>
        /// Server with 2048 MB Ram (2)
        /// </summary>
        Server2048
    }

    /// <summary>
    /// Type of tradable security / underlying asset
    /// </summary>
    public enum SecurityType
    {
        /// <summary>
        /// Base class for all security types (0)
        /// </summary>
        Base,

        /// <summary>
        /// US Equity Security (1)
        /// </summary>
        Equity,

        /// <summary>
        /// Option Security Type (2)
        /// </summary>
        Option,

        /// <summary>
        /// Commodity Security Type (3)
        /// </summary>
        Commodity,

        /// <summary>
        /// FOREX Security (4)
        /// </summary>
        Forex,

        /// <summary>
        /// Future Security Type (5)
        /// </summary>
        Future,

        /// <summary>
        /// Contract For a Difference Security Type (6)
        /// </summary>
        Cfd,

        /// <summary>
        /// Cryptocurrency Security Type (7)
        /// </summary>
        Crypto,

        /// <summary>
        /// Futures Options Security Type (8)
        /// </summary>
        /// <remarks>
        /// Futures options function similar to equity options, but with a few key differences.
        /// Firstly, the contract unit of trade is 1x, rather than 100x. This means that each
        /// option represents the right to buy or sell 1 future contract at expiry/exercise.
        /// The contract multiplier for Futures Options plays a big part in determining the premium
        /// of the option, which can also differ from the underlying future's multiplier.
        /// </remarks>
        FutureOption,

        /// <summary>
        /// Index Security Type (9)
        /// </summary>
        Index,

        /// <summary>
        /// Index Option Security Type (10)
        /// </summary>
        /// <remarks>
        /// For index options traded on American markets, they tend to be European-style options and are Cash-settled.
        /// </remarks>
        IndexOption,

        /// <summary>
        /// Crypto Future Type (11)
        /// </summary>
        CryptoFuture,
    }

    /// <summary>
    /// Account type: margin or cash
    /// </summary>
    public enum AccountType
    {
        /// <summary>
        /// Margin account type (0)
        /// </summary>
        Margin,

        /// <summary>
        /// Cash account type (1)
        /// </summary>
        Cash
    }

    /// <summary>
    /// Market data style: is the market data a summary (OHLC style) bar, or is it a time-price value.
    /// </summary>
    public enum MarketDataType
    {
        /// Base market data type (0)
        Base,
        /// TradeBar market data type (OHLC summary bar) (1)
        TradeBar,
        /// Tick market data type (price-time pair) (2)
        Tick,
        /// Data associated with an instrument (3)
        Auxiliary,
        /// QuoteBar market data type (4) [Bid(OHLC), Ask(OHLC) and Mid(OHLC) summary bar]
        QuoteBar,
        /// Option chain data (5)
        OptionChain,
        /// Futures chain data (6)
        FuturesChain
    }

    /// <summary>
    /// Datafeed enum options for selecting the source of the datafeed.
    /// </summary>
    public enum DataFeedEndpoint
    {
        /// Backtesting Datafeed Endpoint (0)
        Backtesting,
        /// Loading files off the local system (1)
        FileSystem,
        /// Getting datafeed from a QC-Live-Cloud (2)
        LiveTrading,
        /// Database (3)
        Database
    }

    /// <summary>
    /// Cloud storage permission options.
    /// </summary>
    public enum StoragePermissions
    {
        /// Public Storage Permissions (0)
        Public,

        /// Authenticated Read Storage Permissions (1)
        Authenticated
    }

    /// <summary>
    /// Types of tick data
    /// </summary>
    /// <remarks>QuantConnect currently only has trade, quote, open interest tick data.</remarks>
    public enum TickType
    {
        /// Trade type tick object (0)
        Trade ,
        /// Quote type tick object (1)
        Quote,
        /// Open Interest type tick object (for options, futures) (2)
        OpenInterest
    }

    /// <summary>
    /// Specifies the type of <see cref="QuantConnect.Data.Market.Delisting"/> data
    /// </summary>
    public enum DelistingType
    {
        /// <summary>
        /// Specifies a warning of an imminent delisting (0)
        /// </summary>
        Warning = 0,

        /// <summary>
        /// Specifies the symbol has been delisted (1)
        /// </summary>
        Delisted = 1
    }

    /// <summary>
    /// Specifies the type of <see cref="QuantConnect.Data.Market.Split"/> data
    /// </summary>
    public enum SplitType
    {
        /// <summary>
        /// Specifies a warning of an imminent split event (0)
        /// </summary>
        Warning = 0,

        /// <summary>
        /// Specifies the symbol has been split (1)
        /// </summary>
        SplitOccurred = 1
    }

    /// <summary>
    /// Resolution of data requested.
    /// </summary>
    /// <remarks>Always sort the enum from the smallest to largest resolution</remarks>
    public enum Resolution
    {
        /// Tick Resolution (0)
        Tick,
        /// Second Resolution (1)
        Second,
        /// Minute Resolution (2)
        Minute,
        /// Hour Resolution (3)
        Hour,
        /// Daily Resolution (4)
        Daily
    }

    /// <summary>
    /// Specifies what side a position is on, long/short
    /// </summary>
    public enum PositionSide
    {
        /// <summary>
        /// A short position, quantity less than zero (-1)
        /// </summary>
        Short = -1,

        /// <summary>
        /// No position, quantity equals zero (0)
        /// </summary>
        None = 0,

        /// <summary>
        /// A long position, quantity greater than zero (1)
        /// </summary>
        Long = 1
    }

    /// <summary>
    /// Specifies the different types of options
    /// </summary>
    public enum OptionRight
    {
        /// <summary>
        /// A call option, the right to buy at the strike price (0)
        /// </summary>
        Call,

        /// <summary>
        /// A put option, the right to sell at the strike price (1)
        /// </summary>
        Put
    }

    /// <summary>
    /// Specifies the style of an option
    /// </summary>
    public enum OptionStyle
    {
        /// <summary>
        /// American style options are able to be exercised at any time on or before the expiration date (0)
        /// </summary>
        American,

        /// <summary>
        /// European style options are able to be exercised on the expiration date only (1)
        /// </summary>
        European
    }

    /// <summary>
    /// Specifies the type of settlement in derivative deals
    /// </summary>
    public enum SettlementType
    {
        /// <summary>
        /// Physical delivery of the underlying security (0)
        /// </summary>
        PhysicalDelivery,

        /// <summary>
        /// Cash is paid/received on settlement (1)
        /// </summary>
        Cash
    }

    /// <summary>
    /// Wrapper for algorithm status enum to include the charting subscription.
    /// </summary>
    public class AlgorithmControl
    {
        /// <summary>
        /// Default initializer for algorithm control class.
        /// </summary>
        public AlgorithmControl()
        {
            // default to true, API can override
            Initialized = false;
            HasSubscribers = true;
            Status = AlgorithmStatus.Running;
            ChartSubscription = Messages.AlgorithmControl.ChartSubscription;
        }

        /// <summary>
        /// Register this control packet as not defaults.
        /// </summary>
        public bool Initialized { get; set; }

        /// <summary>
        /// Current run status of the algorithm id.
        /// </summary>
        public AlgorithmStatus Status { get; set; }

        /// <summary>
        /// Currently requested chart.
        /// </summary>
        public string ChartSubscription { get; set; }

        /// <summary>
        /// True if there's subscribers on the channel
        /// </summary>
        public bool HasSubscribers { get; set; }
    }

    /// <summary>
    /// States of a live deployment.
    /// </summary>
    public enum AlgorithmStatus
    {
        /// Error compiling algorithm at start (0)
        DeployError,
        /// Waiting for a server (1)
        InQueue,
        /// Running algorithm (2)
        Running,
        /// Stopped algorithm or exited with runtime errors (3)
        Stopped,
        /// Liquidated algorithm (4)
        Liquidated,
        /// Algorithm has been deleted (5)
        Deleted,
        /// Algorithm completed running (6)
        Completed,
        /// Runtime Error Stoped Algorithm (7)
        RuntimeError,
        /// Error in the algorithm id (not used) (8)
        Invalid,
        /// The algorithm is logging into the brokerage (9)
        LoggingIn,
        /// The algorithm is initializing (10)
        Initializing,
        /// History status update (11)
        History
    }

    /// <summary>
    /// Specifies where a subscription's data comes from
    /// </summary>
    public enum SubscriptionTransportMedium
    {
        /// <summary>
        /// The subscription's data comes from disk (0)
        /// </summary>
        LocalFile,

        /// <summary>
        /// The subscription's data is downloaded from a remote source (1)
        /// </summary>
        RemoteFile,

        /// <summary>
        /// The subscription's data comes from a rest call that is polled and returns a single line/data point of information (2)
        /// </summary>
        Rest,

        /// <summary>
        /// The subscription's data is streamed (3)
        /// </summary>
        Streaming,

        /// <summary>
        /// The subscription's data comes from the object store (4)
        /// </summary>
        ObjectStore
    }

    /// <summary>
    /// Used by the <see cref="Data.LeanDataWriter"/> to determine which merge write policy should be applied
    /// </summary>
    public enum WritePolicy
    {
        /// <summary>
        /// Will overwrite any existing file or zip entry with the new content (0)
        /// </summary>
        Overwrite = 0,

        /// <summary>
        /// Will inject and merge new content with the existings file content (1)
        /// </summary>
        Merge,

        /// <summary>
        /// Will append new data to the end of the file or zip entry (2)
        /// </summary>
        Append
    }

    /// <summary>
    /// enum Period - Enum of all the analysis periods, AS integers. Reference "Period" Array to access the values
    /// </summary>
    public enum Period
    {
        /// Period Short Codes - 10
        TenSeconds = 10,
        /// Period Short Codes - 30 Second
        ThirtySeconds = 30,
        /// Period Short Codes - 60 Second
        OneMinute = 60,
        /// Period Short Codes - 120 Second
        TwoMinutes = 120,
        /// Period Short Codes - 180 Second
        ThreeMinutes = 180,
        /// Period Short Codes - 300 Second
        FiveMinutes = 300,
        /// Period Short Codes - 600 Second
        TenMinutes = 600,
        /// Period Short Codes - 900 Second
        FifteenMinutes = 900,
        /// Period Short Codes - 1200 Second
        TwentyMinutes = 1200,
        /// Period Short Codes - 1800 Second
        ThirtyMinutes = 1800,
        /// Period Short Codes - 3600 Second
        OneHour = 3600,
        /// Period Short Codes - 7200 Second
        TwoHours = 7200,
        /// Period Short Codes - 14400 Second
        FourHours = 14400,
        /// Period Short Codes - 21600 Second
        SixHours = 21600
    }

    /// <summary>
    /// Specifies how data is normalized before being sent into an algorithm
    /// </summary>
    public enum DataNormalizationMode
    {
        /// <summary>
        /// No modifications to the asset price at all. For Equities, dividends are paid in cash and splits are applied directly to your portfolio quantity. (0)
        /// </summary>
        Raw,
        /// <summary>
        /// Splits and dividends are backward-adjusted into the price of the asset. The price today is identical to the current market price. (1)
        /// </summary>
        Adjusted,
        /// <summary>
        /// Equity splits are applied to the price adjustment but dividends are paid in cash to your portfolio. This normalization mode allows you to manage dividend payments (e.g. reinvestment) while still giving a smooth time series of prices for indicators. (2)
        /// </summary>
        SplitAdjusted,
        /// <summary>
        /// Equity splits are applied to the price adjustment and the value of all future dividend payments is added to the initial asset price. (3)
        /// </summary>
        TotalReturn,
        /// <summary>
        /// Eliminates price jumps between two consecutive contracts, adding a factor based on the difference of their prices. The first contract has the true price. Factor 0. (4)
        /// </summary>
        /// <remarks>First contract is the true one, factor 0</remarks>
        ForwardPanamaCanal,
        /// <summary>
        /// Eliminates price jumps between two consecutive contracts, adding a factor based on the difference of their prices. The last contract has the true price. Factor 0. (5)
        /// </summary>
        /// <remarks>Last contract is the true one, factor 0</remarks>
        BackwardsPanamaCanal,
        /// <summary>
        /// Eliminates price jumps between two consecutive contracts, multiplying the prices by their ratio. The last contract has the true price. Factor 1. (6)
        /// </summary>
        /// <remarks>Last contract is the true one, factor 1</remarks>
        BackwardsRatio,
        /// <summary>
        /// Splits and dividends are adjusted into the prices in a given date. Only for history requests. (7)
        /// </summary>
        ScaledRaw,
    }

    /// <summary>
    /// Continuous contracts mapping modes
    /// </summary>
    public enum DataMappingMode
    {
        /// <summary>
        /// The contract maps on the previous day of expiration of the front month (0)
        /// </summary>
        LastTradingDay,
        /// <summary>
        /// The contract maps on the first date of the delivery month of the front month. If the contract expires prior to this date,
        /// then it rolls on the contract's last trading date instead (1)
        /// </summary>
        /// <remarks>For example, the Crude Oil WTI (CL) 'DEC 2021 CLZ1' contract expires on November, 19 2021, so the mapping date will be its expiration date.</remarks>
        /// <remarks>Another example is the Corn 'DEC 2021 ZCZ1' contract, which expires on December, 14 2021, so the mapping date will be December 1, 2021.</remarks>
        FirstDayMonth,
        /// <summary>
        /// The contract maps when the following back month contract has a higher open interest that the current front month (2)
        /// </summary>
        OpenInterest,
        /// <summary>
        /// The contract maps when any of the back month contracts of the next year have a higher volume that the current front month (3)
        /// </summary>
        OpenInterestAnnual,
    }

    /// <summary>
    /// The different types of <see cref="CashBook.Updated"/> events
    /// </summary>
    public enum CashBookUpdateType
    {
        /// <summary>
        /// A new <see cref="Cash.Symbol"/> was added (0)
        /// </summary>
        Added,
        /// <summary>
        /// One or more <see cref="Cash"/> instances were removed (1)
        /// </summary>
        Removed,
        /// <summary>
        /// An existing <see cref="Cash.Symbol"/> was updated (2)
        /// </summary>
        Updated
    }

    /// <summary>
    /// Defines Lean exchanges codes and names
    /// </summary>
    public static class Exchanges
    {
        /// <summary>
        /// Gets the exchange as single character representation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetPrimaryExchangeCodeGetPrimaryExchange(this string exchange,
            SecurityType securityType = SecurityType.Equity,
            string market = Market.USA)
        {
            return exchange.GetPrimaryExchange(securityType, market).Code;
        }

        /// <summary>
        /// Gets the exchange as PrimaryExchange object.
        /// </summary>
        /// <remarks>Useful for performance</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exchange GetPrimaryExchange(this string exchange,
            SecurityType securityType = SecurityType.Equity,
            string market = Market.USA)
        {
            var primaryExchange = Exchange.UNKNOWN;
            if (string.IsNullOrEmpty(exchange))
            {
                return primaryExchange;
            }

            if (securityType == SecurityType.Equity)
            {
                switch (exchange.LazyToUpper())
                {
                    case "T":
                    case "Q":
                    case "NASDAQ":
                    case "NASDAQ_OMX":
                        return Exchange.NASDAQ;
                    case "Z":
                    case "BATS":
                    case "BATS Z":
                    case "BATS_Z":
                        return Exchange.BATS;
                    case "P":
                    case "ARCA":
                        return Exchange.ARCA;
                    case "N":
                    case "NYSE":
                        return Exchange.NYSE;
                    case "C":
                    case "NSX":
                    case "NSE":
                        if (market == Market.USA)
                        {
                            return Exchange.NSX;
                        }
                        else if (market == Market.India)
                        {
                            return Exchange.NSE;
                        }
                        return Exchange.UNKNOWN;
                    case "D":
                    case "FINRA":
                        return Exchange.FINRA;
                    case "I":
                    case "ISE":
                        return Exchange.ISE;
                    case "M":
                    case "CSE":
                        return Exchange.CSE;
                    case "W":
                    case "CBOE":
                        return Exchange.CBOE;
                    case "A":
                    case "AMEX":
                        return Exchange.AMEX;
                    case "SIAC":
                        return Exchange.SIAC;
                    case "J":
                    case "EDGA":
                        return Exchange.EDGA;
                    case "K":
                    case "EDGX":
                        return Exchange.EDGX;
                    case "B":
                    case "NASDAQ BX":
                    case "NASDAQ_BX":
                        return Exchange.NASDAQ_BX;
                    case "X":
                    case "NASDAQ PSX":
                    case "NASDAQ_PSX":
                        return Exchange.NASDAQ_PSX;
                    case "Y":
                    case "BATS Y":
                    case "BATS_Y":
                    case "BYX":
                        return Exchange.BATS_Y;
                    case "BB":
                    case "BOSTON":
                        return Exchange.BOSTON;
                    case "BSE":
                        return Exchange.BSE;
                    case "IEX":
                        return Exchange.IEX;
                    case "SMART":
                        return Exchange.SMART;
                    case "OTCX":
                        return Exchange.OTCX;
                    case "MP":
                    case "MIAX PEARL":
                    case "MIAX_PEARL":
                        return Exchange.MIAX_PEARL;
                    case "L":
                    case "LTSE":
                        return Exchange.LTSE;
                    case "MM":
                    case "MEMX":
                        return Exchange.MEMX;
                    case "CSFB":
                        return Exchange.CSFB;
                }
            }
            else if (securityType == SecurityType.Option)
            {
                switch (exchange.LazyToUpper())
                {
                    case "A":
                    case "AMEX":
                        return Exchange.AMEX_Options;
                    case "M":
                    case "MIAX":
                        return Exchange.MIAX;
                    case "ME":
                    case "MIAX EMERALD":
                    case "MIAX_EMERALD":
                        return Exchange.MIAX_EMERALD;
                    case "MP":
                    case "MIAX PEARL":
                    case "MIAX_PEARL":
                        return Exchange.MIAX_PEARL;
                    case "I":
                    case "ISE":
                        return Exchange.ISE;
                    case "H":
                    case "ISE GEMINI":
                    case "ISE_GEMINI":
                        return Exchange.ISE_GEMINI;
                    case "J":
                    case "ISE MERCURY":
                    case "ISE_MERCURY":
                        return Exchange.ISE_MERCURY;
                    case "O":
                    case "OPRA":
                        return Exchange.OPRA;
                    case "W":
                    case "C2":
                        return Exchange.C2;
                    case "XNDQ":
                        return Exchange.NASDAQ_Options;
                    case "ARCX":
                        return Exchange.ARCA_Options;
                    case "EDGO":
                        return Exchange.EDGO;
                    case "BOX":
                    case "B":
                        return Exchange.BOX;
                    case "PHLX":
                        return Exchange.PHLX;
                    case "SPHR":
                    case "MIAX SAPPHIRE":
                    case "MIAX_SAPPHIRE":
                        return Exchange.MIAX_SAPPHIRE;
                    default:
                        return Exchange.UNKNOWN;
                }
            }
            else if (securityType == SecurityType.Future || securityType == SecurityType.FutureOption)
            {
                switch (exchange.LazyToUpper())
                {
                    case "CME":
                        return Exchange.CME;
                    case "CBOT":
                        return Exchange.CBOT;
                    case "NYMEX":
                        return Exchange.NYMEX;
                    case "ICE":
                        return Exchange.ICE;
                    case "CFE":
                        return Exchange.CFE;
                    case "COMEX":
                        return Exchange.COMEX;
                    case "NYSELIFFE":
                        return Exchange.NYSELIFFE;
                    case "EUREX":
                        return Exchange.EUREX;
                    default:
                        return Exchange.UNKNOWN;
                }
            }
            return Exchange.UNKNOWN;
        }
    }

    /// <summary>
    /// Defines the different channel status values
    /// </summary>
    public static class ChannelStatus
    {
        /// <summary>
        /// The channel is empty
        /// </summary>
        public const string Vacated = "channel_vacated";

        /// <summary>
        /// The channel has subscribers
        /// </summary>
        public const string Occupied = "channel_occupied";
    }

    /// <summary>
    /// Represents the types deployment targets for algorithms
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeploymentTarget
    {
        /// <summary>
        /// Cloud Platform (0)
        /// </summary>
        CloudPlatform,

        /// <summary>
        /// Local Platform (1)
        /// </summary>
        LocalPlatform,

        /// <summary>
        /// Private Cloud Platform (2)
        /// </summary>
        PrivateCloudPlatform
    }

    /// <summary>
    /// Represents the deployment modes of an algorithm
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AlgorithmMode
    {
        /// <summary>
        /// Live (0)
        /// </summary>
        Live,

        /// <summary>
        /// Optimization (1)
        /// </summary>
        Optimization,

        /// <summary>
        /// Backtesting (2)
        /// </summary>
        Backtesting,

        /// <summary>
        /// Research (3)
        /// </summary>
        Research
    }
}
