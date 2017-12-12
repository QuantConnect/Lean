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
 *
*/

using System;

namespace QuantConnect.Algorithm.Framework.Signals
{
    /// <summary>
    /// Defines the scores given to a particular signal
    /// </summary>
    public class SignalScore
    {
        /// <summary>
        /// Gets the direction score
        /// </summary>
        public double Direction { get; private set; }

        /// <summary>
        /// Gets the magnitude score
        /// </summary>
        public double Magnitude { get; private set; }

        public SignalScore(double direction, double magnitude)
        {
            Direction = direction;
            Magnitude = magnitude;
        }

        public void SetScore(SignalScoreType type, double value)
        {
            switch (type)
            {
                case SignalScoreType.Direction:
                    Direction = value;
                    break;

                case SignalScoreType.Magnitude:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public double GetScore(SignalScoreType type)
        {
            switch (type)
            {
                case SignalScoreType.Direction:
                    return Direction;

                case SignalScoreType.Magnitude:
                    return Magnitude;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public override string ToString()
        {
            return $"Direction: {Math.Round(100 * Direction, 2)} Magnitude: {Math.Round(100 * Magnitude, 2)}";
        }
    }
}