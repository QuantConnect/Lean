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

namespace QuantConnect.Util
{
    /// <summary>
    /// This class allows implicit conversions between Resolution and TimeSpan
    /// </summary>
    public struct ResolutionTimeSpan
    {
        private readonly TimeSpan _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResolutionTimeSpan"/> class
        /// </summary>
        /// <param name="timeSpan">The timespan to use</param>
        public ResolutionTimeSpan(TimeSpan timeSpan)
        {
            _value = timeSpan;
        }

        /// <summary>
        ///  User-defined conversion from ResolutionTimeSpan to TimeSpan
        /// </summary>
        /// <param name="resolutionTimeSpan">The <see cref="ResolutionTimeSpan"/> to convert from</param>
        public static implicit operator TimeSpan(ResolutionTimeSpan resolutionTimeSpan)
        {
            return resolutionTimeSpan._value;
        }

        /// <summary>
        /// User-defined conversion from TimeSpan to ResolutionTimeSpan
        /// </summary>
        /// <param name="timeSpan">The <see cref="TimeSpan"/> to convert from</param>
        public static implicit operator ResolutionTimeSpan(TimeSpan timeSpan)
        {
            return new ResolutionTimeSpan(timeSpan);
        }

        /// <summary>
        /// User-defined conversion from Resolution to ResolutionTimeSpan
        /// </summary>
        /// <param name="resolution">the <see cref="Resolution"/> to convert from</param>
        public static implicit operator ResolutionTimeSpan(Resolution resolution)
        {
            return new ResolutionTimeSpan(resolution.ToTimeSpan());
        }

        /// <summary>
        /// Returns a nice name for the resolution where possible.
        /// </summary>
        /// <returns></returns>
        public new string ToString()
        {
            var higherResolutionEquivalent = ((TimeSpan)this).ToHigherResolutionEquivalent(false);

            string res;
            switch (higherResolutionEquivalent)
            {
                case Resolution.Tick:
                    res = "_tick";
                    break;

                case Resolution.Second:
                    res = "_sec";
                    break;

                case Resolution.Minute:
                    res = "_min";
                    break;

                case Resolution.Hour:
                    res = "_hr";
                    break;

                case Resolution.Daily:
                    res = "_day";
                    break;

                default:
                    res = ((TimeSpan)this).ToString();
                    break;
            }
            return res;
        }
    }
}
