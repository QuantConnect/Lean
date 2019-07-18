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
    /// Provides scope into Lean that is convenient for managing a lean instance
    /// </summary>
    [InheritedExport(typeof(ILeanManager))]
    public interface ILeanManager : IDisposable
    {
        /// <summary>
        /// Initialize the ILeanManager implementation
        /// </summary>
        /// <param name="systemHandlers">Exposes lean engine system handlers running LEAN</param>
        /// <param name="algorithmHandlers">Exposes the lean algorithm handlers running lean</param>
        /// <param name="job">The job packet representing either a live or backtest Lean instance</param>
        /// <param name="algorithmManager">The Algorithm manager</param>
        void Initialize(LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job, AlgorithmManager algorithmManager);

        /// <summary>
        /// Sets the IAlgorithm instance in the ILeanManager
        /// </summary>
        /// <param name="algorithm">The IAlgorithm instance being run</param>
        void SetAlgorithm(IAlgorithm algorithm);

        /// <summary>
        /// Update ILeanManager with the IAlgorithm instance
        /// </summary>
        void Update();

        /// <summary>
        /// This method is called after algorithm initialization
        /// </summary>
        void OnAlgorithmStart();

        /// <summary>
        /// This method is called before algorithm termination
        /// </summary>
        void OnAlgorithmEnd();
    }
}
