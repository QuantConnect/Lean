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

using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Server
{
    /// <summary>
    /// NOP implementation of the ILeanManager interface
    /// </summary>
    public class LocalLeanManager : ILeanManager
    {
        private LeanEngineSystemHandlers _systemHandlers;

        /// <summary>
        /// Empty implementation of the ILeanManager interface
        /// </summary>
        /// <param name="systemHandlers">Exposes lean engine system handlers running LEAN</param>
        /// <param name="algorithmHandlers">Exposes the lean algorithm handlers running lean</param>
        /// <param name="job">The job packet representing either a live or backtest Lean instance</param>
        /// <param name="algorithmManager">The Algorithm manager</param>
        public void Initialize(LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job, AlgorithmManager algorithmManager)
        {
            _systemHandlers = systemHandlers;
        }

        /// <summary>
        /// Sets the IAlgorithm instance in the ILeanManager
        /// </summary>
        /// <param name="algorithm">The IAlgorithm instance being run</param>
        public void SetAlgorithm(IAlgorithm algorithm)
        {
            algorithm.SetApi(_systemHandlers.Api);
            RemoteFileSubscriptionStreamReader.SetDownloadProvider((Api.Api)_systemHandlers.Api);
        }

        /// <summary>
        /// Update ILeanManager with the IAlgorithm instance
        /// </summary>
        public void Update()
        {
            // NOP
        }

        /// <summary>
        /// This method is called after algorithm initialization
        /// </summary>
        public void OnAlgorithmStart()
        {
            // NOP
        }

        /// <summary>
        /// This method is called before algorithm termination
        /// </summary>
        public void OnAlgorithmEnd()
        {
            // NOP
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // NOP
        }
    }
}
