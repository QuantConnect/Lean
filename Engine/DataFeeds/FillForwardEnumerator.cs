using System;
using System.Collections;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    public class FillForwardEnumerator : IEnumerator<BaseData>
    {
        private bool _isFillingForward;
        private DailyState _dailyState;
        private readonly DailyState _defaultState;

        private readonly DateTime _endTime;
        private readonly TimeSpan _resolution;
        private readonly SecurityExchange _exchange;
        private readonly bool _isExtendedMarketHours;
        private readonly bool _isDailyData;
        private readonly IEnumerator<BaseData> _enumerator;

        public FillForwardEnumerator(IEnumerator<BaseData> enumerator, SecurityExchange exchange, TimeSpan resolution, bool isExtendedMarketHours, DateTime endTime, TimeSpan dataResolution)
        {
            _enumerator = enumerator;
            _exchange = exchange;
            _resolution = resolution;
            _isExtendedMarketHours = isExtendedMarketHours;
            _endTime = endTime;
            _isDailyData = dataResolution == Time.OneDay;
            if (dataResolution > Time.OneDay)
            {
                throw new ArgumentOutOfRangeException("dataResolution", "Currently this implementation maxes out at daily resolution");
            }
            _defaultState = _resolution == Time.OneDay ? DailyState.AfterMarketCheckMissingDay : DailyState.DailyEmit;
            _dailyState = _defaultState;
        }

        public BaseData Current
        {
            get;
            private set;
        }

        public bool MoveNext()
        {
            var previous = Current;
            bool endOfData;
            BaseData fillForward;
            if (!_isFillingForward)
            {
                if (!_enumerator.MoveNext())
                {
                    // check to see if we ran out of data before the end of the subscription
                    if (previous != null && previous.EndTime < _endTime)
                    {
                        var endOfSubscription = previous.Clone(true);
                        endOfSubscription.Time = _endTime;
                        if (RequiresFillForwardData(previous, endOfSubscription, out fillForward, out endOfData))
                        {
                            // don't mark as filling forward so we come back into this block, subscription is done
                            //_isFillingForward = true;
                            Current = fillForward;
                            return true;
                        }
                    
                        Current = endOfSubscription;
                        return true;
                    }
                    return false;
                }
            }

            if (previous == null)
            {
                Current = _enumerator.Current;
                return true;
            }

            if (RequiresFillForwardData(previous, _enumerator.Current, out fillForward, out endOfData))
            {
                _isFillingForward = true;
                Current = fillForward;
                return true;
            }

            if (endOfData)
            {
                _isFillingForward = false;
                return false;
            }

            _isFillingForward = false;
            Current = _enumerator.Current;
            return true;
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        private bool RequiresFillForwardData(BaseData previous, BaseData next, out BaseData fillForward, out bool endOfData)
        {
            if (next.Time < previous.Time)
            {
                throw new ArgumentException();
            }

            if (_isDailyData)
            {
                // daily is kind of special
                return DailyRequiresFillForwardData(previous, next, out fillForward, out endOfData);
            }

            // check to see if the gap between previous and next warrants fill forward behavior
            if (next.Time - previous.Time <= _resolution)
            {
                fillForward = null;
                endOfData = false;
                return false;
            }

            // check to see if the exchange is open at the next resolution
            var barStartTime = previous.Time + _resolution;
            var barEndTime = previous.EndTime + _resolution;
            
            // excluding daily, if we're expectd to emit next 'resolution' then do it
            if (!_isDailyData && _exchange.IsOpenDuringBar(barStartTime, barEndTime, _isExtendedMarketHours))
            {
                fillForward = previous.Clone(true);
                fillForward.Time = barStartTime;
                endOfData = false;
                return true;
            }
            if (_isDailyData)
            {
                if (IsOpen(previous.Time + _resolution) || IsOpen(previous.Time + _resolution.Subtract(TimeSpan.FromTicks(1))))
                {
                    fillForward = previous.Clone(true);
                    fillForward.Time = barStartTime;
                    endOfData = false;
                    return true;
                }
            }
            
            var exchangeOpenTimeOfDay = (_isExtendedMarketHours ? _exchange.ExtendedMarketOpen : _exchange.MarketOpen);

            // advance our time until the next date that is open, this is to skip over weekends/holidays and such
            var nextOpenDate = (previous.Time + _resolution).Date;
            while (nextOpenDate < previous.Time || !_exchange.DateIsOpen(nextOpenDate))
            {
                nextOpenDate = nextOpenDate.AddDays(1);
            }

            // if next.Time is at exchange opening of the next tradeable date, then we don't need fill forward
            var exchangeOpen = nextOpenDate + exchangeOpenTimeOfDay;
            if (next.Time == exchangeOpen)
            {
                fillForward = null;
                endOfData = false;
                return false;
            }

            // if we've made it here it's because we have a day gap, so fill forward at the exchange open time
            var fillForwardBarStartTime = exchangeOpen.RoundUp(_resolution);

            endOfData = false;
            if (next.Time <= fillForwardBarStartTime)
            {
                fillForward = null;
                return false;
            }

            var nextIsBehindCurrent = exchangeOpen > next.EndTime;
            if (nextIsBehindCurrent)
            {
                // advance the enumerator until we're ahead of current again
                while (_enumerator.Current.Time < fillForwardBarStartTime)
                {
                    if (!_enumerator.MoveNext())
                    {
                        endOfData = true;
                        break;
                    }
                }
            }

            if (_enumerator.Current != null && _enumerator.Current.Time <= fillForwardBarStartTime)
            {
                fillForward = null;
                return false;
            }

            fillForward = (nextIsBehindCurrent ? next : previous).Clone(true);
            fillForward.Time = fillForwardBarStartTime;

            return true;
        }

        private bool DailyRequiresFillForwardData(BaseData previous, BaseData next, out BaseData fillForward, out bool endOfData)
        {
            // check to see if the gap between previous and next warrants fill forward behavior
            if (next.Time - previous.Time <= _resolution)
            {
                fillForward = null;
                endOfData = false;
                return false;
            }

            switch (_dailyState)
            {
                // we just emitted a daily bar, either FF or normal, either way we need to now fill in during market hours
                case DailyState.DailyEmit:

                    // handle first bar of day, 9:30-> 9:31
                    fillForward = previous.Clone(true);
                    fillForward.Time = GetMarketOpen(previous.Time.Date).RoundUp(_resolution);
                    endOfData = false;
                    _dailyState = _resolution == Time.OneDay ? DailyState.DailyEmit : DailyState.MarketOpen;
                    return true;

                case DailyState.MarketOpen:
                case DailyState.ResolutionStep:

                    // we just issued a market open bar, so we need to take a single resolution step until the Time+_resolution is outside market hours
                    if (_exchange.IsOpenDuringBar(previous.Time, previous.Time + _resolution, _isExtendedMarketHours))
                    {
                        fillForward = previous.Clone(true);
                        fillForward.Time = previous.Time + _resolution;
                        endOfData = false;
                        _dailyState = DailyState.ResolutionStep;
                        return true;
                    }

                    // after here the market is closed at the next 'resolution' step, so we're going to emit our 12:00 bar
                    _dailyState = DailyState.DailyEmit;
                    
                    // if we weren't open at the next step, check for the next day
                    //case DailyState.AfterMarketCheckMissingDay:
                    var nextOpenDate1 = GetNextOpenDateAfter(previous.Time.Date);
                    if (next.Time.Date != nextOpenDate1)
                    {
                        // the next piece of data skips a day, so FF the missing day
                        fillForward = previous.Clone(true);
                        fillForward.Time = nextOpenDate1;
                        endOfData = false;
                        return true;
                    }

                    // the next open date is the next piece of data, just emit that, no need for FF
                    fillForward = null;
                    endOfData = false;
                    return false;

                case DailyState.AfterMarketCheckMissingDay:

                    // after here the market is closed at the next 'resolution' step, so we're going to emit our 12:00 bar
                    _dailyState = _defaultState;

                    var nextOpenDate2 = GetNextOpenDateAfter(previous.Time.Date);
                    if (next.Time.Date != nextOpenDate2)
                    {
                        // the next piece of data skips a day, so FF the missing day
                        fillForward = previous.Clone(true);
                        fillForward.Time = nextOpenDate2;
                        endOfData = false;
                        return true;
                    }

                    // the next open date is the next piece of data, just emit that, no need for FF
                    fillForward = null;
                    endOfData = false;
                    return false;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        private DateTime GetNextOpenDateAfter(DateTime date)
        {
            do
            {
                date = date + Time.OneDay;
            }
            while (!_exchange.DateIsOpen(date));
            return date;
        }

        private bool IsOpen(DateTime time)
        {
            if (_isExtendedMarketHours)
            {
                return _exchange.DateTimeIsExtendedOpen(time);
            }
            return _exchange.DateTimeIsOpen(time);
        }

        private DateTime GetMarketOpen(DateTime date)
        {
            return date + (_isExtendedMarketHours ? _exchange.ExtendedMarketOpen : _exchange.MarketOpen);
        }

        private DateTime GetMarketClose(DateTime date)
        {
            return date + (_isExtendedMarketHours ? _exchange.ExtendedMarketClose : _exchange.MarketClose);
        }

        enum DailyState
        {
            DailyEmit,
            MarketOpen,
            ResolutionStep,
            AfterMarketCheckMissingDay
        }
    }
}