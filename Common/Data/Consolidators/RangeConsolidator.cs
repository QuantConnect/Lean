using Python.Runtime;
using QLNet;
using QuantConnect.Data.Market;
using System;

namespace QuantConnect.Data.Consolidators
{
    public class RangeConsolidator : BaseTimelessConsolidator
    {
        private RangeBar _currentBar;
        private decimal _rangeSize;
        private bool _firstTick;

        /// <summary>
        /// Bar being created
        /// </summary>
        protected override BaseData CurrentBar
        {
            get
            {
                return _currentBar;
            }
            set
            {
                _currentBar = (RangeBar)value;
            }
        }

        /// <summary>
        /// Gets <see cref="RangeBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public override Type OutputType => typeof(RangeBar);

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override IBaseData WorkingData => _currentBar?.Clone();

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public new event EventHandler<RangeBar> DataConsolidated;

        /// <summary>
        ///Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="rangeSize">The constant value size of each bar</param>
        public RangeConsolidator(decimal rangeSize)
            : this(rangeSize, x => x.Value, x => 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="rangeSize">The size of each bar in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RangeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        public RangeConsolidator(
            decimal rangeSize,
            Func<IBaseData, decimal> selector,
            Func<IBaseData, decimal> volumeSelector = null)
            : base(selector ?? (x => x.Value), volumeSelector ?? (x => 0))
        {
            _rangeSize = rangeSize;
            _firstTick = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="rangeSize">The size of each bar in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RangeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        public RangeConsolidator(decimal rangeSize,
            PyObject selector,
            PyObject volumeSelector = null)
            : base(selector, volumeSelector)
        {
            _rangeSize = rangeSize;
            _firstTick = true;
        }

        /// <summary>
        /// Update the current bar being created with the given data
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        protected override void UpdateBar(DateTime time, decimal currentValue, decimal volume)
        {
            _currentBar.Update(time, currentValue, volume);
        }

        /// <summary>
        /// Checks if the current bar being created has closed. If that is the case, it consolidates
        /// the bar, resets the current bar and stores the needed information from the last bar to
        /// create the next bar
        /// </summary>
        protected override void CheckIfBarIsClosed()
        {
            if (_currentBar.IsClosed)
            {
                OnDataConsolidated(_currentBar);
                _currentBar = null;
            }
        }

        /// <summary>
        /// Creates a new bar with the given data
        /// </summary>
        /// <param name="data">The new data for the bar</param>
        protected override void CreateNewBar(IBaseData data)
        {
            var currentValue = Selector(data);
            var volume = VolumeSelector(data);

            var open = currentValue;
            if (_firstTick)
            {
                open = Math.Ceiling(open / _rangeSize) * _rangeSize;
                _firstTick = false;
            }

            _currentBar = new RangeBar(data.Symbol, data.Time, _rangeSize, open, volume);
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        private void OnDataConsolidated(RangeBar consolidated)
        {
            DataConsolidated?.Invoke(this, consolidated);

            DataConsolidatedHandler?.Invoke(this, consolidated);

            Consolidated = consolidated;
        }
    }
}
