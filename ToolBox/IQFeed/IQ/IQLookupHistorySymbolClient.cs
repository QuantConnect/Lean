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
using System.Globalization;
using QuantConnect.Logging;
using static QuantConnect.StringExtensions;

namespace QuantConnect.ToolBox.IQFeed
{
    // Historical stock data lookup events
    public class LookupTickEventArgs : LookupEventArgs
    {
        public LookupTickEventArgs(string requestId, string line) :
            base(requestId, LookupType.REQ_HST_TCK, LookupSequence.MessageDetail)
        {
            var fields = line.Split(',');
            if (fields.Length < 11)
            {
                Log.Error("LookupIntervalEventArgs.ctor(): " + line);
                return;
            }
            if (!DateTime.TryParseExact(fields[1], "yyyy-MM-dd HH:mm:ss", _enUS, DateTimeStyles.None, out _dateTimeStamp)) _dateTimeStamp = DateTime.MinValue;
            if (!double.TryParse(fields[2], out _last)) _last = 0;
            if (!int.TryParse(fields[3], out _lastSize)) _lastSize = 0;
            if (!int.TryParse(fields[4], out _totalVolume)) _totalVolume = 0;
            if (!double.TryParse(fields[5], out _bid)) _bid = 0;
            if (!double.TryParse(fields[6], out _ask)) _ask = 0;
            if (!int.TryParse(fields[7], out _tickId)) _tickId = 0;
            if (!char.TryParse(fields[10], out _basis)) _basis = ' ';
        }
        public DateTime DateTimeStamp { get { return _dateTimeStamp; } }
        public double Last { get { return _last; } }
        public int LastSize { get { return _lastSize; } }
        public int TotalVolume { get { return _totalVolume; } }
        public double Bid { get { return _bid; } }
        public double Ask { get { return _ask; } }
        public int TickId { get { return _tickId; } }
        public char Basis { get { return _basis; } }

        #region private
        private DateTime _dateTimeStamp;
        private double _last;
        private int _lastSize;
        private int _totalVolume;
        private double _bid;
        private double _ask;
        private int _tickId;
        private char _basis;
        private CultureInfo _enUS = new CultureInfo("en-US");
        #endregion
    }

    public class LookupIntervalEventArgs : LookupEventArgs
    {
        public LookupIntervalEventArgs(string requestId, string line) :
            base(requestId, LookupType.REQ_HST_INT, LookupSequence.MessageDetail)
        {
            var fields = line.Split(',');
            if (fields.Length < 8)
            {
                Log.Error("LookupIntervalEventArgs.ctor(): " + line);
                return;
            }
            if (!DateTime.TryParseExact(fields[1], "yyyy-MM-dd HH:mm:ss", _enUS, DateTimeStyles.None, out _dateTimeStamp)) _dateTimeStamp = DateTime.MinValue;
            if (!double.TryParse(fields[2], out _high)) _high = 0;
            if (!double.TryParse(fields[3], out _low)) _low = 0;
            if (!double.TryParse(fields[4], out _open)) _open = 0;
            if (!double.TryParse(fields[5], out _close)) _close = 0;
            if (!int.TryParse(fields[6], out _totalVolume)) _totalVolume = 0;
            if (!int.TryParse(fields[7], out _periodVolume)) _periodVolume = 0;
        }
        public DateTime DateTimeStamp { get { return _dateTimeStamp; } }
        public double High { get { return _high; } }
        public double Low { get { return _low; } }
        public double Open { get { return _open; } }
        public double Close { get { return _close; } }
        public int TotalVolume { get { return _totalVolume; } }
        public int PeriodVolume { get { return _periodVolume; } }

        #region private
        private DateTime _dateTimeStamp;
        private double _high;
        private double _low;
        private double _open;
        private double _close;
        private int _totalVolume;
        private int _periodVolume;
        private CultureInfo _enUS = new CultureInfo("en-US");
        #endregion
    }

    public class LookupDayWeekMonthEventArgs : LookupEventArgs
    {
        public LookupDayWeekMonthEventArgs(string requestId, string line) :
            base(requestId, LookupType.REQ_HST_DWM, LookupSequence.MessageDetail)
        {
            var fields = line.Split(',');
            if (fields.Length < 8)
            {
                Log.Error("LookupIntervalEventArgs.ctor(): " + line);
                return;
            }
            if (!DateTime.TryParseExact(fields[1], "yyyy-MM-dd HH:mm:ss", _enUS, DateTimeStyles.None, out _dateTimeStamp)) _dateTimeStamp = DateTime.MinValue;
            if (!double.TryParse(fields[2], out _high)) _high = 0;
            if (!double.TryParse(fields[3], out _low)) _low = 0;
            if (!double.TryParse(fields[4], out _open)) _open = 0;
            if (!double.TryParse(fields[5], out _close)) _close = 0;
            if (!int.TryParse(fields[6], out _periodVolume)) _periodVolume = 0;
            if (!int.TryParse(fields[7], out _openInterest)) _openInterest = 0;
        }
        public DateTime DateTimeStamp { get { return _dateTimeStamp; } }
        public double High { get { return _high; } }
        public double Low { get { return _low; } }
        public double Open { get { return _open; } }
        public double Close { get { return _close; } }
        public int PeriodVolume { get { return _periodVolume; } }
        public int OpenInterest { get { return _openInterest; } }

        #region private
        private DateTime _dateTimeStamp;
        private double _high;
        private double _low;
        private double _open;
        private double _close;
        private int _periodVolume;
        private int _openInterest;
        private CultureInfo _enUS = new CultureInfo("en-US");
        #endregion
    }

    // Symbol search lookup events
    public class LookupSymbolEventArgs : LookupEventArgs
    {
        public LookupSymbolEventArgs(string requestId, string line) :
            base(requestId, LookupType.REQ_SYM_SYM, LookupSequence.MessageDetail)
        {
            var fields = line.Split(',');
            if (fields.Length < 5) throw new Exception("Error in Symbol parameter provided");
            _symbol = fields[1];
            _marketId = fields[2];
            _securityId = fields[3];
            _description = "";
            for (var i = 4; i < fields.Length; i++) _description += fields[i];
        }
        public string Symbol { get { return _symbol; } }
        public string MarketId { get { return _marketId; } }
        public string SecurityId { get { return _securityId; } }
        public string Description { get { return _description; } }

        #region private
        private string _symbol;
        private string _marketId;
        private string _securityId;
        private string _description;
        #endregion
    }

    public class LookupSicSymbolEventArgs : LookupEventArgs
    {
        public LookupSicSymbolEventArgs(string requestId, string line) :
            base(requestId, LookupType.REQ_SYM_SIC, LookupSequence.MessageDetail)
        {
            var fields = line.Split(',');
            if (fields.Length < 6) throw new Exception("Error in SIC parameter provided");

            _sic = fields[1];
            _symbol = fields[2];
            _marketId = fields[3];
            _securityId = fields[4];
            _description = "";
            for (var i = 5; i < fields.Length; i++) _description += fields[i];
        }

        public string Sic { get { return _sic; } }
        public string Symbol { get { return _symbol; } }
        public string MarketId { get { return _marketId; } }
        public string SecurityId { get { return _securityId; } }
        public string Description { get { return _description; } }

        #region private
        private string _sic;
        private string _symbol;
        private string _marketId;
        private string _securityId;
        private string _description;
        #endregion
    }

    public class LookupNaicSymbolEventArgs : LookupEventArgs
    {
        public LookupNaicSymbolEventArgs(string requestId, string line) :
            base(requestId, LookupType.REQ_SYM_NAC, LookupSequence.MessageDetail)
        {
            var fields = line.Split(',');
            if (fields.Length < 6) throw new Exception("Error in NAIC parameter provided");

            _naic = fields[1];
            _symbol = fields[2];
            _marketId = fields[3];
            _securityId = fields[4];
            _description = "";
            for (var i = 5; i < fields.Length; i++) _description += fields[i];
        }
        public string Naic { get { return _naic; } }
        public string Symbol { get { return _symbol; } }
        public string MarketId { get { return _marketId; } }
        public string SecurityId { get { return _securityId; } }
        public string Description { get { return _description; } }

        #region private
        private string _naic;
        private string _symbol;
        private string _marketId;
        private string _securityId;
        private string _description;
        #endregion
    }

    public class IQLookupHistorySymbolClient : SocketClient
    {
        // Delegates for event
        public event EventHandler<LookupEventArgs> LookupEvent;

        // Constructor
        public IQLookupHistorySymbolClient(int bufferSize)
            : base(IQSocket.GetEndPoint(PortType.Lookup), bufferSize)
        {
            _histDataPointsPerSend = 500;
            _timeMarketOpen = new Time(09, 30, 00);
            _timeMarketClose = new Time(16, 00, 00);
            _lastRequestNumber = -1;
            _histMaxDataPoints = 5000;
         }

        // Command Requests
        public void Connect()
        {
            ConnectToSocketAndBeginReceive(IQSocket.GetSocket());
        }
        public void Disconnect(int flushSeconds = 2)
        {
            DisconnectFromSocket(flushSeconds);
        }
        public void SetClientName(string name)
        {
            Send("S,SET CLIENT NAME," + name + "\r\n");
        }

        // Historical Data Requests
        public int RequestTickData(string symbol, int dataPoints, bool oldToNew)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_HST_TCK}{_lastRequestNumber.ToStringInvariant("0000000")}";

            var reqString = $"HTX,{symbol},{dataPoints.ToStringInvariant("0000000")},{(oldToNew ? "1" : "0")},{reqNo},{_histDataPointsPerSend.ToStringInvariant("0000000")}\r\n";
            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_HST_TCK, LookupSequence.MessageStart));
            return _lastRequestNumber;
        }
        public int RequestTickData(string symbol, int days, bool oldToNew, Time timeStartInDay = null, Time timeEndInDay = null)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_HST_TCK}{_lastRequestNumber.ToStringInvariant("0000000")}";
            if (timeStartInDay == null) timeStartInDay = _timeMarketOpen;
            if (timeEndInDay == null) timeEndInDay = _timeMarketClose;

            var reqString = $"HTD,{symbol},{days.ToStringInvariant("0000000")},{_histMaxDataPoints.ToStringInvariant("0000000")},{timeStartInDay.IQFeedFormat},{timeEndInDay.IQFeedFormat},{(oldToNew ? "1" : "0")},{reqNo},{_histDataPointsPerSend.ToStringInvariant("0000000")}\r\n";
            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_HST_TCK, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }
        public int RequestTickData(string symbol, DateTime start, DateTime? end, bool oldToNew, Time timeStartInDay = null, Time timeEndInDay = null)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_HST_TCK}{_lastRequestNumber.ToStringInvariant("0000000")}";
            //if (timeStartInDay == null) timeStartInDay = _timeMarketOpen;
            //if (timeEndInDay == null) timeEndInDay = _timeMarketClose;

            var reqString = Invariant($"HTT,{symbol},{start:yyyyMMdd HHmmss},") +
                Invariant($"{(end.HasValue ? end.Value.ToStringInvariant("yyyyMMdd HHmmss") : "")},{_histMaxDataPoints:0000000)},") +
                Invariant($"{(timeStartInDay == null ? "" : timeStartInDay.IQFeedFormat)},{(timeEndInDay == null ? "" : timeEndInDay.IQFeedFormat)},") +
                Invariant($"{(oldToNew ? "1" : "0")},{reqNo},{_histDataPointsPerSend:0000000)}\r\n");
            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_HST_TCK, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }
        public int RequestIntervalData(string symbol, Interval interval, int dataPoints, bool oldToNew)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_HST_INT}{_lastRequestNumber.ToStringInvariant("0000000")}";

            var reqString = $"HIX,{symbol},{interval.Seconds.ToStringInvariant("0000000")}," +
                $"{dataPoints.ToStringInvariant("0000000")},{(oldToNew ? "1" : "0")}," +
                $"{reqNo},{_histDataPointsPerSend.ToStringInvariant("0000000")}\r\n";
            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_HST_INT, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }
        public int RequestIntervalData(string symbol, Interval interval, int days, bool oldToNew, Time timeStartInDay = null, Time timeEndInDay = null)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_HST_INT}{_lastRequestNumber.ToStringInvariant("0000000")}";
            if (timeStartInDay == null) timeStartInDay = _timeMarketOpen;
            if (timeEndInDay == null) timeEndInDay = _timeMarketClose;

            var reqString = $"HID,{symbol},{interval.Seconds.ToStringInvariant("0000000")}," +
                $"{days.ToStringInvariant("0000000")},{_histMaxDataPoints.ToStringInvariant("0000000")}," +
                $"{timeStartInDay.IQFeedFormat},{timeEndInDay.IQFeedFormat},{(oldToNew ? "1" : "0")}," +
                $"{reqNo},{_histDataPointsPerSend.ToStringInvariant("0000000")}\r\n";

            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_HST_INT, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }
        public int RequestIntervalData(string symbol, Interval interval, DateTime start, DateTime? end, bool oldToNew, Time timeStartInDay = null, Time timeEndInDay = null)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_HST_INT}{_lastRequestNumber.ToStringInvariant("0000000")}";
            //if (timeStartInDay == null) timeStartInDay = _timeMarketOpen;
            //if (timeEndInDay == null) timeEndInDay = _timeMarketClose;

            var reqString = $"HIT,{symbol},{interval.Seconds.ToStringInvariant("0000000")}," +
                $"{start.ToStringInvariant("yyyyMMdd HHmmss")},{(end.HasValue ? end.Value.ToStringInvariant("yyyyMMdd HHmmss") : "")},," +
                $"{(timeStartInDay == null ? "" : timeStartInDay.IQFeedFormat)},{(timeEndInDay == null ? "" : timeEndInDay.IQFeedFormat)}," +
                $"{(oldToNew ? "1" : "0")},{reqNo},{_histDataPointsPerSend.ToStringInvariant("0000000")}\r\n";

            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_HST_INT, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }
        public int RequestDailyData(string symbol, int dataPoints, bool oldToNew)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_HST_DWM}{_lastRequestNumber.ToStringInvariant("0000000")}";

            var reqString = $"HDX,{symbol},{dataPoints.ToStringInvariant("0000000")},{(oldToNew ? "1" : "0")}," +
                $"{reqNo},{_histDataPointsPerSend.ToStringInvariant("0000000")}\r\n";

            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_HST_DWM, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }
        public int RequestDailyData(string symbol, DateTime start, DateTime? end, bool oldToNew)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_HST_DWM}{_lastRequestNumber.ToStringInvariant("0000000")}";

            var reqString = $"HDT,{symbol},{start.ToStringInvariant("yyyyMMdd")},{(end.HasValue ? end.Value.ToStringInvariant("yyyyMMdd") : "")}," +
                $"{_histMaxDataPoints.ToStringInvariant("0000000")},{(oldToNew ? "1" : "0")},{reqNo}," +
                $"{_histDataPointsPerSend.ToStringInvariant("0000000")}\r\n";

            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_HST_DWM, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }
        public int RequestWeeklyData(string symbol, int dataPoints, bool oldToNew)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_HST_DWM}{_lastRequestNumber.ToStringInvariant("0000000")}";

            var reqString = $"HWX,{symbol},{dataPoints.ToStringInvariant("0000000")},{(oldToNew ? "1" : "0")},{reqNo}," +
                $"{_histDataPointsPerSend.ToStringInvariant("0000000")}\r\n";

            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_HST_DWM, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }
        public int RequestMonthlyData(string symbol, int dataPoints, bool oldToNew)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_HST_DWM}{_lastRequestNumber.ToStringInvariant("0000000")}";

            var reqString = $"HMX,{symbol},{dataPoints.ToStringInvariant("0000000")},{(oldToNew ? "1" : "0")},{reqNo}," +
                $"{_histDataPointsPerSend.ToStringInvariant("0000000")}\r\n";

            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_HST_DWM, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }

        // Search Symbols by filter
        public enum SearchField { Symbol, Description }
        public enum FilterType { Market, SecurityType }
        public int RequestSymbols(SearchField searchField, string searchText, FilterType filterType, string[] filterValue)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_SYM_SYM}{_lastRequestNumber.ToStringInvariant("0000000")}";

            var reqString = $"SBF,{((searchField == SearchField.Symbol) ? "s" : "d")},{searchText},{(filterType == FilterType.Market ? "e" : "t")}," +
                $"{string.Join(" ", filterValue)},{reqNo}\r\n";

            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_SYM_SYM, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }
        public int RequestSymbolBySic(string searchText)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_SYM_SIC}{_lastRequestNumber.ToStringInvariant("0000000")}";

            var reqString = $"SBS,{searchText},{reqNo}\r\n";

            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_SYM_SIC, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }
        public int RequestSymbolByNaic(string searchText)
        {
            _lastRequestNumber++;
            var reqNo = $"{LookupType.REQ_SYM_NAC}{_lastRequestNumber.ToStringInvariant("0000000")}";

            var reqString = $"SBN,{searchText},{reqNo}\r\n";

            Send(reqString);
            OnLookupEvent(new LookupEventArgs(reqNo, LookupType.REQ_SYM_NAC, LookupSequence.MessageStart));

            return _lastRequestNumber;
        }


        // Events
        protected override void OnTextLineEvent(TextLineEventArgs e)
        {
            if (e.textLine.StartsWith(LookupType.REQ_HST_TCK.ToString()))
            {
                var reqId = e.textLine.Substring(0, e.textLine.IndexOf(','));
                if (e.textLine.StartsWith(reqId + ",!ENDMSG!"))
                {
                    OnLookupEvent(new LookupEventArgs(reqId, LookupType.REQ_HST_TCK, LookupSequence.MessageEnd));
                    return;
                }

                OnLookupEvent(new LookupTickEventArgs(reqId, e.textLine));
                return;
            }

            if (e.textLine.StartsWith(LookupType.REQ_HST_INT.ToString()))
            {
                var reqId = e.textLine.Substring(0, e.textLine.IndexOf(','));
                if (e.textLine.StartsWith(reqId + ",!ENDMSG!"))
                {
                    OnLookupEvent(new LookupEventArgs(reqId, LookupType.REQ_HST_INT, LookupSequence.MessageEnd));
                    return;
                }

                OnLookupEvent(new LookupIntervalEventArgs(reqId, e.textLine));
                return;
            }

            if (e.textLine.StartsWith(LookupType.REQ_HST_DWM.ToString()))
            {
                var reqId = e.textLine.Substring(0, e.textLine.IndexOf(','));
                if (e.textLine.StartsWith(reqId + ",!ENDMSG!"))
                {
                    OnLookupEvent(new LookupEventArgs(reqId, LookupType.REQ_HST_DWM, LookupSequence.MessageEnd));
                    return;
                }

                OnLookupEvent(new LookupDayWeekMonthEventArgs(reqId, e.textLine));
                return;
            }

            if (e.textLine.StartsWith(LookupType.REQ_SYM_SYM.ToString()))
            {
                var reqId = e.textLine.Substring(0, e.textLine.IndexOf(','));
                if (e.textLine.StartsWith(reqId + ",!ENDMSG!"))
                {
                    OnLookupEvent(new LookupEventArgs(reqId, LookupType.REQ_SYM_SYM, LookupSequence.MessageEnd));
                    return;
                }
                if (e.textLine.StartsWith(reqId + ",E")) { return; }

                OnLookupEvent(new LookupSymbolEventArgs(reqId, e.textLine));
                return;
            }

            if (e.textLine.StartsWith(LookupType.REQ_SYM_NAC.ToString()))
            {
                var reqId = e.textLine.Substring(0, e.textLine.IndexOf(','));
                if (e.textLine.StartsWith(reqId + ",!ENDMSG!"))
                {
                    OnLookupEvent(new LookupEventArgs(reqId, LookupType.REQ_SYM_NAC, LookupSequence.MessageEnd));
                    return;
                }
                if (e.textLine.StartsWith(reqId + ",E")) { return; }


                OnLookupEvent(new LookupNaicSymbolEventArgs(reqId, e.textLine));
                return;
            }

            if (e.textLine.StartsWith(LookupType.REQ_SYM_SIC.ToString()))
            {
                var reqId = e.textLine.Substring(0, e.textLine.IndexOf(','));
                if (e.textLine.StartsWith(reqId + ",!ENDMSG!"))
                {
                    OnLookupEvent(new LookupEventArgs(reqId, LookupType.REQ_SYM_SIC, LookupSequence.MessageEnd));
                    return;
                }
                if (e.textLine.StartsWith(reqId + ",E")) { return; }

                OnLookupEvent(new LookupSicSymbolEventArgs(reqId, e.textLine));
                return;
            }

            throw new Exception("(Lookup) NOT HANDLED:" + e.textLine);
        }
        protected virtual void OnLookupEvent(LookupEventArgs e)
        {
            if (LookupEvent != null) LookupEvent(this, e);
        }

        #region private
        private int _histDataPointsPerSend;
        private int _histMaxDataPoints;
        private Time _timeMarketOpen;
        private Time _timeMarketClose;
        private int _lastRequestNumber;
        #endregion

        #region public
        public int MaxDataPoints
        {
            get { return _histMaxDataPoints; }
            set { _histMaxDataPoints = value; }
        }

        public int DataPointsPerSend
        {
            get { return _histDataPointsPerSend; }
            set { _histDataPointsPerSend = value; }
        }
        #endregion
    }
}
