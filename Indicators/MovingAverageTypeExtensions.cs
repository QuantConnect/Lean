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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Provides extension methods for the MovingAverageType enumeration
    /// </summary>
    public static class MovingAverageTypeExtensions
    {
        /// <summary>
        /// Creates a new indicator from the specified MovingAverageType. So if MovingAverageType.Simple
        /// is specified, then a new SimpleMovingAverage will be returned.
        /// </summary>
        /// <param name="movingAverageType">The type of averaging indicator to create</param>
        /// <param name="period">The smoothing period</param>
        /// <returns>A new indicator that matches the MovingAverageType</returns>
        public static IndicatorBase<IndicatorDataPoint> AsIndicator(this MovingAverageType movingAverageType, int period)
        {
            switch (movingAverageType)
            {
                case MovingAverageType.Simple:
                    return new SimpleMovingAverage(period);

                case MovingAverageType.Exponential:
                    return new ExponentialMovingAverage(period);

                case MovingAverageType.Wilders:
                    return new WilderMovingAverage(period);

                case MovingAverageType.LinearWeightedMovingAverage:
                    return new LinearWeightedMovingAverage(period);

                case MovingAverageType.DoubleExponential:
                    return new DoubleExponentialMovingAverage(period);

                case MovingAverageType.TripleExponential:
                    return new TripleExponentialMovingAverage(period);

                case MovingAverageType.Triangular:
                    return new TriangularMovingAverage(period);

                case MovingAverageType.T3:
                    return new T3MovingAverage(period);

                case MovingAverageType.Kama:
                    return new KaufmanAdaptiveMovingAverage(period);

                case MovingAverageType.Hull:
                    return new HullMovingAverage(period);

                case MovingAverageType.Alma:
                    return new ArnaudLegouxMovingAverage(period);

                default:
                    throw new ArgumentOutOfRangeException(nameof(movingAverageType));
            }
        }

        /// <summary>
        /// Creates a new indicator from the specified MovingAverageType. So if MovingAverageType.Simple
        /// is specified, then a new SimpleMovingAverage will be returned.
        /// </summary>
        /// <param name="movingAverageType">The type of averaging indicator to create</param>
        /// <param name="name">The name of the new indicator</param>
        /// <param name="period">The smoothing period</param>
        /// <returns>A new indicator that matches the MovingAverageType</returns>
        public static IndicatorBase<IndicatorDataPoint> AsIndicator(this MovingAverageType movingAverageType, string name, int period)
        {
            switch (movingAverageType)
            {
                case MovingAverageType.Simple:
                    return new SimpleMovingAverage(name, period);

                case MovingAverageType.Exponential:
                    return new ExponentialMovingAverage(name, period);

                case MovingAverageType.Wilders:
                    return new WilderMovingAverage(name, period);

                case MovingAverageType.LinearWeightedMovingAverage:
                    return new LinearWeightedMovingAverage(name, period);

                case MovingAverageType.DoubleExponential:
                    return new DoubleExponentialMovingAverage(name, period);

                case MovingAverageType.TripleExponential:
                    return new TripleExponentialMovingAverage(name, period);

                case MovingAverageType.Triangular:
                    return new TriangularMovingAverage(name, period);

                case MovingAverageType.T3:
                    return new T3MovingAverage(name, period);

                case MovingAverageType.Kama:
                    return new KaufmanAdaptiveMovingAverage(name, period);

                case MovingAverageType.Hull:
                    return new HullMovingAverage(name, period);
                
                case MovingAverageType.Alma:
                    return new ArnaudLegouxMovingAverage(name, period);

                default:
                    throw new ArgumentOutOfRangeException(nameof(movingAverageType));
            }
        }
    }
}
