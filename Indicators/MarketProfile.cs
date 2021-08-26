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
    public class MarketProfile : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private int _period;

        private decimal _roundoff;

        /// <summary>
        /// Rolling Window to erase old DataPoints out of the given period
        /// </summary>
        protected RollingWindow<TradeBar> DataPoints;

        /// <summary>
        /// Close values in the given period of time
        /// </summary>
        protected List<decimal> Close;

        /// <summary>
        /// If the mode is TPO, Volume represent the frequency of the Close values.
        /// Otherwise, Volume represent the sum of the Volume values for the same Close value. 
        /// </summary>
        protected Dictionary<decimal, decimal> Volume;

        /// <summary>
        /// The highest reached close price level during the period
        /// </summary>
        private decimal _profilehigh;
        public decimal PH
        {
            get { return _profilehigh; }
        }

        /// <summary>
        /// The lowest reached close price level during the period
        /// </summary>
        public decimal _profilelow;
        public decimal PL
        {
            get { return _profilelow; }
        }

        /// <summary>
        /// A rolling sum of the Volume values for the given period
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> TotalVolume { get; }

        /// <summary>
        /// Price where the most trading occured (Point of Control(POC))
        /// This price is MarketProfile.Current.Value
        /// </summary>
        private decimal _pocprice;
        public decimal POCPrice
        {
            get { return _pocprice; }
        }

        /// <summary>
        /// Volume where the most tradding occured (Point of Control(POC))
        /// </summary>
        private decimal _pocvolume;
        public decimal POCVolume
        {
            get { return _pocvolume; }
        }

        /// <summary>
        /// 70% of the day's trading
        /// </summary>
        public decimal _valuearea;
        public decimal ValueArea
        {
            get { return _valuearea; }
        }

        /// <summary>
        /// The highest close price level within the value area
        /// </summary>
        public decimal _vah;
        public decimal VAH
        {
            get { return _vah; }
        }

        /// <summary>
        /// The lowest close price level within the value area
        /// </summary>
        public decimal _val;
        public decimal VAL
        {
            get { return _val; }
        }

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
        public MarketProfile(int period = 2)
            : this($"MP({period})", period)
        {
        }

        /// <summary>
        /// Creates a new MarkProfile indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="roundoff">The amount of round. By default 0.05</param>
        public MarketProfile(string name, int period, decimal roundoff = 0.05m)
            : base(name)
        {
            DataPoints = new RollingWindow<TradeBar>(period);
            Close = new List<decimal>();
            Volume = new Dictionary<decimal, decimal>();
            TotalVolume = new Sum(name + "_Sum", period);
            _period = period;
            POCIndex = 0;
            _pocprice = 0;
            _pocvolume = 0;
            _roundoff = 1 / roundoff;
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A a value for this indicator, Point of Control (POC) price</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            DataPoints.Add(input);
            Add(input);
            POCIndex = GetMax();
            // Get the POC price and volume values
            _pocprice = Close[POCIndex];
            _pocvolume = Volume[_pocprice];

            // Get the highest and lowest close prices
            _profilehigh = Close.Max();
            _profilelow = Close.Min();

            // Calulate the Value area
            CalculateValueArea();

            return POCPrice;
        }

        /// <summary>
        /// Add the new input value to the Close array and Volume dictionary.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        public virtual void Add(TradeBar input) { }

        /// <summary>
        /// Finds the close price with biggest value in Volume.
        /// </summary>
        /// <returns> Close price with biggest value in Volume</returns>
        private int GetMax()
        {
            int Max_idx = 0;
            for (int i = 0; i < Close.Count; i++)
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
            _valuearea = TotalVolume.Current.Value * 0.70m;
            decimal trialVolume = _pocvolume;

            int minIndex = POCIndex;
            int maxIndex = POCIndex;

            int lastMin, lastMax;
            int nextMinIndex, nextMaxIndex;

            decimal lowVolume, highVolume;
            while (trialVolume <= _valuearea)
            {
                lastMin = minIndex;
                lastMax = maxIndex;

                nextMinIndex = Clip(minIndex - 1, 0, Close.Count - 1);
                nextMaxIndex = Clip(maxIndex + 1, 0, Close.Count - 1);

                if (nextMinIndex != lastMin)
                {
                    lowVolume = Volume[Close[nextMinIndex]];
                }
                else
                {
                    lowVolume = 0;
                }

                if (nextMaxIndex != lastMax)
                {
                    highVolume = Volume[Close[nextMaxIndex]];
                }
                else
                {
                    highVolume = 0;
                }

                if ((highVolume == 0) || ((lowVolume != 0) && (lowVolume > highVolume)))
                {
                    trialVolume += lowVolume;
                    minIndex = nextMinIndex;
                }
                else if ((lowVolume == 0) || ((highVolume != 0) && (highVolume > lowVolume)))
                {
                    trialVolume += highVolume;
                    maxIndex = nextMaxIndex;
                }
                else
                {
                    break;
                }

            }
            _vah = Close[maxIndex];
            _val = Close[minIndex];
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
        private static int Clip(int a, int min, int max)
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
        protected decimal Round(decimal a)
        {
            return Math.Ceiling(a * _roundoff) / _roundoff;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            DataPoints = new RollingWindow<TradeBar>(_period);
            Close = new List<decimal>();
            Volume = new Dictionary<decimal, decimal>();
            TotalVolume.Reset();
            POCIndex = 0;
            _pocprice = 0;
            _pocvolume = 0;
            base.Reset();
        }
    }
}
