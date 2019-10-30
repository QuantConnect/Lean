﻿/*
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
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

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
        public static string JsonFormat = "yyyy-MM-ddTHH:mm:ss";
        /// MySQL Format Date Representation
        public const string DB = "yyyy-MM-dd HH:mm:ss";
        /// QuantConnect UX Date Representation
        public const string UI = "yyyy-MM-dd HH:mm:ss";
        /// en-US format
        public const string US = "M/d/yyyy h:mm:ss tt";
        /// Date format of QC forex data
        public const string Forex = "yyyyMMdd HH:mm:ss.ffff";
        /// YYYYMM Year and Month Character Date Representation (used for futures)
        public const string YearMonth = "yyyyMM";
    }

    /// <summary>
    /// Singular holding of assets from backend live nodes:
    /// </summary>
    [JsonObject]
    public class Holding
    {
        /// Symbol of the Holding:
        public Symbol Symbol = Symbol.Empty;

        /// Type of the security
        public SecurityType Type;

        /// The currency symbol of the holding, such as $
        public string CurrencySymbol;

        /// Average Price of our Holding in the currency the symbol is traded in
        public decimal AveragePrice;

        /// Quantity of Symbol We Hold.
        public decimal Quantity;

        /// Current Market Price of the Asset in the currency the symbol is traded in
        public decimal MarketPrice;

        /// Current market conversion rate into the account currency
        public decimal? ConversionRate;

        /// Current market value of the holding
        public decimal MarketValue;

        /// Current unrealized P/L of the holding
        public decimal UnrealizedPnL;

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
            Type = holding.Type;
            Quantity = holding.Quantity;
            MarketValue = holding.HoldingsValue;
            CurrencySymbol = Currencies.GetCurrencySymbol(security.QuoteCurrency.Symbol);
            ConversionRate = security.QuoteCurrency.ConversionRate;

            var rounding = 2;
            if (holding.Type == SecurityType.Forex || holding.Type == SecurityType.Cfd)
            {
                rounding = 5;
            }
            //do not round crypto
            else if (holding.Type == SecurityType.Crypto)
            {
                rounding = 28;
            }


            AveragePrice = Math.Round(holding.AveragePrice, rounding);
            MarketPrice = Math.Round(holding.Price, rounding);
            UnrealizedPnL = Math.Round(holding.UnrealizedProfit, 2);
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
                Type = Type,
                Quantity = Quantity,
                MarketPrice = MarketPrice,
                MarketValue = MarketValue,
                UnrealizedPnL = UnrealizedPnL,
                ConversionRate  = ConversionRate,
                CurrencySymbol = CurrencySymbol
            };
        }

        /// <summary>
        /// Writes out the properties of this instance to string
        /// </summary>
        public override string ToString()
        {
            var value = Invariant($"{Symbol.Value}: {Quantity} @ ") +
                        Invariant($"{CurrencySymbol}{AveragePrice} - ") +
                        Invariant($"Market: {CurrencySymbol}{MarketPrice}");

            if (ConversionRate != 1m)
            {
                value += Invariant($" - Conversion: {ConversionRate}");
            }

            return value;
        }
    }

    /// <summary>
    /// Processing runmode of the backtest.
    /// </summary>
    /// <obsolete>The runmode enum is now obsolete and all tasks are run in series mode. This was done to ensure algorithms have memory of the day before.</obsolete>
    public enum RunMode
    {
        /// Automatically detect the runmode of the algorithm: series for minute data, parallel for second-tick
        Automatic,
        /// Series runmode for the algorithm
        Series,
        /// Parallel runmode for the algorithm
        Parallel
    }

    /// <summary>
    /// Represents the types of environments supported by brokerages for trading
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BrokerageEnvironment
    {
        /// <summary>
        /// Live trading
        /// </summary>
        [EnumMember(Value = "live")]
        Live,

        /// <summary>
        /// Paper trading
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
        /// C# Language Project
        /// </summary>
        [EnumMember(Value = "C#")]
        CSharp,

        /// <summary>
        /// FSharp Project
        /// </summary>
        [EnumMember(Value = "F#")]
        FSharp,

        /// <summary>
        /// Visual Basic Project
        /// </summary>
        [EnumMember(Value = "VB")]
        VisualBasic,

        /// <summary>
        /// Java Language Project
        /// </summary>
        [EnumMember(Value = "Ja")]
        Java,

        /// <summary>
        /// Python Language Project
        /// </summary>
        [EnumMember(Value = "Py")]
        Python
    }


    /// <summary>
    /// User / Algorithm Job Subscription Level
    /// </summary>
    public enum UserPlan
    {
        /// <summary>
        /// Free User (Backtesting).
        /// </summary>
        Free,

        /// <summary>
        /// Hobbyist User with Included 512mb Server.
        /// </summary>
        Hobbyist,

        /// <summary>
        /// Professional plan for financial advisors
        /// </summary>
        Professional
    }


    /// <summary>
    /// Live server types available through the web IDE. / QC deployment.
    /// </summary>
    public enum ServerType
    {
        /// <summary>
        /// Additional server
        /// </summary>
        Server512,

        /// <summary>
        /// Upgraded server
        /// </summary>
        Server1024,

        /// <summary>
        /// Server with 2048 MB Ram.
        /// </summary>
        Server2048
    }


    /// <summary>
    /// Type of tradable security / underlying asset
    /// </summary>
    public enum SecurityType
    {
        /// <summary>
        /// Base class for all security types:
        /// </summary>
        Base,

        /// <summary>
        /// US Equity Security
        /// </summary>
        Equity,

        /// <summary>
        /// Option Security Type
        /// </summary>
        Option,

        /// <summary>
        /// Commodity Security Type
        /// </summary>
        Commodity,

        /// <summary>
        /// FOREX Security
        /// </summary>
        Forex,

        /// <summary>
        /// Future Security Type
        /// </summary>
        Future,

        /// <summary>
        /// Contract For a Difference Security Type.
        /// </summary>
        Cfd,

        /// <summary>
        /// Cryptocurrency Security Type.
        /// </summary>
        Crypto
    }

    /// <summary>
    /// Account type: margin or cash
    /// </summary>
    public enum AccountType
    {
        /// <summary>
        /// Margin account type
        /// </summary>
        Margin,

        /// <summary>
        /// Cash account type
        /// </summary>
        Cash
    }

    /// <summary>
    /// Market data style: is the market data a summary (OHLC style) bar, or is it a time-price value.
    /// </summary>
    public enum MarketDataType
    {
        /// Base market data type
        Base,
        /// TradeBar market data type (OHLC summary bar)
        TradeBar,
        /// Tick market data type (price-time pair)
        Tick,
        /// Data associated with an instrument
        Auxiliary,
        /// QuoteBar market data type [Bid(OHLC), Ask(OHLC) and Mid(OHLC) summary bar]
        QuoteBar,
        /// Option chain data
        OptionChain,
        /// Futures chain data
        FuturesChain
    }

    /// <summary>
    /// Datafeed enum options for selecting the source of the datafeed.
    /// </summary>
    public enum DataFeedEndpoint
    {
        /// Backtesting Datafeed Endpoint
        Backtesting,
        /// Loading files off the local system
        FileSystem,
        /// Getting datafeed from a QC-Live-Cloud
        LiveTrading,
        /// Database
        Database
    }

    /// <summary>
    /// Cloud storage permission options.
    /// </summary>
    public enum StoragePermissions
    {
        /// Public Storage Permissions
        Public,

        /// Authenticated Read Storage Permissions
        Authenticated
    }

    /// <summary>
    /// Types of tick data
    /// </summary>
    /// <remarks>QuantConnect currently only has trade, quote, open interest tick data.</remarks>
    public enum TickType
    {
        /// Trade type tick object.
        Trade,
        /// Quote type tick object.
        Quote,
        /// Open Interest type tick object (for options, futures)
        OpenInterest
    }

    /// <summary>
    /// Specifies the type of <see cref="QuantConnect.Data.Market.Delisting"/> data
    /// </summary>
    public enum DelistingType
    {
        /// <summary>
        /// Specifies a warning of an imminent delisting
        /// </summary>
        Warning = 0,

        /// <summary>
        /// Specifies the symbol has been delisted
        /// </summary>
        Delisted = 1
    }

    /// <summary>
    /// Specifies the type of <see cref="QuantConnect.Data.Market.Split"/> data
    /// </summary>
    public enum SplitType
    {
        /// <summary>
        /// Specifies a warning of an imminent split event
        /// </summary>
        Warning = 0,

        /// <summary>
        /// Specifies the symbol has been split
        /// </summary>
        SplitOccurred = 1
    }

    /// <summary>
    /// Resolution of data requested.
    /// </summary>
    /// <remarks>Always sort the enum from the smallest to largest resolution</remarks>
    public enum Resolution
    {
        /// Tick Resolution (1)
        Tick,
        /// Second Resolution (2)
        Second,
        /// Minute Resolution (3)
        Minute,
        /// Hour Resolution (4)
        Hour,
        /// Daily Resolution (5)
        Daily
    }

    /// <summary>
    /// Specifies the different types of options
    /// </summary>
    public enum OptionRight
    {
        /// <summary>
        /// A call option, the right to buy at the strike price
        /// </summary>
        Call,

        /// <summary>
        /// A put option, the right to sell at the strike price
        /// </summary>
        Put
    }

    /// <summary>
    /// Specifies the style of an option
    /// </summary>
    public enum OptionStyle
    {
        /// <summary>
        /// American style options are able to be exercised at any time on or before the expiration date
        /// </summary>
        American,

        /// <summary>
        /// European style options are able to be exercised on the expiration date only.
        /// </summary>
        European
    }

    /// <summary>
    /// Specifies the type of settlement in derivative deals
    /// </summary>
    public enum SettlementType
    {
        /// <summary>
        /// Physical delivery of the underlying security
        /// </summary>
        PhysicalDelivery,

        /// <summary>
        /// Cash is paid/received on settlement
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
            ChartSubscription = "Strategy Equity";
        }

        /// <summary>
        /// Register this control packet as not defaults.
        /// </summary>
        public bool Initialized;

        /// <summary>
        /// Current run status of the algorithm id.
        /// </summary>
        public AlgorithmStatus Status;

        /// <summary>
        /// Currently requested chart.
        /// </summary>
        public string ChartSubscription;

        /// <summary>
        /// True if there's subscribers on the channel
        /// </summary>
        public bool HasSubscribers;
    }

    /// <summary>
    /// States of a live deployment.
    /// </summary>
    public enum AlgorithmStatus
    {
        /// Error compiling algorithm at start
        DeployError,    //1
        /// Waiting for a server
        InQueue,        //2
        /// Running algorithm
        Running,        //3
        /// Stopped algorithm or exited with runtime errors
        Stopped,        //4
        /// Liquidated algorithm
        Liquidated,     //5
        /// Algorithm has been deleted
        Deleted,        //6
        /// Algorithm completed running
        Completed,      //7
        /// Runtime Error Stoped Algorithm
        RuntimeError,    //8
        /// Error in the algorithm id (not used).
        Invalid,
        /// The algorithm is logging into the brokerage
        LoggingIn,
        /// The algorithm is initializing
        Initializing,
        /// History status update
        History
    }

    /// <summary>
    /// Specifies where a subscription's data comes from
    /// </summary>
    public enum SubscriptionTransportMedium
    {
        /// <summary>
        /// The subscription's data comes from disk
        /// </summary>
        LocalFile,

        /// <summary>
        /// The subscription's data is downloaded from a remote source
        /// </summary>
        RemoteFile,

        /// <summary>
        /// The subscription's data comes from a rest call that is polled and returns a single line/data point of information
        /// </summary>
        Rest,

        /// <summary>
        /// The subscription's data is streamed
        /// </summary>
        Streaming
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
        /// The raw price with dividends added to cash book
        /// </summary>
        Raw,
        /// <summary>
        /// The adjusted prices with splits and dividendends factored in
        /// </summary>
        Adjusted,
        /// <summary>
        /// The adjusted prices with only splits factored in, dividends paid out to the cash book
        /// </summary>
        SplitAdjusted,
        /// <summary>
        /// The split adjusted price plus dividends
        /// </summary>
        TotalReturn
    }

    /// <summary>
    /// Global Market Short Codes and their full versions: (used in tick objects)
    /// </summary>
    public static class MarketCodes
    {
        /// US Market Codes
        public static Dictionary<string, string> US = new Dictionary<string, string>()
        {
            {"A", "American Stock Exchange"},
            {"B", "Boston Stock Exchange"},
            {"C", "National Stock Exchange"},
            {"D", "FINRA ADF"},
            {"I", "International Securities Exchange"},
            {"J", "Direct Edge A"},
            {"K", "Direct Edge X"},
            {"M", "Chicago Stock Exchange"},
            {"N", "New York Stock Exchange"},
            {"P", "Nyse Arca Exchange"},
            {"Q", "NASDAQ OMX"},
            {"T", "NASDAQ OMX"},
            {"U", "OTC Bulletin Board"},
            {"u", "Over-the-Counter trade in Non-NASDAQ issue"},
            {"W", "Chicago Board Options Exchange"},
            {"X", "Philadelphia Stock Exchange"},
            {"Y", "BATS Y-Exchange, Inc"},
            {"Z", "BATS Exchange, Inc"},
            {"IEX", "Investors Exchange"},
        };

        /// Canada Market Short Codes:
        public static Dictionary<string, string> Canada = new Dictionary<string, string>()
        {
            {"T", "Toronto"},
            {"V", "Venture"}
        };
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
    /// US Public Holidays - Not Tradeable:
    /// </summary>
    public static class USHoliday
    {
        /// <summary>
        /// Public Holidays
        /// </summary>
        public static readonly HashSet<DateTime> Dates = new HashSet<DateTime>
        {
            /* New Years Day*/
            new DateTime(1998, 01, 01),
            new DateTime(1999, 01, 01),
            new DateTime(2001, 01, 01),
            new DateTime(2002, 01, 01),
            new DateTime(2003, 01, 01),
            new DateTime(2004, 01, 01),
            new DateTime(2006, 01, 02),
            new DateTime(2007, 01, 01),
            new DateTime(2008, 01, 01),
            new DateTime(2009, 01, 01),
            new DateTime(2010, 01, 01),
            new DateTime(2011, 01, 01),
            new DateTime(2012, 01, 02),
            new DateTime(2013, 01, 01),
            new DateTime(2014, 01, 01),
            new DateTime(2015, 01, 01),
            new DateTime(2016, 01, 01),
            new DateTime(2017, 01, 02),
            new DateTime(2018, 01, 01),
            new DateTime(2019, 01, 01),
            new DateTime(2020, 01, 01),
            new DateTime(2021, 01, 01),
            new DateTime(2022, 01, 01),
            new DateTime(2023, 01, 02),

            /* Day of Mouring */
            new DateTime(2007, 01, 02),

            /* World Trade Center */
            new DateTime(2001, 09, 11),
            new DateTime(2001, 09, 12),
            new DateTime(2001, 09, 13),
            new DateTime(2001, 09, 14),

            /* Regan Funeral */
            new DateTime(2004, 06, 11),

            /* Hurricane Sandy */
            new DateTime(2012, 10, 29),
            new DateTime(2012, 10, 30),

            /* Martin Luther King Jnr Day*/
            new DateTime(1998, 01, 19),
            new DateTime(1999, 01, 18),
            new DateTime(2000, 01, 17),
            new DateTime(2001, 01, 15),
            new DateTime(2002, 01, 21),
            new DateTime(2003, 01, 20),
            new DateTime(2004, 01, 19),
            new DateTime(2005, 01, 17),
            new DateTime(2006, 01, 16),
            new DateTime(2007, 01, 15),
            new DateTime(2008, 01, 21),
            new DateTime(2009, 01, 19),
            new DateTime(2010, 01, 18),
            new DateTime(2011, 01, 17),
            new DateTime(2012, 01, 16),
            new DateTime(2013, 01, 21),
            new DateTime(2014, 01, 20),
            new DateTime(2015, 01, 19),
            new DateTime(2016, 01, 18),
            new DateTime(2017, 01, 16),
            new DateTime(2018, 01, 15),
            new DateTime(2019, 01, 21),
            new DateTime(2020, 01, 20),
            new DateTime(2021, 01, 18),
            new DateTime(2022, 01, 17),
            new DateTime(2023, 01, 16),

            /* Washington / Presidents Day */
            new DateTime(1998, 02, 16),
            new DateTime(1999, 02, 15),
            new DateTime(2000, 02, 21),
            new DateTime(2001, 02, 19),
            new DateTime(2002, 02, 18),
            new DateTime(2003, 02, 17),
            new DateTime(2004, 02, 16),
            new DateTime(2005, 02, 21),
            new DateTime(2006, 02, 20),
            new DateTime(2007, 02, 19),
            new DateTime(2008, 02, 18),
            new DateTime(2009, 02, 16),
            new DateTime(2010, 02, 15),
            new DateTime(2011, 02, 21),
            new DateTime(2012, 02, 20),
            new DateTime(2013, 02, 18),
            new DateTime(2014, 02, 17),
            new DateTime(2015, 02, 16),
            new DateTime(2016, 02, 15),
            new DateTime(2017, 02, 20),
            new DateTime(2018, 02, 19),
            new DateTime(2019, 02, 18),
            new DateTime(2020, 02, 17),
            new DateTime(2021, 02, 15),
            new DateTime(2022, 02, 21),
            new DateTime(2023, 02, 20),

            /* Good Friday */
            new DateTime(1998, 04, 10),
            new DateTime(1999, 04, 02),
            new DateTime(2000, 04, 21),
            new DateTime(2001, 04, 13),
            new DateTime(2002, 03, 29),
            new DateTime(2003, 04, 18),
            new DateTime(2004, 04, 09),
            new DateTime(2005, 03, 25),
            new DateTime(2006, 04, 14),
            new DateTime(2007, 04, 06),
            new DateTime(2008, 03, 21),
            new DateTime(2009, 04, 10),
            new DateTime(2010, 04, 02),
            new DateTime(2011, 04, 22),
            new DateTime(2012, 04, 06),
            new DateTime(2013, 03, 29),
            new DateTime(2014, 04, 18),
            new DateTime(2015, 04, 03),
            new DateTime(2016, 03, 25),
            new DateTime(2017, 04, 14),
            new DateTime(2018, 03, 30),
            new DateTime(2019, 04, 19),
            new DateTime(2020, 04, 10),
            new DateTime(2021, 04, 02),
            new DateTime(2022, 04, 15),
            new DateTime(2023, 04, 07),

            /* Memorial Day */
            new DateTime(1998, 05, 25),
            new DateTime(1999, 05, 31),
            new DateTime(2000, 05, 29),
            new DateTime(2001, 05, 28),
            new DateTime(2002, 05, 27),
            new DateTime(2003, 05, 26),
            new DateTime(2004, 05, 31),
            new DateTime(2005, 05, 30),
            new DateTime(2006, 05, 29),
            new DateTime(2007, 05, 28),
            new DateTime(2008, 05, 26),
            new DateTime(2009, 05, 25),
            new DateTime(2010, 05, 31),
            new DateTime(2011, 05, 30),
            new DateTime(2012, 05, 28),
            new DateTime(2013, 05, 27),
            new DateTime(2014, 05, 26),
            new DateTime(2015, 05, 25),
            new DateTime(2016, 05, 30),
            new DateTime(2017, 05, 29),
            new DateTime(2018, 05, 28),
            new DateTime(2019, 05, 27),
            new DateTime(2020, 05, 25),
            new DateTime(2021, 05, 31),
            new DateTime(2022, 05, 30),
            new DateTime(2023, 05, 29),

            /* Independence Day */
            new DateTime(1998, 07, 03),
            new DateTime(1999, 07, 05),
            new DateTime(2000, 07, 04),
            new DateTime(2001, 07, 04),
            new DateTime(2002, 07, 04),
            new DateTime(2003, 07, 04),
            new DateTime(2004, 07, 05),
            new DateTime(2005, 07, 04),
            new DateTime(2006, 07, 04),
            new DateTime(2007, 07, 04),
            new DateTime(2008, 07, 04),
            new DateTime(2009, 07, 03),
            new DateTime(2010, 07, 05),
            new DateTime(2011, 07, 04),
            new DateTime(2012, 07, 04),
            new DateTime(2013, 07, 04),
            new DateTime(2014, 07, 04),
            new DateTime(2014, 07, 04),
            new DateTime(2015, 07, 03),
            new DateTime(2016, 07, 04),
            new DateTime(2017, 07, 04),
            new DateTime(2018, 07, 04),
            new DateTime(2019, 07, 04),
            new DateTime(2020, 07, 04),
            new DateTime(2021, 07, 05),
            new DateTime(2022, 07, 04),
            new DateTime(2023, 07, 04),

            /* Labor Day */
            new DateTime(1998, 09, 07),
            new DateTime(1999, 09, 06),
            new DateTime(2000, 09, 04),
            new DateTime(2001, 09, 03),
            new DateTime(2002, 09, 02),
            new DateTime(2003, 09, 01),
            new DateTime(2004, 09, 06),
            new DateTime(2005, 09, 05),
            new DateTime(2006, 09, 04),
            new DateTime(2007, 09, 03),
            new DateTime(2008, 09, 01),
            new DateTime(2009, 09, 07),
            new DateTime(2010, 09, 06),
            new DateTime(2011, 09, 05),
            new DateTime(2012, 09, 03),
            new DateTime(2013, 09, 02),
            new DateTime(2014, 09, 01),
            new DateTime(2015, 09, 07),
            new DateTime(2016, 09, 05),
            new DateTime(2017, 09, 04),
            new DateTime(2018, 09, 03),
            new DateTime(2019, 09, 02),
            new DateTime(2020, 09, 07),
            new DateTime(2021, 09, 06),
            new DateTime(2022, 09, 05),
            new DateTime(2023, 09, 04),

            /* Thanksgiving Day */
            new DateTime(1998, 11, 26),
            new DateTime(1999, 11, 25),
            new DateTime(2000, 11, 23),
            new DateTime(2001, 11, 22),
            new DateTime(2002, 11, 28),
            new DateTime(2003, 11, 27),
            new DateTime(2004, 11, 25),
            new DateTime(2005, 11, 24),
            new DateTime(2006, 11, 23),
            new DateTime(2007, 11, 22),
            new DateTime(2008, 11, 27),
            new DateTime(2009, 11, 26),
            new DateTime(2010, 11, 25),
            new DateTime(2011, 11, 24),
            new DateTime(2012, 11, 22),
            new DateTime(2013, 11, 28),
            new DateTime(2014, 11, 27),
            new DateTime(2015, 11, 26),
            new DateTime(2016, 11, 24),
            new DateTime(2017, 11, 23),
            new DateTime(2018, 11, 22),
            new DateTime(2019, 11, 28),
            new DateTime(2020, 11, 26),
            new DateTime(2021, 11, 25),
            new DateTime(2022, 11, 24),
            new DateTime(2023, 11, 23),

            /* Christmas */
            new DateTime(1998, 12, 25),
            new DateTime(1999, 12, 24),
            new DateTime(2000, 12, 25),
            new DateTime(2001, 12, 25),
            new DateTime(2002, 12, 25),
            new DateTime(2003, 12, 25),
            new DateTime(2004, 12, 24),
            new DateTime(2005, 12, 26),
            new DateTime(2006, 12, 25),
            new DateTime(2007, 12, 25),
            new DateTime(2008, 12, 25),
            new DateTime(2009, 12, 25),
            new DateTime(2010, 12, 24),
            new DateTime(2011, 12, 26),
            new DateTime(2012, 12, 25),
            new DateTime(2013, 12, 25),
            new DateTime(2014, 12, 25),
            new DateTime(2015, 12, 25),
            new DateTime(2016, 12, 26),
            new DateTime(2017, 12, 25),
            new DateTime(2018, 12, 25),
            new DateTime(2019, 12, 25),
            new DateTime(2020, 12, 25),
            new DateTime(2021, 12, 24),
            new DateTime(2022, 12, 26),
            new DateTime(2023, 12, 25)
        };
    }
}
