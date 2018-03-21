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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Defines the different types of moving averages
    /// </summary>  
    public enum MovingAverageType
    {
        /// <summary>
        /// An unweighted, arithmetic mean
        /// </summary>
        Simple,
        /// <summary>
        /// The standard exponential moving average, using a smoothing factor of 2/(n+1)
        /// </summary>
        Exponential,
        /// <summary>
        /// An exponential moving average, using a smoothing factor of 1/n and simple moving average as seeding
        /// </summary>
        Wilders,
        /// <summary>
        /// A weighted moving average type
        /// </summary>
        LinearWeightedMovingAverage,
        /// <summary>
        /// The double exponential moving average
        /// </summary>
        DoubleExponential,
        /// <summary>
        /// The triple exponential moving average
        /// </summary>
        TripleExponential,
        /// <summary>
        /// The triangular moving average
        /// </summary>
        Triangular,
        /// <summary>
        /// The T3 moving average
        /// </summary>
        T3,
        /// <summary>
        /// The Kaufman Adaptive Moving Average
        /// </summary>
        Kama,
        /// <summary>
        /// The Hull Moving Average
        /// </summary>
        Hull,
        /// <summary>
        /// The Arnaud Legoux Moving Average
        /// </summary>
        Alma
    }
}
