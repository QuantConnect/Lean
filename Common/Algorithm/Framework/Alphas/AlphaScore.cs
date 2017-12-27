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

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Defines the scores given to a particular alpha
    /// </summary>
    public class AlphaScore
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
        /// Gets whether or not this is the alpha's final score
        /// </summary>
        public bool IsFinalScore { get; private set; }

        /// <summary>
        /// Initializes a new, default instance of the <see cref="AlphaScore"/> class
        /// </summary>
        public AlphaScore()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaScore"/> class
        /// </summary>
        /// <param name="direction">The alpha direction score</param>
        /// <param name="magnitude">The alpha percent change score</param>
        /// <param name="updatedTimeUtc">The algorithm utc time these scores were computed</param>
        public AlphaScore(double direction, double magnitude, DateTime updatedTimeUtc)
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
        internal void SetScore(AlphaScoreType type, double value, DateTime algorithmUtcTime)
        {
            if (IsFinalScore) return;

            UpdatedTimeUtc = algorithmUtcTime;

            switch (type)
            {
                case AlphaScoreType.Direction:
                    Direction = Math.Max(0, Math.Min(1, value));
                    break;

                case AlphaScoreType.Magnitude:
                    Magnitude = Math.Max(0, Math.Min(1, value));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>
        /// Marks the score as finalized, preventing any further updates.
        /// </summary>
        /// <param name="algorithmUtcTime">The algorithm's utc time at which time these scores were finalized</param>
        internal void Finalize(DateTime algorithmUtcTime)
        {
            IsFinalScore = true;
            UpdatedTimeUtc = algorithmUtcTime;
        }

        /// <summary>
        /// Gets the specified score
        /// </summary>
        /// <param name="type">The type of score to get, Direction/Magnitude</param>
        /// <returns>The requested score</returns>
        public double GetScore(AlphaScoreType type)
        {
            switch (type)
            {
                case AlphaScoreType.Direction:
                    return Direction;

                case AlphaScoreType.Magnitude:
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
            return $"Direction: {Math.Round(100 * Direction, 2)} PercentChange: {Math.Round(100 * Magnitude, 2)}";
        }
    }
}