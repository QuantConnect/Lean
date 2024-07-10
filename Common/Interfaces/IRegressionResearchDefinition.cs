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

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Defines interface for research notebooks to be run as part of the research test suite.
    /// </summary>
    public interface IRegressionResearchDefinition
    {
        #pragma warning disable CS1574
        /// <summary>
        /// This is used by the research regression test system to validate the output
        /// </summary>
        /// <remarks>Requires to be implemented last in the file <see cref="ResearchRegressionTests.UpdateResearchRegressionOutputInSourceFile"/>
        /// get should start from next line</remarks>
        #pragma warning restore CS1574
        string ExpectedOutput { get; }
    }
}
