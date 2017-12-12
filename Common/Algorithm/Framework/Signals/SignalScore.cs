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
        /// Gets the time these scores were last updated
        /// </summary>
        public DateTime UpdatedTimeUtc { get; private set; }

        /// <summary>
        /// Gets the direction score
        /// </summary>
        public double Direction { get; private set; }

        /// <summary>
        /// Gets the magnitude score
        /// </summary>
        public double Magnitude { get; private set; }

        /// <summary>
        /// Initializes a new, default instance of the <see cref="SignalScore"/> class
        /// </summary>
        public SignalScore()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalScore"/> class
        /// </summary>
        /// <param name="direction">The signal direction score</param>
        /// <param name="magnitude">The signal magnitude score</param>
        /// <param name="updatedTimeUtc">The algorithm utc time these scores were computed</param>
        public SignalScore(double direction, double magnitude, DateTime updatedTimeUtc)
        {
            Direction = direction;
            Magnitude = magnitude;
            UpdatedTimeUtc = updatedTimeUtc;
        }

        /// <summary>
        /// Sets the specified score type with the value
        /// </summary>
        /// <param name="type">The score type to be set, Direction/Magnitude</param>
        /// <param name="value">The new value for the score</param>
        /// <param name="algorithmUtcTime">The algorithm's utc time at which time the new score was computed</param>
        public void SetScore(SignalScoreType type, double value, DateTime algorithmUtcTime)
        {
            UpdatedTimeUtc = algorithmUtcTime;

            switch (type)
            {
                case SignalScoreType.Direction:
                    Direction = value;
                    break;

                case SignalScoreType.Magnitude:
                    Magnitude = value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>
        /// Gets the specified score
        /// </summary>
        /// <param name="type">The type of score to get, Direction/Magnitude</param>
        /// <returns>The requested score</returns>
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

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return $"Direction: {Math.Round(100 * Direction, 2)} Magnitude: {Math.Round(100 * Magnitude, 2)}";
        }
    }
}