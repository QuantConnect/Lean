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

namespace QuantConnect.Data.Market 
{
    /// <summary>
    /// Represents a range bar - trade bar of fixed length (high minus low).
    /// </summary>
    public class RangeBar : BaseData
    {
        private decimal _open;
        private decimal _high;
        private decimal _low;

        /// <summary>
        /// Bar length
        /// </summary>
        public decimal Length { get; set; }

        /// <summary>
        /// Volume:
        /// </summary>
        public virtual decimal Volume { get; set; }

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public virtual decimal Open {
            get { return _open; }
            set {
                _open = value;
            }
        }

        /// <summary>
        /// High price of the RangeBar during the time period.
        /// </summary>
        public virtual decimal High {
            get { return _high; }
            set {
                _high = value;
            }
        }

        /// <summary>
        /// Low price of the RangeBar during the time period.
        /// </summary>
        public virtual decimal Low {
            get { return _low; }
            set {
                _low = value;
            }
        }

        /// <summary>
        /// Closing price of the RangeBar.
        /// </summary>
        public virtual decimal Close {
            get { return Value; }
            set {
                Value = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeBar"/> class from a new tick
        /// </summary>
        /// <param name="tick">First tick to initialize the bar</param>
        /// <param name="length">Bar length</param>
        public RangeBar(Tick tick, decimal length)
        {
            Length = length;
            Symbol = tick.Symbol;
            Time = tick.Time;
            EndTime = tick.EndTime;
            Open = tick.Price;
            High = tick.Price;
            Low = tick.Price;
            Close = tick.Price;
            Volume = tick.Quantity;
        }

        /// <summary>
        /// Updates <see cref="RangeBar"/> with the specified values and returns whether or not this bar should be closed
        /// </summary>
        /// <param name="tick">last tick</param>
        /// <returns></returns>
        public bool Update(Tick tick)
        {
            var price = tick.Price;
            var volume = tick.Quantity;
            var time = tick.Time;

            if (price > High)
            {
                if (price - Low > Length)
                {
                    return true;
                }

                High = price;
            }

            if (price < Low)
            {
                if (High - price > Length) 
                {
                    return true;
                }

                Low = price;
            }
            
            Volume += volume;
            Close = price;
            EndTime = time;

            return false;
        }

        /// <summary>
        /// Formats a string with the symbol and values.
        /// </summary>
        public override string ToString() {
            return $"{Symbol}: {Time} - {EndTime} " +
                $"O: {Open.SmartRounding()} " +
                $"H: {High.SmartRounding()} " +
                $"L: {Low.SmartRounding()} " +
                $"C: {Close.SmartRounding()} " +
                $"V: {Volume.SmartRounding()}";
        }
    }
}
