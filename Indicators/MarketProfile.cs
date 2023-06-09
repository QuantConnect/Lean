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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents an Indicator of the Market Profile and its attributes
    /// 
    /// The concept of Market Profile stems from the idea that
    /// markets have a form of organization determined by time,
    /// price, and volume.Each day, the market will develop a range
    /// for the day and a value area, which represents an equilibrium
    /// point where there are an equal number of buyers and sellers.
    /// In this area, prices never stay stagnant. They are constantly
    /// diverging, and Market Profile records this activity for traders
    /// to interpret.
    /// 
    /// It can be computed in two modes: TPO (Time Price Opportunity) or VOL (Volume Profile)
    /// A discussion on the difference between TPO (Time Price Opportunity) 
    /// and VOL (Volume Profile) chart types: https://jimdaltontrading.com/tpo-vs-volume-profile
    /// </summary>
    public abstract class MarketProfile : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Percentage of total volume contained in the ValueArea
        /// </summary>
        private readonly decimal _valueAreaVolumePercentage;

        /// <summary>
        /// The range of roundoff to the prices. i.e two decimal places, three decimal places
        /// </summary>
        private readonly decimal _priceRangeRoundOff;

        /// <summary>
        /// Rolling Window to erase old VolumePerPrice values out of the given period
        /// First item is going to contain Data Point's close value
        /// 
        /// Second item is going to contain the Volume, which can be 1 or
        /// the Data Point's volume value
        /// </summary>
        private RollingWindow<Tuple<decimal, decimal>> _oldDataPoints { get; }

        /// <summary>
        /// Close values and Volume values in the given period of time.
        /// Close values are the keys and Volume values the values.
        /// The list is sorted in ascending order of the keys
        /// </summary>
        private SortedList<decimal, decimal> _volumePerPrice;

        /// <summary>
        /// A rolling sum of the Volume values for the given period
        /// </summary>
        private IndicatorBase<IndicatorDataPoint> _totalVolume { get; }

        /// <summary>
        /// POC Index
        /// </summary>
        private int _pointOfControl;

        /// <summary>
        /// Get a copy of the _volumePerPrice field
        /// </summary>
        public SortedList<decimal, decimal> VolumePerPrice => new SortedList<decimal, decimal>(_volumePerPrice);

        /// <summary>
        /// The highest reached close price level during the period.
        /// That value is called Profile High
        /// </summary>
        public decimal ProfileHigh { get; private set; }

        /// <summary>
        /// The lowest reached close price level during the period.
        /// That value is called Profile Low
        /// </summary>
        public decimal ProfileLow { get; private set; }

        /// <summary>
        /// Price where the most trading occured (Point of Control(POC))
        /// This price is MarketProfile.Current.Value
        /// </summary>
        public decimal POCPrice { get; private set; }

        /// <summary>
        /// Volume where the most tradding occured (Point of Control(POC))
        /// </summary>
        public decimal POCVolume { get; private set; }

        /// <summary>
        /// The range of price levels in which a specified percentage of all volume 
        /// was traded during the time period. Typically, this percentage is set 
        /// to 70% however it is up to the trader’s discretion.
        /// </summary>
        public decimal ValueAreaVolume { get; private set; }

        /// <summary>
        /// The highest close price level within the value area
        /// </summary>
        public decimal ValueAreaHigh { get; private set; }

        /// <summary>
        /// The lowest close price level within the value area
        /// </summary>
        public decimal ValueAreaLow { get; private set; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _totalVolume.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; private set; }

        /// <summary>
        /// Creates a new MarkProfile indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="valueAreaVolumePercentage">The percentage of volume contained in the value area</param>
        /// <param name="priceRangeRoundOff">How many digits you want to round and the precision.
        /// i.e 0.01 round to two digits exactly. 0.05 by default.</param>
        protected MarketProfile(string name, int period, decimal valueAreaVolumePercentage = 0.70m, decimal priceRangeRoundOff = 0.05m)
            : base(name)
        {
            // Check roundoff is positive
            if (priceRangeRoundOff <= 0)
            {
                throw new ArgumentException("Must be strictly bigger than zero.", nameof(priceRangeRoundOff));
            }

            WarmUpPeriod = period;
            _valueAreaVolumePercentage = valueAreaVolumePercentage;
            _oldDataPoints = new RollingWindow<Tuple<decimal, decimal>>(period);
            _volumePerPrice = new SortedList<decimal, decimal>();
            _totalVolume = new Sum(name + "_Sum", period);
            _priceRangeRoundOff = 1 / priceRangeRoundOff;
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A a value for this indicator, Point of Control (POC) price</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            // Define Volume and add it to _volumePerPrice and _oldDataPoints
            var VolumeQuantity = GetVolume(input);
            Add(input, VolumeQuantity);

            // Get the index of the close price with maximum volume
            _pointOfControl = GetMax();

            var volumePerPriceCount = VolumePerPrice.Count;

            // Get the POC price and volume values
            POCPrice = volumePerPriceCount != 0 ? VolumePerPrice.Keys[_pointOfControl] : 0;
            POCVolume = volumePerPriceCount != 0 ? VolumePerPrice.Values[_pointOfControl] : 0;

            // Get the highest and lowest close prices
            ProfileHigh = volumePerPriceCount != 0 ? VolumePerPrice.Keys.Max() : 0;
            ProfileLow = volumePerPriceCount != 0 ? VolumePerPrice.Keys.Min() : 0;

            // Calculate the Value Area Volume and Value Area High and Low
            CalculateValueArea();

            return POCPrice;
        }

        /// <summary>
        /// Get the Volume value that's going to be used
        /// </summary>
        /// <param name="input">Data</param>
        /// <returns>The Volume value it's going to be used</returns>
        protected abstract decimal GetVolume(TradeBar input);

        /// <summary>
        /// Add the new input value to the Close array and Volume dictionary.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <param name="VolumeQuantity">Volume quantity of the data point, it dependes of DefineVolume method.</param>
        private void Add(TradeBar input, decimal VolumeQuantity) 
        {
            // Check if the RollingWindow _oldDataPoints has been filled to its capacity
            var isFilled = _oldDataPoints.IsReady;

            _oldDataPoints.Add(new Tuple<decimal, decimal>(input.Close, VolumeQuantity));

            var ClosePrice = Round(input.Close);
            if (!_volumePerPrice.Keys.Contains(ClosePrice))
            {
                _volumePerPrice.Add(ClosePrice,VolumeQuantity);
            }
            else
            {
                _volumePerPrice[ClosePrice] += VolumeQuantity;
            }

            _totalVolume.Update(input.Time, VolumeQuantity);

            // If isFilled is true it means that the capacity was full before we added a new data point
            // so by this time the RollingWindow has already removed the first added data point, so we
            // need to remove it from the sortedList _volumePerPrice
            if (isFilled)
            {
                var RemovedDataPoint = _oldDataPoints.MostRecentlyRemoved;
                ClosePrice = Round(RemovedDataPoint.Item1);
                // Two equal points can be inserted in _oldDataPoints, where the volume of the second one is zero. Then
                // when the first one is removed from _oldDataPoints, its value in _volumePerPrice is also removed as
                // the remaining value is zero.
                if (_volumePerPrice.ContainsKey(ClosePrice))
                {
                    _volumePerPrice[ClosePrice] -= RemovedDataPoint.Item2;
                    if (_volumePerPrice[ClosePrice] == 0)
                    {
                        _volumePerPrice.Remove(ClosePrice);
                    }
                }
            }
        }

        /// <summary>
        /// Finds the close price with biggest volume value.
        /// </summary>
        /// <returns> Index of the close price with biggest volume value</returns>
        private int GetMax()
        {
            var maxIdx = 0;
            for (int index = 0; index < VolumePerPrice.Values.Count; index++)
            {
                if (VolumePerPrice.Values[index] > VolumePerPrice.Values[maxIdx])
                {
                    maxIdx = index;
                }
                else if(VolumePerPrice.Values[index] == VolumePerPrice.Values[maxIdx])
                {
                    // Find the maximum with minimum distance to the center
                    var mid = VolumePerPrice.Count - 1;
                    if(Math.Abs(mid/2 - index)<Math.Abs(mid/2 - maxIdx))
                    {
                        maxIdx = index;
                    }
                }
            }
            return maxIdx;
        }

        /// <summary>
        /// Calculate the Value Area Volume and the highest and lowest prices within it (VAH and VAL).
        /// </summary>
        private void CalculateValueArea()
        {
            // First ValueArea estimation
            ValueAreaVolume = _totalVolume.Current.Value * _valueAreaVolumePercentage;

            var currentVolume = POCVolume;

            var minIndex = _pointOfControl;
            var maxIndex = _pointOfControl;

            int lastMin, lastMax;
            int nextMinIndex, nextMaxIndex;

            decimal lowVolume, highVolume;
            
            // When this loop ends we will have a more accurate value of ValueAreaVolume
            // but mainly the prices that delimite this area, ValueAreaLow and ValueAreaHigh
            // so ValueArea, can also be seen as the range between ValueAreaLow and ValueAreaHigh
            while (currentVolume <= ValueAreaVolume && ValueAreaVolume != 0)
            {
                lastMin = minIndex;
                lastMax = maxIndex;

                nextMinIndex = Math.Max(minIndex - 1, 0);
                nextMaxIndex = Math.Min(maxIndex + 1, VolumePerPrice.Count - 1);

                if (nextMinIndex != lastMin)
                {
                    lowVolume = VolumePerPrice.Values[nextMinIndex];
                }
                else
                {
                    lowVolume = 0;
                }

                if (nextMaxIndex != lastMax)
                {
                    highVolume = VolumePerPrice.Values[nextMaxIndex];
                }
                else
                {
                    highVolume = 0;
                }

                // Take the largest volume value between the above and below prices
                // of the Point of Control (the initial maxIndex and minIndex respectively)

                if ((highVolume == 0) || ((lowVolume != 0) && (lowVolume > highVolume)))
                {
                    currentVolume += lowVolume;
                    minIndex = nextMinIndex;
                }
                else if ((lowVolume == 0) || ((highVolume != 0) && (highVolume >= lowVolume)))
                {
                    currentVolume += highVolume;
                    maxIndex = nextMaxIndex;
                }
                else
                {
                    break;
                }

                // We expand this range between minIndex and maxIndex until the sum of all volume values between
                // them is bigger than the initial ValueAreaVolume value
            }
            ValueAreaHigh = VolumePerPrice.Count != 0 ? VolumePerPrice.Keys[maxIndex] : 0;
            ValueAreaLow = VolumePerPrice.Count != 0 ? VolumePerPrice.Keys[minIndex] : 0;
        }

        /// <summary>
        /// Round the decimal number
        /// </summary>
        /// <param name="a">The decimal number to round</param>
        /// <returns>The rounded decimal number</returns>
        private decimal Round(decimal a)
        {
            return Math.Ceiling(a * _priceRangeRoundOff) / _priceRangeRoundOff;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _oldDataPoints.Reset();
            _volumePerPrice.Clear();
            _totalVolume.Reset();
            base.Reset();
        }
    }
}
