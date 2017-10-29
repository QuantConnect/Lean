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
using System.Linq;
using System.Globalization;
using System.IO;
using System.Timers;

namespace QuantConnect.ToolBox.IQFeed
{
    public class Level1ServerDisconnectedArgs : Level1TextLineEventArgs
    {
        public Level1ServerDisconnectedArgs(string line)
            : base(line)
        {
        }
    }
    public class Level1ServerReconnectFailedArgs : Level1TextLineEventArgs
    {
        public Level1ServerReconnectFailedArgs(string line)
            : base(line)
        {
        }
    }

    public class Level1TextLineEventArgs : EventArgs
    {
        public readonly string TextLine;
        public Level1TextLineEventArgs(string line)
        {
            TextLine = line;
        }
    }
 
    public class Level1SummaryUpdateEventArgs : EventArgs
    {
        public enum UpdateType { Trade, ExtendedTrade, Bid, Ask, Other }
        public Level1SummaryUpdateEventArgs(string line)
        {
            try
            {                
                var fields = line.Split(',');
                _summary = fields[0] == "P";
                _symbol = fields[1];
                _notFound = line.Contains("Not Found");

                if (_notFound) return;
                if (!double.TryParse(fields[3], out _last)) _last = 0;
                if (!double.TryParse(fields[4], out _change)) _change = 0;
                if (!double.TryParse(fields[5], out _change)) _percentChange = 0;
                if (!int.TryParse(fields[6], out _totalVolume)) _totalVolume = 0;
                if (!int.TryParse(fields[7], out _incrementalVolume)) _incrementalVolume = 0;
                if (!double.TryParse(fields[8], out _high)) _high = 0;
                if (!double.TryParse(fields[9], out _low)) _low = 0;
                if (!double.TryParse(fields[10], out _bid)) _bid = 0;
                if (!double.TryParse(fields[11], out _ask)) _ask = 0;
                if (!int.TryParse(fields[12], out _bidSize)) _bidSize = 0;
                if (!int.TryParse(fields[13], out _askSize)) _askSize = 0;
                if (!int.TryParse(fields[14], out _tick)) _tick = 0;
                if (!int.TryParse(fields[15], out _bidTick)) _bidTick = 0;
                if (!double.TryParse(fields[16], out _range)) _range = 0;
                if (!string.IsNullOrEmpty(fields[17]))
                {
                    switch (fields[17].Substring(fields[17].Length - 1, 1))
                    {
                        case "t":
                            _updateType = UpdateType.Trade;
                            break;
                        case "T":
                            _updateType = UpdateType.ExtendedTrade;
                            break;
                        case "b":
                            _updateType = UpdateType.Bid;
                            break;
                        case "a":
                            _updateType = UpdateType.Ask;
                            break;
                        default:
                            _updateType = UpdateType.Other;
                            break;
                    }
                }
                else
                {
                    return;
                }
                if (!int.TryParse(fields[18], out _openInterest)) _openInterest = 0;
                if (!double.TryParse(fields[19], out _open)) _open = 0;
                if (!double.TryParse(fields[20], out _close)) _close = 0;
                if (!double.TryParse(fields[21], out _spread)) _spread = 0;
                if (!double.TryParse(fields[23], out _settle)) _settle = 0;
                if (!int.TryParse(fields[24], out _delay)) _delay = 0;
                _shortRestricted = false;
                if (fields[26] == "R") _shortRestricted = true;
                if (!double.TryParse(fields[27], out _netAssetValue)) _netAssetValue = 0;
                if (!double.TryParse(fields[28], out _averageMaturity)) _averageMaturity = 0;
                if (!double.TryParse(fields[29], out _7DayYield)) _7DayYield = 0;
                if (!DateTime.TryParseExact(fields[30], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _lastTradeDate)) _lastTradeDate = DateTime.MinValue;
                if (!double.TryParse(fields[32], out _extendedTradingLast)) _extendedTradingLast = 0;
                if (!int.TryParse(fields[34], out _regionalVolume)) _regionalVolume = 0;
                if (!double.TryParse(fields[35], out _netAssetValue2)) _netAssetValue2 = 0;
                if (!double.TryParse(fields[36], out _extendedTradingChange)) _extendedTradingChange = 0;
                if (!double.TryParse(fields[37], out _extendedTradingDifference)) _extendedTradingDifference = 0;
                if (!double.TryParse(fields[38], out _priceEarningsRatio)) _priceEarningsRatio = 0;
                if (!double.TryParse(fields[39], out _percentOffAverageVolume)) _percentOffAverageVolume = 0;
                if (!double.TryParse(fields[40], out _bidChange)) _bidChange = 0;
                if (!double.TryParse(fields[41], out _askChange)) _askChange = 0;
                if (!double.TryParse(fields[42], out _changeFromOpen)) _changeFromOpen = 0;
                _marketOpen = false;
                if (fields[43] == "1") _marketOpen = true;
                if (!double.TryParse(fields[44], out _volatility)) _volatility = 0;
                if (!double.TryParse(fields[45], out _marketCapitalization)) _marketCapitalization = 0;
                _fractionDisplayCode = fields[46];
                _decimalPrecision = fields[47];
                _daysToExpiration = fields[48];
                if (!int.TryParse(fields[49], out _previousDayVolume)) _previousDayVolume = 0;
                if (!double.TryParse(fields[51], out _openRange1)) _openRange1 = 0;
                if (!double.TryParse(fields[52], out _closeRange1)) _closeRange1 = 0;
                if (!double.TryParse(fields[53], out _openRange2)) _openRange2 = 0;
                if (!double.TryParse(fields[54], out _closeRange2)) _closeRange2 = 0;
                if (!int.TryParse(fields[55], out _numberOfTradesToday)) _numberOfTradesToday = 0;
                _bidTime = new Time(fields[56]);
                _askTime = new Time(fields[57]);
                if (!double.TryParse(fields[58], out _vwap)) _vwap = 0;
                if (!int.TryParse(fields[59], out _tickId)) _tickId = 0;
                _financialStatusIndicator = fields[60];
                if (!DateTime.TryParseExact(fields[61], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _settlementDate)) _settlementDate = DateTime.MinValue;
                if (!int.TryParse(fields[62], out _tradeMarketCenter)) _tradeMarketCenter = 0;
                if (!int.TryParse(fields[63], out _bidMarketCenter)) _bidMarketCenter = 0;
                if (!int.TryParse(fields[64], out _askMarketCenter)) _askMarketCenter = 0;
                _tradeTime = new Time(fields[65]);
                _availableRegions = fields[66];
            }
            catch (Exception err)
            {
                Logging.Log.Debug("IQLevel1Client.Init(): " + err.Message);
            }
        }

        public bool NotFound { get { return _notFound; } }
        public bool Summary { get { return _summary; } }
        public string Symbol { get { return _symbol; } } 
        public double Last { get { return _last; } }
        public double Change { get { return _change; } }
        public double PercentChange { get { return _percentChange; } }
        public int TotalVolume { get { return _totalVolume; } }
        public int IncrementalVolume { get { return _incrementalVolume; } }
        public double High { get { return _high; } }
        public double Low { get { return _low; } }
        public double Bid { get { return _bid; } }
        public double Ask { get { return _ask; } }
        public int BidSize { get { return _bidSize; } }
        public int AskSize { get { return _askSize; } }
        public int Tick { get { return _tick; } }
        public int BidTick { get { return _bidTick; } }
        public double Range { get { return _range; } }
        public UpdateType TypeOfUpdate { get { return _updateType; } }
        public int OpenInterest { get { return _openInterest; } }
        public double Open { get { return _open; } }
        public double Close { get { return _close; } }
        public double Spread { get { return _spread; } }
        public double Settle { get { return _settle; } }
        public int Delay { get { return _delay; } }
        public bool ShortRestricted { get { return _shortRestricted; } }
        public double NetAssetValue { get { return _netAssetValue; } }
        public double AverageMaturity { get { return _averageMaturity; } }
        public double SevenDayYield { get { return _7DayYield; } }
        public DateTime LastTradeDate { get { return _lastTradeDate; } }
        public double ExtendedTradingLast { get { return _extendedTradingLast; } }
        public int RegionalVolume { get { return _regionalVolume; } }
        public double NetAssetValue2 { get { return _netAssetValue2; } }
        public double ExtendedTradingChange { get { return _extendedTradingChange; } }
        public double ExtendedTradingDifference { get { return _extendedTradingDifference; } }
        public double PriceEarningsRatio { get { return _priceEarningsRatio; } }
        public double PercentOffAverageVolume { get { return _percentOffAverageVolume; } }
        public double BidChange { get { return _bidChange; } }
        public double AskChange { get { return _askChange; } }
        public double ChangeFromOpen { get { return _changeFromOpen; } }
        public bool MarketOpen { get { return _marketOpen; } }
        public double Volatility { get { return _volatility; } }
        public double MarketCapitalization { get { return _marketCapitalization; } }
        public string FractionDisplayCode { get { return _fractionDisplayCode; } }
        public string DecimalPrecision { get { return _decimalPrecision; } }
        public string DaysToExpiration { get { return _daysToExpiration; } }
        public int PreviousDayVolume { get { return _previousDayVolume; } }
        public double OpenRange1 { get { return _openRange1; } }
        public double CloseRange1 { get { return _closeRange1; } }
        public double OpenRange2 { get { return _openRange2; } }
        public double CloseRange2 { get { return _closeRange2; } }
        public int NumberofTradesToday { get { return _numberOfTradesToday; } }
        public Time BidTime { get { return _bidTime; } }
        public Time AskTime { get { return _askTime; } }
        public double Vwap { get { return _vwap; } }
        public int TickId { get { return _tickId; } }
        public string FinancialStatusIndicator { get { return _financialStatusIndicator; } }
        public DateTime SettlementDate { get { return _settlementDate; } }
        public int TradeMarketCenter { get { return _tradeMarketCenter;  } }
        public int BidMarketCenter { get { return _bidMarketCenter; } }
        public int AskMarketCenter { get { return _askMarketCenter; } }
        public Time TradeTime { get { return _tradeTime; } }
        public string AvailableRegions { get { return _availableRegions; } }

        #region private
        private bool _notFound;
        private bool _summary;
        private string _symbol;
        private double _last;
        private double _change;
        private double _percentChange;
        private int _totalVolume;
        private int _incrementalVolume;
        private double _high;
        private double _low;
        private double _bid;
        private double _ask;
        private int _bidSize;
        private int _askSize;
        private int _tick;
        private int _bidTick;
        private double _range;
        private UpdateType _updateType;
        private int _openInterest;
        private double _open;
        private double _close;
        private double _spread;
        private double _settle;
        private int _delay;
        private bool _shortRestricted;
        private double _netAssetValue;
        private double _averageMaturity;
        private double _7DayYield;
        private DateTime _lastTradeDate;
        private double _extendedTradingLast;
        private int _regionalVolume;
        private double _netAssetValue2;
        private double _extendedTradingChange;
        private double _extendedTradingDifference;
        private double _priceEarningsRatio;
        private double _percentOffAverageVolume;
        private double _bidChange;
        private double _askChange;
        private double _changeFromOpen;
        private bool _marketOpen;
        private double _volatility;
        private double _marketCapitalization;
        private string _fractionDisplayCode;
        private string _decimalPrecision;
        private string _daysToExpiration;
        private int _previousDayVolume;
        private double _openRange1;
        private double _closeRange1;
        private double _openRange2;
        private double _closeRange2;
        private int _numberOfTradesToday;
        private Time _bidTime;
        private Time _askTime;
        private double _vwap;
        private int _tickId;
        private string _financialStatusIndicator;
        private DateTime _settlementDate;
        private int _tradeMarketCenter;
        private int _bidMarketCenter;
        private int _askMarketCenter;
        private Time _tradeTime;
        private string _availableRegions;
        private CultureInfo _enUS = new CultureInfo("en-US");
        #endregion
    }
    public class Level1FundamentalEventArgs : EventArgs
    {
        private static readonly StreamWriter _logger = new StreamWriter("fundamental.log");
        private static readonly Timer _timer = new Timer(1.0);

        static Level1FundamentalEventArgs()
        {
            _timer.Enabled = true;
            _timer.AutoReset = true;
            _timer.Elapsed += (sender, args) => _logger.Flush();
        }

        public Level1FundamentalEventArgs(string line)
        {
            var fields = line.Split(',');

            var now = DateTime.Now;
            _logger.WriteLine(now + ":" + now.Second + "," + line);

            _symbol = fields[1];
            if (!double.TryParse(fields[3], out _pe)) _pe = 0;
            if (!int.TryParse(fields[4], out _averageVolume)) _averageVolume = 0;
            if (!double.TryParse(fields[5], out _high52Week)) _high52Week = 0;
            if (!double.TryParse(fields[6], out _low52Week)) _low52Week = 0;
            if (!double.TryParse(fields[7], out _calendarYearHigh)) _calendarYearHigh = 0;
            if (!double.TryParse(fields[8], out _calendarYearLow)) _calendarYearLow = 0;
            if (!double.TryParse(fields[9], out _dividendYield)) _dividendYield = 0;
            if (!double.TryParse(fields[10], out _dividendAmount)) _dividendAmount = 0;
            if (!double.TryParse(fields[11], out _dividendRate)) _dividendRate = 0;
            if (!DateTime.TryParseExact(fields[12], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _payDate)) _payDate = DateTime.MinValue;
            if (!DateTime.TryParseExact(fields[13], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _exDividendDate)) _exDividendDate = DateTime.MinValue;
            if (!int.TryParse(fields[17], out _shortInterest)) _shortInterest = 0;
            if (!double.TryParse(fields[19], out _currentYearEarningsPerShare)) _currentYearEarningsPerShare = 0;
            if (!double.TryParse(fields[20], out _nextYearEarningsPerShare)) _nextYearEarningsPerShare = 0;
            if (!double.TryParse(fields[21], out _fiveYearGrowthPercentage)) _fiveYearGrowthPercentage = 0;
            if (!int.TryParse(fields[22], out _fiscalYearEnd)) _fiscalYearEnd = 0;
            _companyName = fields[24];
            _rootOptionSymbol = fields[25];
            if (!double.TryParse(fields[26], out _percentHeldByInstitutions)) _percentHeldByInstitutions = 0;
            if (!double.TryParse(fields[27], out _beta)) _beta = 0;
            _leaps = fields[28];
            if (!double.TryParse(fields[29], out _currentAssets)) _currentAssets = 0;
            if (!double.TryParse(fields[30], out _currentLiabilities)) _currentLiabilities = 0;
            if (!DateTime.TryParseExact(fields[31], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _balanceSheetDate)) _balanceSheetDate = DateTime.MinValue;
            if (!double.TryParse(fields[32], out _longTermDebt)) _longTermDebt = 0;
            if (!double.TryParse(fields[33], out _commonSharesOutstanding)) _commonSharesOutstanding = 0;
            var splits = fields[35].Split(' ');
            if (!double.TryParse(splits[0], out _splitFactor1)) _splitFactor1 = 0;
            if (!DateTime.TryParseExact(splits[1], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _splitDate1)) _splitDate1 = DateTime.MinValue;
            splits = fields[36].Split(' ');
            if (!double.TryParse(splits[0], out _splitFactor2)) _splitFactor2 = 0;
            if (!DateTime.TryParseExact(splits[1], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _splitDate2)) _splitDate2 = DateTime.MinValue;
            _formatCode = fields[39];
            if (!int.TryParse(fields[40], out _precision)) _precision = 0;
            _sic = fields[41];
            if (!double.TryParse(fields[42], out _historicalVolatility)) _historicalVolatility = 0;
            _securityType = fields[43];
            _listedMarket = fields[44];
            if (!DateTime.TryParseExact(fields[45], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _date52WeekHigh)) _date52WeekHigh = DateTime.MinValue;
            if (!DateTime.TryParseExact(fields[46], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _date52WeekLow)) _date52WeekLow = DateTime.MinValue;
            if (!DateTime.TryParseExact(fields[47], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _dateCalendarYearHigh)) _dateCalendarYearHigh = DateTime.MinValue;
            if (!DateTime.TryParseExact(fields[48], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _dateCalendarYearLow)) _dateCalendarYearLow = DateTime.MinValue;
            if (!double.TryParse(fields[49], out _yearEndClose)) _yearEndClose = 0;
            if (!DateTime.TryParseExact(fields[50], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _maturityDate)) _maturityDate = DateTime.MinValue;
            if (!double.TryParse(fields[51], out _couponRate)) _couponRate = 0;
            if (!DateTime.TryParseExact(fields[52], "MM/dd/yyyy", _enUS, DateTimeStyles.None, out _expirationDate)) _expirationDate = DateTime.MinValue;
            if (!double.TryParse(fields[53], out _strikePrice)) _strikePrice = 0;
            _naics = fields[54];
            _exchangeRoot = fields[55];
 
        }

        public string Symbol { get { return _symbol; } }
        public double Pe { get { return _pe; } }
        public int AverageVolume { get { return _averageVolume; } }
        public double High52Week { get { return _high52Week; } }
        public double Low52Week { get { return _low52Week; } }
        public double CalendarYearHigh { get { return _calendarYearHigh; } }
        public double CalendarYearLow { get { return _calendarYearLow; } }
        public double DividendYield { get { return _dividendYield; } }
        public double DividendAmount { get { return _dividendAmount; } }
        public double DividendRate { get { return _dividendRate; } }
        public DateTime PayDate { get { return _payDate; } }
        public DateTime ExDividendDate { get { return _exDividendDate; } }
        public int ShortInterest { get { return _shortInterest; } }
        public double CurrentYearEarningsPerShare { get { return _currentYearEarningsPerShare; } } 
        public double FiveYearGrowthPercentage  { get { return _fiveYearGrowthPercentage; } }
        public double NextYearEarningsPerShare { get { return _nextYearEarningsPerShare; } }
        public double FiscalYearEnd { get { return _fiscalYearEnd; } }
        public string CompanyName { get { return _companyName; } }
        public string RootOptionSymbol { get { return _rootOptionSymbol; } }
        public double PercentHeldByInstitutions { get { return _percentHeldByInstitutions; } }
        public double Beta { get { return _beta; } }
        public string Leaps { get { return _leaps; } }
        public double CurrentAssets { get { return _currentAssets; } }
        public double CurrentLiabilities { get { return _currentLiabilities; } }
        public DateTime BalanceSheetDate { get { return _balanceSheetDate; } }
        public double LongTermDebt { get { return _longTermDebt; } }
        public double CommonSharesOutstanding { get { return _commonSharesOutstanding; } }
        public double SplitFactor1 { get { return _splitFactor1; } }
        public DateTime SplitDate1 { get { return _splitDate1; } }
        public double SplitFactor2 { get { return _splitFactor2; } }
        public DateTime SplitDate2 { get { return _splitDate2; } }
        public string FormatCode { get { return _formatCode; } }
        public int Precision { get { return _precision; } }
        public string Sic { get { return _sic; } }
        public double HistoricalVolatility { get { return _historicalVolatility; } }
        public string SecurityType { get { return _securityType; } }
        public string ListedMarket { get { return _listedMarket; } }
        public DateTime Date52WeekHigh { get { return _date52WeekHigh; } }
        public DateTime Date52WeekLow { get { return _date52WeekLow; } }
        public DateTime DateCalendarYearHigh { get { return _dateCalendarYearHigh; } }
        public DateTime DateCalendarYearLow { get { return _dateCalendarYearLow; } }
        public double YearEndClose { get { return _yearEndClose; } }
        public DateTime MaturityDate { get { return _maturityDate; } }
        public double CouponRate { get { return _couponRate; } }
        public DateTime ExpirationDate { get { return _expirationDate; } }
        public double StrikePrice { get { return _strikePrice; } }
        public string Naics { get { return _naics; } }
        public string ExchangeRoot { get { return _exchangeRoot; } } 

        #region private
        private string _symbol;
        private double _pe;
        private int _averageVolume;
        private double _high52Week;
        private double _low52Week;
        private double _calendarYearHigh;
        private double _calendarYearLow;
        private double _dividendYield;
        private double _dividendAmount;
        private double _dividendRate;
        private DateTime _payDate;
        private DateTime _exDividendDate;
        private int _shortInterest;
        private double _currentYearEarningsPerShare;
        private double _nextYearEarningsPerShare;
        private double _fiveYearGrowthPercentage;
        private int _fiscalYearEnd;
        private string _companyName;
        private string _rootOptionSymbol;
        private double _percentHeldByInstitutions;
        private double _beta;
        private string _leaps;
        private double _currentAssets;
        private double _currentLiabilities;
        private DateTime _balanceSheetDate;
        private double _longTermDebt;
        private double _commonSharesOutstanding;
        private double _splitFactor1;
        private DateTime _splitDate1;
        private double _splitFactor2;
        private DateTime _splitDate2;
        private string _formatCode;
        private int _precision;
        private string _sic;
        private double _historicalVolatility;
        private string _securityType;
        private string _listedMarket;
        private DateTime _date52WeekHigh;
        private DateTime _date52WeekLow;
        private DateTime _dateCalendarYearHigh;
        private DateTime _dateCalendarYearLow;
        private double _yearEndClose;
        private DateTime _maturityDate;
        private double _couponRate;
        private DateTime _expirationDate;
        private double _strikePrice;
        private string _naics;
        private string _exchangeRoot;
        private CultureInfo _enUS = new CultureInfo("en-US");
        #endregion
    }
    public class Level1TimerEventArgs : System.EventArgs
    {
        public Level1TimerEventArgs(string line)
        {
            var fields = line.Split(',');
            if (!DateTime.TryParseExact(fields[1], "yyyyMMdd HH:mm:ss", _enUS, DateTimeStyles.None, out _dateTimeStamp)) _dateTimeStamp = DateTime.MinValue;

        }
        public DateTime DateTimeStamp { get { return _dateTimeStamp; } }
        #region private
        private DateTime _dateTimeStamp;
        private CultureInfo _enUS = new CultureInfo("en-US");
        #endregion
    }

    public class Level1NewsEventArgs : System.EventArgs
    {
        public Level1NewsEventArgs(string line)
        {
            var fields = line.Split(',');
            _distributorType = fields[1];
            _storyId = fields[2];
            _symbol = fields[3].Split(':');
            if (!DateTime.TryParseExact(fields[4], "yyyyMMdd HHmmss", _enUS, DateTimeStyles.None, out _newsDateTime)) _newsDateTime = DateTime.MinValue;
            _headline = fields[5];
        }
        public string DistributorType { get { return _distributorType; } }
        public string StoryId { get { return _storyId; } }
        public string[] Symbol { get { return _symbol; } }
        public DateTime NewsDateTime { get { return _newsDateTime; } }
        public string Headline { get { return _headline; } }
        #region private
        private string _distributorType;
        private string _storyId;
        private string[] _symbol;
        private DateTime _newsDateTime;
        private string _headline;
        private CultureInfo _enUS = new CultureInfo("en-US");
        #endregion
    }


    public class Level1RegionalEventArgs : System.EventArgs
    {
        public Level1RegionalEventArgs(string line)
        {
            var fields = line.Split(',');
            _symbol = fields[1];
            if (!double.TryParse(fields[3], out _regionalBid)) _regionalBid = 0;
            if (!int.TryParse(fields[4], out _regionalBidSize)) _regionalBidSize = 0;
            _regionalBidTime = new Time(fields[5]);
            if (!double.TryParse(fields[6], out _regionalAsk)) _regionalAsk = 0;
            if (!int.TryParse(fields[7], out _regionalAskSize)) _regionalAskSize = 0;
            _regionalAskTime = new Time(fields[8]);
            _fractionDisplayCode = fields[9];
            _decimalPrecision = fields[10];
            _marketCenter = fields[11];
        }
        public string Symbol { get { return _symbol; } }
        public double RegionalBid { get { return _regionalBid; } }
        public int RegionalBidSize { get { return _regionalBidSize; } }
        public Time RegionalBidTime { get { return _regionalBidTime; } }
        public double RegionalAsk { get { return _regionalAsk; } }
        public int RegionalAskSize { get { return _regionalAskSize; } }
        public Time RegioalAskTime { get { return _regionalAskTime; } }
        public string FractionDisplayCode { get { return _fractionDisplayCode; } }
        public string DecimalPrecision { get { return _decimalPrecision; } }
        public string MarketCenter { get { return _marketCenter; } } 

        #region private
        private string _symbol;
        private double _regionalBid;
        private int _regionalBidSize;
        private Time _regionalBidTime;
        private double _regionalAsk;
        private int _regionalAskSize;
        private Time _regionalAskTime;
        private string _fractionDisplayCode;
        private string _decimalPrecision;
        private string _marketCenter;
        #endregion
    }

    public class IQLevel1WatchItem
    {
        public IQLevel1WatchItem(string symbol, bool active, bool regionOn = false)
        {
            _symbol = symbol;
            _active = active;
            _regionOn = regionOn;
        }
        public string Symbol { get { return _symbol; } }
        public bool isActive { get { return _active; } }
        public bool isRegionalOn { get { return _regionOn; } } 

        public void Activate()
        {
            if (_socket == null) { throw new Exception("Watch Item not connected"); }
            if (_active) { return; }
            _socket.Send("w" + _symbol + "\r\n");
            _active = true;
        }
        public void DeActivate()
        {
            if (_socket == null) { throw new Exception("Watch Item not connected"); }
            if (!_active) { return; }
            _socket.Send("r" + _symbol + "\r\n");
            _active = false;
            _regionOn = false;
        }
        public void SetRegionalOn()
        {
            if (_socket == null) { throw new Exception("Watch Item not connected"); }
            if (!_active) { throw new Exception("Cannot set Regional Quotes On, Activate first"); }
            if (_regionOn) { return; }
            _socket.Send("S,REGON," + _symbol + "\r\n");
            _regionOn = true;
        }
        public void SetRegionalOff()
        {
            if (_socket == null) { throw new Exception("Watch Item not connected"); }
            if (!_regionOn) { return; }
            _socket.Send("S,REGOFF," + _symbol + "\r\n");
            _regionOn = false;
        }
        public void RequestFundamental()
        {
            if (_socket == null) { throw new Exception("Watch Item not connected"); }
            if (!_active) { throw new Exception("Cannot request Fundamental data, Activate first"); }
            _socket.Send("f" + _symbol + "\r\n");
        }

        internal void Connect(SocketClient socket)
        {
            _socket = socket;
            var tempActive = _active;
            _active = false;
            var tempRegionOn = _regionOn;
            _regionOn = false;
            if (tempActive)
            {
                Activate();
            }
            if (tempRegionOn)
            {
                SetRegionalOn();
            }
        }
        internal void Disconnect()
        {
            DeActivate();
            _socket = null;
        }

        #region private 
        private string _symbol;
        private bool _active;
        private bool _regionOn;
        private SocketClient _socket;
        #endregion
    }

    public class IQLevel1Client : SocketClient 
    {
        // Delegates for event
        public event EventHandler<Level1SummaryUpdateEventArgs> Level1SummaryUpdateEvent;
        public event EventHandler<Level1FundamentalEventArgs> Level1FundamentalEvent;
        public event EventHandler<Level1TimerEventArgs> Level1TimerEvent;
        public event EventHandler<Level1RegionalEventArgs> Level1RegionalEvent;
        public event EventHandler<Level1NewsEventArgs> Level1NewsEvent;
        public event EventHandler<Level1ServerDisconnectedArgs> Level1ServerDisconnectedEvent;
        public event EventHandler<Level1ServerReconnectFailedArgs> Level1ServerReconnectFailed;
        public event EventHandler<Level1TextLineEventArgs> Level1UnknownEvent;

        public IQLevel1Client(int bufferSize)
            : base(IQSocket.GetEndPoint(PortType.Level1), bufferSize)
        {
            _key = new Dictionary<string,IQLevel1WatchItem>();
            IsNewsOn = false;
        }

        public void Connect()
        {
            ConnectToSocketAndBeginReceive(IQSocket.GetSocket());
            Send("S,CONNECT\r\n");
        }
        public void Disconnect(int flushSeconds = 2)
        {
            Send("S,DISCONNECT\r\n");
            DisconnectFromSocket(flushSeconds);
        }
        public void SetClientName(string name)
        {
            Send("S,SET CLIENT NAME," + name + "\r\n");
        }
        public void ForceTimeStamp()
        {
            Send("T\r\n");
        }
        public void SetNewsOn()
        {
            Send("S,NEWSON\r\n");
            IsNewsOn = true;
        }
        public void SetNewsOff()
        {
            Send("S,NEWSOFF\r\n");
            IsNewsOn = false;
        }
        public bool IsNewsOn { get; private set; }

        /// <summary>
        /// Add this symbol to our subscription list.
        /// </summary>
        public void Subscribe(string symbol, bool requestFundamental = true, bool active = true, bool regionOn = false)
        {
            var item = new IQLevel1WatchItem(symbol, active, regionOn);
            if (_key.ContainsKey(item.Symbol))
            {
                return;
            }
            _key.Add(item.Symbol, item);
            item.Connect(this);
            if (requestFundamental)
            {
                item.RequestFundamental();
            }
        }

        /// <summary>
        /// Remove this symbol from our subscriptions.
        /// </summary>
        public void Unsubscribe(string symbol)
        {
            if (!_key.ContainsKey(symbol))
            {
                return;
            }
            _key[symbol].DeActivate();
            _key.Remove(symbol);
        }

        /// <summary>
        /// Check if the storage contains this symbol
        /// </summary>
        public bool Contains(string symbol)
        {
            return _key.Keys.Contains(symbol);
        }

        /// <summary>
        /// Unsubscribe from all symbols and clear the internal storage.
        /// </summary>
        public void Clear()
        {
            Send("S,UNWATCH ALL\r\n");
            foreach (var wi in _key.Values)
            {
                wi.Disconnect();
            }
            _key.Clear();
        }

        /// <summary>
        /// Number of subscribed items
        /// </summary>
        public int Count
        {
            get { return _key.Count; }
        }

        protected override void OnTextLineEvent(TextLineEventArgs e)
        {
            if (e.textLine.StartsWith("Q,") || e.textLine.StartsWith("P,"))
            {
                OnLevel1SummaryUpdateEvent(new Level1SummaryUpdateEventArgs(e.textLine));
                return;
            }
            if (e.textLine.StartsWith("F,"))
            {
                OnLevel1FundamentalEvent(new Level1FundamentalEventArgs(e.textLine));
                return;
            }
            if (e.textLine.StartsWith("R,"))
            {
                OnLevel1RegionalEvent(new Level1RegionalEventArgs(e.textLine));
                return;
            }
            if (e.textLine.StartsWith("T,"))
            {
                OnLevel1TimerEvent(new Level1TimerEventArgs(e.textLine));
                return;
            }
            if (e.textLine.StartsWith("N,"))
            {
                OnLevel1NewsEvent(new Level1NewsEventArgs(e.textLine));
                return;
            }

            if (e.textLine.StartsWith("S,KEY,"))
            {
                // Todo: Process
                return;
            }

            if (e.textLine.StartsWith("S,SERVER CONNECTED"))
            {
                // Todo: Process
                return;
            }

            if (e.textLine.StartsWith("S,SERVER DISCONNECTED"))
            {
                OnLevel1ServerDisconnected(new Level1ServerDisconnectedArgs(e.textLine));
                return;
            }

            if (e.textLine.StartsWith("S,SERVER RECONNECT FAILED"))
            {
                OnLevel1ServerReconnectFailed(new Level1ServerReconnectFailedArgs(e.textLine));
                return;
            }

            if (e.textLine.StartsWith("S,IP,"))
            {
                // Todo: Process
                return;
            }

            if (e.textLine.StartsWith("S,CUST,"))
            {
                // Todo: Process
                return;
            }

            OnLevel1UnknownEvent(new Level1TextLineEventArgs(e.textLine));    
        }

        protected virtual void OnLevel1UnknownEvent(Level1TextLineEventArgs e)
        {
            if (Level1UnknownEvent != null) Level1UnknownEvent(this, e); 
        }

        protected virtual void OnLevel1ServerReconnectFailed(Level1ServerReconnectFailedArgs e)
        {
            if (Level1ServerReconnectFailed != null) Level1ServerReconnectFailed(this, e); 
        }

        protected virtual void OnLevel1ServerDisconnected(Level1ServerDisconnectedArgs e)
        {
            if (Level1ServerDisconnectedEvent != null) Level1ServerDisconnectedEvent(this, e); 
        }

        protected virtual void OnLevel1SummaryUpdateEvent(Level1SummaryUpdateEventArgs e)
        {
            if (Level1SummaryUpdateEvent != null) Level1SummaryUpdateEvent(this, e);
        }
        protected virtual void OnLevel1FundamentalEvent(Level1FundamentalEventArgs e)
        {
            if (Level1FundamentalEvent != null) Level1FundamentalEvent(this, e);
        }
        protected virtual void OnLevel1TimerEvent(Level1TimerEventArgs e)
        {
            if (Level1TimerEvent != null) Level1TimerEvent(this, e);
        }
        protected virtual void OnLevel1RegionalEvent(Level1RegionalEventArgs e)
        {
            if (Level1RegionalEvent != null) Level1RegionalEvent(this, e);
        }
        protected virtual void OnLevel1NewsEvent(Level1NewsEventArgs e)
        {
            if (Level1NewsEvent != null) Level1NewsEvent(this, e);
        }

        #region private
        private Dictionary<string,IQLevel1WatchItem> _key;

        #endregion
     }
}
