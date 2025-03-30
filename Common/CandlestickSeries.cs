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

using System.Collections.Generic;
using System;
using QuantConnect.Data.Market;
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// Candlestick Chart Series Object - Series data and properties for a candlestick chart
    /// </summary>
    [JsonConverter(typeof(SeriesJsonConverter))]
    public class CandlestickSeries : BaseSeries
    {
        /// <summary>
        /// Default constructor for chart series
        /// </summary>
        public CandlestickSeries() : base()
        {
            SeriesType = SeriesType.Candle;
        }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        public CandlestickSeries(string name)
            : base(name, SeriesType.Candle)
        {
        }

        /// <summary>
        /// Foundational constructor on the series class
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="index">Index position on the chart of the series</param>
        public CandlestickSeries(string name, int index)
            : this(name, index, "$")
        {
        }

        /// <summary>
        /// Foundational constructor on the series class
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="index">Index position on the chart of the series</param>
        /// <param name="unit">Unit for the series axis</param>
        public CandlestickSeries(string name, int index, string unit)
            : this(name, unit)
        {
            Index = index;
        }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        /// <param name="unit">Unit of the series</param>
        public CandlestickSeries(string name, string unit)
            : base(name, SeriesType.Candle, unit)
        {
        }

        /// <summary>
        /// Add a new point to this series
        /// </summary>
        /// <param name="time">Time of the chart point</param>
        /// <param name="open">Candlestick open price</param>
        /// <param name="high">Candlestick high price</param>
        /// <param name="low">Candlestick low price</param>
        /// <param name="close">Candlestick close price</param>
        public void AddPoint(DateTime time, decimal open, decimal high, decimal low, decimal close)
        {
            base.AddPoint(new Candlestick(time, open, high, low, close));
        }

        /// <summary>
        /// Add a new point to this series
        /// </summary>
        public void AddPoint(TradeBar bar)
        {
            base.AddPoint(new Candlestick(bar));
        }

        /// <summary>
        /// Add a new point to this series
        /// </summary>
        /// <param name="point">The data point to add</param>
        public override void AddPoint(ISeriesPoint point)
        {
            if (point as Candlestick == null)
            {
                throw new ArgumentException("CandlestickSeries.AddPoint requires a Candlestick object");
            }

            base.AddPoint(point);
        }

        /// <summary>
        /// Add a new point to this series
        /// </summary>
        /// <param name="time">The time of the data point</param>
        /// <param name="values">The values of the data point</param>
        public override void AddPoint(DateTime time, List<decimal> values)
        {
            if (values.Count != 4)
            {
                throw new ArgumentException("CandlestickSeries.AddPoint requires 4 values (open, high, low, close)");
            }

            base.AddPoint(new Candlestick(time, values[0], values[1], values[2], values[3]));
        }

        /// <summary>
        /// Will sum up all candlesticks into a new single one, using the time of latest point
        /// </summary>
        /// <returns>The new candlestick</returns>
        public override ISeriesPoint ConsolidateChartPoints()
        {
            if (Values.Count <= 0) return null;

            decimal? openSum = null;
            decimal? highSum = null;
            decimal? lowSum = null;
            decimal? closeSum = null;
            foreach (Candlestick point in Values)
            {
                if (point.Open.HasValue)
                {
                    openSum ??= 0;
                    openSum += point.Open.Value;
                }
                if (point.High.HasValue)
                {
                    highSum ??= 0;
                    highSum += point.High.Value;
                }
                if (point.Low.HasValue)
                {
                    lowSum ??= 0;
                    lowSum += point.Low.Value;
                }
                if (point.Close.HasValue)
                {
                    closeSum ??= 0;
                    closeSum += point.Close.Value;
                }
            }

            var lastCandlestick = Values[Values.Count - 1];
            return new Candlestick(lastCandlestick.Time, openSum, highSum, lowSum, closeSum);
        }

        /// <summary>
        /// Return a new instance clone of this object
        /// </summary>
        /// <returns></returns>
        public override BaseSeries Clone(bool empty = false)
        {
            var series = new CandlestickSeries(Name, Index, Unit) { ZIndex = ZIndex, IndexName = IndexName, Tooltip = Tooltip };

            if (!empty)
            {
                series.Values = CloneValues();
            }

            return series;
        }
    }
}
