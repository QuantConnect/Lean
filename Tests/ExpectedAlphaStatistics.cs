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
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Tests
{
    /// <summary>
    /// Wraps <see cref="AlphaRuntimeStatistics"/> to clean the syntax of defining expected alpha results
    /// </summary>
    public class ExpectedAlphaStatistics : AlphaRuntimeStatistics
    {
        public double MeanPopuationDirection
        {
            set { MeanPopulationScore.SetScore(AlphaScoreType.Direction, value, new DateTime()); }
        }

        public double MeanPopuationMagnitude
        {
            set { MeanPopulationScore.SetScore(AlphaScoreType.Magnitude, value, new DateTime()); }
        }

        public double RollingAveragedPopuationDirection
        {
            set { RollingAveragedPopulationScore.SetScore(AlphaScoreType.Direction, value, new DateTime()); }
        }

        public double RollingAveragedPopuationMagnitude
        {
            set { RollingAveragedPopulationScore.SetScore(AlphaScoreType.Magnitude, value, new DateTime()); }
        }
    }
}