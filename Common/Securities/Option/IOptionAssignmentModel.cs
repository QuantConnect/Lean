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

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// The option assignment model emulates exercising of short option positions in the portfolio.
    /// </summary>
    public interface IOptionAssignmentModel
    {
        /// <summary>
        /// Get's the option assignments to generate if any
        /// </summary>
        /// <param name="parameters">The option assignment parameters data transfer class</param>
        /// <returns>The option assignment result</returns>
        public OptionAssignmentResult GetAssignment(OptionAssignmentParameters parameters);
    }
}
