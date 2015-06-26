using System;
using System.Collections;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    public class SubscriptionFilterEnumerator : IEnumerator<BaseData>
    {
        public event EventHandler<Exception> DataFilterError;

        private readonly Security _security;
        private readonly DateTime _startTime;
        private readonly DateTime _endTime;
        private readonly IEnumerator<BaseData> _enumerator;

        public SubscriptionFilterEnumerator(IEnumerator<BaseData> enumerator, Security security, DateTime endTime)
        {
            _enumerator = enumerator;
            _security = security;
            _endTime = endTime;
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public bool MoveNext()
        {
            while (_enumerator.MoveNext())
            {
                try
                {
                    if (!_security.DataFilter.Filter(_security, _enumerator.Current))
                    {
                        continue;
                    }
                }
                catch (Exception err)
                {
                    OnDataFilterError(err);
                    continue;
                }

                if (!_security.Exchange.IsOpenDuringBar(_enumerator.Current.Time, _enumerator.Current.EndTime, _security.IsExtendedMarketHours))
                {
                    continue;
                }

                // make sure we haven't passed the end
                if (_enumerator.Current.Time > _endTime)
                {
                    return false;
                }
                Current = _enumerator.Current;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        public BaseData Current
        {
            get; private set;
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        protected virtual void OnDataFilterError(Exception e)
        {
            var handler = DataFilterError;
            if (handler != null) handler(this, e);
        }
    }
}