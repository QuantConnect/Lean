using System;
using System.Collections.Generic;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Rolling (sample) covariance between two numeric time series X and Y.
    /// Maintains rolling sums for O(1) updates over a fixed lookback period.
    ///
    /// Cov(X,Y) = (sum(xy) - sum(x) * sum(y) / n) / (n - 1), for n >= 2.
    /// </summary>
    public class Covariance
    {
        private readonly int _period;
        private readonly Queue<decimal> _x = new();
        private readonly Queue<decimal> _y = new();

        // rolling sums
        private decimal _sumX, _sumY, _sumXY;

        /// <summary>Latest covariance value (0 until enough samples).</summary>
        public decimal Value { get; private set; }

        /// <summary>Number of samples required.</summary>
        public int Period => _period;

        /// <summary>True when the rolling window is full.</summary>
        public bool IsReady => _x.Count == _period;

        /// <summary>Last update time.</summary>
        public DateTime Time { get; private set; }

        public Covariance(int period)
        {
            if (period < 2)
                throw new ArgumentOutOfRangeException(nameof(period), "Covariance period must be >= 2.");
            _period = period;
        }

        /// <summary>
        /// Update with a new pair (x_t, y_t) at the provided time.
        /// Returns the latest covariance value.
        /// </summary>
        public decimal Update(DateTime time, decimal x, decimal y)
        {
            Time = time;

            // add new
            _x.Enqueue(x);
            _y.Enqueue(y);
            _sumX  += x;
            _sumY  += y;
            _sumXY += x * y;

            // remove oldest if exceeding period
            if (_x.Count > _period)
            {
                var oldX = _x.Dequeue();
                var oldY = _y.Dequeue();
                _sumX  -= oldX;
                _sumY  -= oldY;
                _sumXY -= oldX * oldY;
            }

            var n = _x.Count; // == _y.Count
            if (n >= 2)
            {
                // sample covariance
                Value = (_sumXY - (_sumX * _sumY) / n) / (n - 1);
            }
            else
            {
                Value = 0m;
            }

            return Value;
        }

        /// <summary>Reset internal state.</summary>
        public void Reset()
        {
            _x.Clear();
            _y.Clear();
            _sumX = _sumY = _sumXY = 0m;
            Value = 0m;
            Time = default;
        }
    }
}
