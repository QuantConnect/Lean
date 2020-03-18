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

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Specifies the bias of the portfolio (Short, Long/Short, Long) 
    /// </summary>
    public enum PortfolioBias
    {
        /// <summary>
        /// Portfolio can only have short positions
        /// </summary>
        Short = -1,

        /// <summary>
        /// Portfolio can have both long and short positions
        /// </summary>
        LongShort = 0,

        /// <summary>
        /// Portfolio can only have long positions
        /// </summary>
        Long = 1
    }
}