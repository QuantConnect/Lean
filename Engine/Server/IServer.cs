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
using System.ComponentModel.Composition;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Server
{
    /// <summary>
    /// Provides an outer scope to Lean and Lean.Engine that is convenient
    /// for specializing logic around the server hosting Lean
    /// </summary>
    [InheritedExport(typeof(IServer))]
    public interface IServer : IDisposable
    {
        /// <summary>
        /// Initialize the IServer implementation
        /// </summary>
        /// <param name="systemHandlers">Exposes lean engine system handlers running LEAN</param>
        /// <param name="algorithmHandlers">Exposes the lean algorithm handlers running lean</param>
        /// <param name="job">The job packet representing either a live or backtest Lean instance</param>
        /// <param name="algorithmManager">The Algorithm manager</param>
        void Initialize(LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job, AlgorithmManager algorithmManager);

        /// <summary>
        /// Sets the IAlgorithm instance in the IServer
        /// </summary>
        /// <param name="algorithm">The IAlgorithm instance being run</param>
        void SetAlgorithm(IAlgorithm algorithm);

        /// <summary>
        /// Update IServer with the IAlgorithm instance
        /// </summary>
        void Update();
    }
}
