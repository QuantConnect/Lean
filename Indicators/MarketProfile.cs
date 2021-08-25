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
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents an Indicator of the Market Profile and its attributes
    /// 
    /// Market Profile takes the data from each day’s trading session and 
    /// organizes it to help us understand who is in control of the market 
    /// and what is perceived as fair value.
    /// </summary>
    public class MarketProfile : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private int Period;

        /// <summary>
        /// Market Profile mode: TPO(Time Price Opportunity) or VOL(Volume Profile)
        /// </summary>
        public string Mode;

        /// <summary>
        /// Queue to erase old DataPoints out of the given period
        /// </summary>
        private Queue<TradeBar> DataPoints;

        /// <summary>
        /// Close values in the given period of time
        /// </summary>
        private List<decimal> Close;

        /// <summary>
        /// If the mode is TPO, Volume represent the frequency of the Close values.
        /// Otherwise, Volume represent the sum of the Volume values for the same Close value. 
        /// </summary>
        private Dictionary<decimal, decimal> Volume;

        /// <summary>
        /// The highest reached close price level during the period
        /// </summary>
        public decimal ProfileHigh;

        /// <summary>
        /// The lowest reached close price level during the period
        /// </summary>
        public decimal ProfileLow;

        /// <summary>
        /// A rolling sum of the Volume values for the given period
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> TotalVolume { get; }

        /// <summary>
        /// Price where the most trading occured (Point of Control(POC))
        /// </summary>
        public decimal POCPrice;

        /// <summary>
        /// Volume where the most tradding occured (Point of Control(POC))
        /// </summary>
        public decimal POCVolume;

        /// <summary>
        /// 70% of the day's trading
        /// </summary>
        public decimal ValueArea;

        /// <summary>
        /// The highest close price level within the value area
        /// </summary>
        public decimal VAH;

        /// <summary>
        /// The lowest close price level within the value area
        /// </summary>
        public decimal VAL;
        
        /// <summary>
        /// POC Index
        /// </summary>
        private int POCIndex;

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => TotalVolume.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the MarketProfile class using the specified period
        /// </summary>
        /// <param name="period">The period over which to perform the computation</param>
        public MarketProfile(int period=2)
            :this($"MP({period})", period, "TPO")
        {
        }

        /// <summary>
        /// Initializes a new instance of the MarketProfile class using the specified period and name
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period over which to perform the computation</param>
        public MarketProfile(string name, int period = 2)
            : this(name, period, "TPO")
        {
        }

        /// <summary>
        /// Creates a new MarkProfile indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="mode">The mode of this indicator</param>
        public MarketProfile(string name, int period, string mode)
            :base(name)
        {
            DataPoints = new Queue<TradeBar>();
            Close = new List<decimal>();
            Volume = new Dictionary<decimal, decimal>();
            TotalVolume = new Sum(name + "_Sum", period);
            Period = period;
            Mode = mode;
            POCIndex = 0;
            POCPrice = 0;
            POCVolume = 0;
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A a value for this indicator, Point of Control (POC) price</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            DataPoints.Enqueue(input);

            if (Mode == "TPO")
            {
                TPO(input);
            }
            else if(Mode == "VOL")
            {
                VOL(input);
            }
            else
            {
                throw new NotImplementedException();
            }
            POCIndex = GetMax();
            // Get the POC price and volume values
            POCPrice = Close[POCIndex];
            POCVolume = Volume[POCPrice];

            // Get the highest and lowest close prices
            ProfileHigh = Close.Max();
            ProfileLow = Close.Min();

            // Calulate the Value area
            CalculateValueArea();

            return POCPrice;
        }

        /// <summary>
        /// Add the new input value to the Close array and Volume dictionary in the TPO mode.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        private void TPO(TradeBar input)
        {
            decimal ClosePrice = Round(input.Close);
            if (!Close.Contains(ClosePrice))
            {
                Close.Add(ClosePrice);
                Volume[ClosePrice] = 1;
            }
            else
            {
                Volume[ClosePrice]+=1;
            }


            TotalVolume.Update(input.Time, 1);


            // Check if the indicator is in the given period
            if (DataPoints.Count > Period)
            {
                TradeBar firstItem = DataPoints.Peek();
                ClosePrice = Round(firstItem.Close);
                Volume[ClosePrice]--;
                if (Volume[ClosePrice] == 0)
                {
                    Volume.Remove(ClosePrice);
                    Close.Remove(ClosePrice);
                }
                DataPoints.Dequeue();
            }

        }

        /// <summary>
        /// Add the new input value to the Close array and Volume dictionary in the VOL mode.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        private void VOL(TradeBar input)
        {
            decimal ClosePrice = Round(input.Close);// Roundoff the close price
            if (!Close.Contains(ClosePrice))
            {
                Close.Add(ClosePrice);
                Volume[ClosePrice] = input.Volume;
            }
            else
            {
                Volume[ClosePrice] += input.Volume;
            }
            TotalVolume.Update(input.Time, input.Volume);

            // Check if the indicator is in the given period
            if (DataPoints.Count > Period)
            {
                TradeBar firstItem = DataPoints.Peek();
                ClosePrice = Round(firstItem.Close); // Roundoff the close price
                Volume[ClosePrice]-=firstItem.Volume;
                if (Volume[ClosePrice] == 0)
                {
                    Volume.Remove(ClosePrice);
                    Close.Remove(ClosePrice);
                }
                DataPoints.Dequeue();
            }

        }

        /// <summary>
        /// Finds the close price with biggest value in Volume.
        /// </summary>
        /// <returns> Close price with biggest value in Volume</returns>
        private int GetMax()
        {
            int Max_idx = 0;
            for(int i=0; i < Close.Count; i++)
            {
                if (Volume[Close[i]] > Volume[Close[Max_idx]])
                {
                    Max_idx = i;
                }
            }
            return Max_idx;
        }

        /// <summary>
        /// Calculate the Value Area and the highest and lowest prices within it(VAH and VAL).
        /// </summary>
        private void CalculateValueArea()
        {
            ValueArea = TotalVolume.Current.Value * 0.70m;
            decimal trialVolume = POCVolume;

            int minIndex = POCIndex;
            int maxIndex = POCIndex;

            int lastMin, lastMax;
            int nextMinIndex, nextMaxIndex;

            decimal lowVolume, highVolume;
            while (trialVolume <= ValueArea)
            {
                lastMin = minIndex;
                lastMax = maxIndex;

                nextMinIndex = clip(minIndex - 1, 0, Close.Count() - 1);
                nextMaxIndex = clip(maxIndex + 1, 0, Close.Count() - 1);

                if (nextMinIndex != lastMin)
                {
                    lowVolume = Volume[Close[nextMinIndex]];
                }
                else
                {
                    lowVolume = 0;
                }

                if(nextMaxIndex != lastMax)
                {
                    highVolume = Volume[Close[nextMaxIndex]];
                }
                else
                {
                    highVolume = 0;
                }

                if ((highVolume==0) || ((lowVolume != 0) && (lowVolume > highVolume)))
                {
                    trialVolume += lowVolume;
                    minIndex = nextMinIndex;
                }else if ((lowVolume == 0) || ((highVolume != 0) && (highVolume > lowVolume)))
                {
                    trialVolume += highVolume;
                    maxIndex = nextMaxIndex;
                }
                else
                {
                    break;
                }

            }
            VAH = Close[maxIndex];
            VAL = Close[minIndex];
        }

        /// <summary>
        /// Handle the out of range index errors.
        /// </summary>
        /// <param name="a">Given index</param>
        /// <param name="min">Mnimum index allowed</param>
        /// <param name="max">Maximum index allowed</param>
        /// <returns>The same index given if this within the minimum and maximum values.
        /// The minimum index allowed if the index given is less than the minimum value.
        /// The maximum index allowed if the index given is greater than the maximum value.
        /// </returns>
        private int clip(int a, int min, int max)
        {
            if (a < min)
            {
                a = min;
            }
            if (a > max)
            {
                a = max;
            }
            return a;
        }

        /// <summary>
        /// Round the decimal number
        /// </summary>
        /// <param name="a">The decimal number to round</param>
        /// <returns>The rounded decimal number</returns>
        private decimal Round(decimal a)
        {
            decimal roundoff = 1 / 0.05m;
            return Math.Ceiling(a * roundoff) / roundoff;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            DataPoints = new Queue<TradeBar>();
            Close = new List<decimal>();
            Volume = new Dictionary<decimal, decimal>();
            TotalVolume.Reset();
            POCIndex = 0;
            POCPrice = 0;
            POCVolume = 0;
            base.Reset();
        }
    }
}
