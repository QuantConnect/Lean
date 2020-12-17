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

using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// This consolidator can transform a stream of <see cref="BaseData"/> instances into a stream of <see cref="RenkoBar"/>
    /// with Renko type <see cref="RenkoType.Wicked"/>.
    /// </summary>
    public class WickedRenkoConsolidator : BaseRenkoConsolidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WickedRenkoConsolidator"/> class using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        public WickedRenkoConsolidator(decimal barSize)
            : base(barSize)
        {
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(IBaseData data)
        {
            var rate = data.Price;

            if (FirstTick)
            {
                FirstTick = false;

                OpenOn = data.Time;
                CloseOn = data.Time;
                OpenRate = rate;
                HighRate = rate;
                LowRate = rate;
                CloseRate = rate;
            }
            else
            {
                CloseOn = data.Time;

                if (rate > HighRate) HighRate = rate;

                if (rate < LowRate) LowRate = rate;

                CloseRate = rate;

                if (CloseRate > OpenRate)
                {
                    if (LastWicko == null || LastWicko.Direction == BarDirection.Rising)
                    {
                        Rising(data);
                        return;
                    }

                    var limit = LastWicko.Open + BarSize;

                    if (CloseRate > limit)
                    {
                        var wicko = new RenkoBar(data.Symbol, OpenOn, CloseOn, BarSize, LastWicko.Open, limit,
                            LowRate, limit);

                        LastWicko = wicko;

                        OnDataConsolidated(wicko);

                        OpenOn = CloseOn;
                        OpenRate = limit;
                        LowRate = limit;

                        Rising(data);
                    }
                }
                else if (CloseRate < OpenRate)
                {
                    if (LastWicko == null || LastWicko.Direction == BarDirection.Falling)
                    {
                        Falling(data);
                        return;
                    }

                    var limit = LastWicko.Open - BarSize;

                    if (CloseRate < limit)
                    {
                        var wicko = new RenkoBar(data.Symbol, OpenOn, CloseOn, BarSize, LastWicko.Open, HighRate,
                            limit, limit);

                        LastWicko = wicko;

                        OnDataConsolidated(wicko);

                        OpenOn = CloseOn;
                        OpenRate = limit;
                        HighRate = limit;

                        Falling(data);
                    }
                }
            }
        }

        private void Rising(IBaseData data)
        {
            decimal limit;

            while (CloseRate > (limit = OpenRate + BarSize))
            {
                var wicko = new RenkoBar(data.Symbol, OpenOn, CloseOn, BarSize, OpenRate, limit, LowRate, limit);

                LastWicko = wicko;

                OnDataConsolidated(wicko);

                OpenOn = CloseOn;
                OpenRate = limit;
                LowRate = limit;
            }
        }

        private void Falling(IBaseData data)
        {
            decimal limit;

            while (CloseRate < (limit = OpenRate - BarSize))
            {
                var wicko = new RenkoBar(data.Symbol, OpenOn, CloseOn, BarSize, OpenRate, HighRate, limit, limit);

                LastWicko = wicko;

                OnDataConsolidated(wicko);

                OpenOn = CloseOn;
                OpenRate = limit;
                HighRate = limit;
            }
        }
    }

    /*/// <summary>
    /// Provides a type safe wrapper on the RenkoConsolidator class of "<see cref="RenkoType.Wicked"/>" type.
    /// </summary>
    public class WickedRenkoConsolidator<TInput> : BaseRenkoConsolidator
        where TInput : IBaseData

    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoConsolidator"/> class of "<see cref="RenkoType.Wicked"/>"
        /// type using the specified <paramref name="barSize"/>.
        /// </summary>
        /// <param name="barSize">The constant value size of each bar</param>
        public WickedRenkoConsolidator(decimal barSize)
            : base(barSize)
        {
        }


        /// <summary>
        /// Updates this consolidator with the specified data.
        /// </summary>
        /// <remarks>
        /// Type safe shim method.
        /// </remarks>
        /// <param name="data">The new data for the consolidator</param>
        public void Update(TInput data)
        {
            base.Update(data);
        }
    }*/
}
