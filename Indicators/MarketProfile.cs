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
        /// Period of the indicator
        /// </summary>
        private readonly int _period;

        /// <summary>
        /// The range of roundoff to the prices. i.e two decimal places, three decimal places
        /// </summary>
        private readonly decimal _priceRangeRoundoff;

        /// <summary>
        /// Rolling Window to erase old VolumePerPrice values out of the given period
        /// First item is going to contain Data Point's close value
        /// 
        /// Second item is going to contain the Volume, which can be 1 or
        /// the Data Point's volume value
        /// </summary>
        private RollingWindow<Tuple<decimal, decimal>> OldDataPoints { get; }

        /// <summary>
        /// Close values and Volume values in the given period of time.
        /// Close values are the keys and Volume values the values.
        /// The list is sorted in ascending order of the keys
        /// </summary>
        private SortedList<decimal, decimal> VolumePerPrice { get; }

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
        /// A rolling sum of the Volume values for the given period
        /// </summary>
        private IndicatorBase<IndicatorDataPoint> TotalVolume { get; }

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
        public decimal ValueArea { get; private set; }

        /// <summary>
        /// The highest close price level within the value area
        /// </summary>
        public decimal ValueAreaHigh { get; private set; }

        /// <summary>
        /// The lowest close price level within the value area
        /// </summary>
        public decimal ValueAreaLow { get; private set; }

        /// <summary>
        /// POC Index
        /// </summary>
        private int PointOfControl;

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => TotalVolume.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Creates a new MarkProfile indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="roundoff">How many digits you want to round and the precision. 
        /// i.e roundoff=0.01 round to two digits exactly. roundoff=0.05 by default.</param>
        protected MarketProfile(string name, int period, decimal roundoff = 0.05m)
            : base(name)
        {
            // Check roundoff is positive
            if (roundoff <= 0)
            {
                throw new ArgumentException("Roundoff must be extrictly bigger than zero.");
            }

            OldDataPoints = new RollingWindow<Tuple<decimal, decimal>>(period);
            VolumePerPrice = new SortedList<decimal, decimal>();
            TotalVolume = new Sum(name + "_Sum", period);
            _priceRangeRoundoff = 1 / roundoff;
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A a value for this indicator, Point of Control (POC) price</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            // Define Volume and add it to VolumePerPrice and OldDataPoints
            var VolumeQuantity = DefineVolume(input);
            Add(input,VolumeQuantity);

            // Get the index of the close price with maximum volume
            PointOfControl = GetMax();
            // Get the POC price and volume values
            POCPrice = VolumePerPrice.Keys[PointOfControl];
            POCVolume = VolumePerPrice.Values[PointOfControl];

            // Get the highest and lowest close prices
            ProfileHigh = VolumePerPrice.Keys.Max();
            ProfileLow = VolumePerPrice.Keys.Min();

            // Calulate the Value Area
            CalculateValueArea();

            return POCPrice;
        }

        /// <summary>
        /// Define the Volume value that's going to be used
        /// </summary>
        /// <param name="input">Data</param>
        /// <returns>The Volume value it's going to be used</returns>
        protected abstract decimal DefineVolume(TradeBar input);

        /// <summary>
        /// Add the new input value to the Close array and Volume dictionary.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <param name="VolumeQuantity">Volume quantity of the data point, it dependes of DefineVolume method.</param>
        private void Add(TradeBar input, decimal VolumeQuantity) 
        {
            // Check if the RollingWindow DataPoint has been filled to its capacity
            var isFilled = OldDataPoints.IsReady;

            OldDataPoints.Add(new Tuple<decimal, decimal>(input.Close, VolumeQuantity));

            var ClosePrice = Round(input.Close);
            if (!VolumePerPrice.Keys.Contains(ClosePrice))
            {
                VolumePerPrice.Add(ClosePrice,VolumeQuantity);
            }
            else
            {
                VolumePerPrice[ClosePrice] += VolumeQuantity;
            }

            TotalVolume.Update(input.Time, VolumeQuantity);

            // If isFilled is true it means that the capacity was full before we added a new data point
            // so by this time the RollingWindow has already removed the first added data point, so we
            // need to remove it from the sortedList VolumePerPrice
            if (isFilled)
            {
                var RemovedDataPoint = OldDataPoints.MostRecentlyRemoved;
                ClosePrice = Round(RemovedDataPoint.Item1);
                VolumePerPrice[ClosePrice] -= RemovedDataPoint.Item2;
                if (VolumePerPrice[ClosePrice] == 0)
                {
                    VolumePerPrice.Remove(ClosePrice);
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
        /// Calculate the Value Area and the highest and lowest prices within it(VAH and VAL).
        /// </summary>
        private void CalculateValueArea()
        {
            // First ValueArea estimation
            ValueArea = TotalVolume.Current.Value * 0.70m;

            var currentVolume = POCVolume;

            var minIndex = PointOfControl;
            var maxIndex = PointOfControl;

            int lastMin, lastMax;
            int nextMinIndex, nextMaxIndex;

            decimal lowVolume, highVolume;
            
            // When this loop ends we will have a more accurate value of ValueArea
            // but mainly the prices that delimite this area, ValueAreaLow and ValueAreaHigh
            // so ValueArea can also be seen as the range between ValueAreaLow and ValueAreaHigh
            while (currentVolume <= ValueArea)
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
                // them is bigger than the initial ValueArea value
            }
            ValueAreaHigh = VolumePerPrice.Keys[maxIndex];
            ValueAreaLow = VolumePerPrice.Keys[minIndex];
        }

        /// <summary>
        /// Round the decimal number
        /// </summary>
        /// <param name="a">The decimal number to round</param>
        /// <returns>The rounded decimal number</returns>
        private decimal Round(decimal a)
        {
            return Math.Ceiling(a * _priceRangeRoundoff) / _priceRangeRoundoff;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            OldDataPoints.Reset();
            VolumePerPrice.Clear();
            TotalVolume.Reset();
            base.Reset();
        }
    }
}
