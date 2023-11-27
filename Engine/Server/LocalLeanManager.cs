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

using QuantConnect.Util;
using QuantConnect.Packets;
using QuantConnect.Commands;
using QuantConnect.Interfaces;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Transport;

namespace QuantConnect.Lean.Engine.Server
{
    /// <summary>
    /// NOP implementation of the ILeanManager interface
    /// </summary>
    public class LocalLeanManager : ILeanManager
    {
        /// <summary>
        /// The current algorithm
        /// </summary>
        protected IAlgorithm Algorithm { get; set; }

        private AlgorithmNodePacket _job;
        private ICommandHandler _commandHandler;

        /// <summary>
        /// The system handlers
        /// </summary>
        protected LeanEngineSystemHandlers SystemHandlers { get; set; }

        /// <summary>
        /// The algorithm handlers
        /// </summary>
        protected LeanEngineAlgorithmHandlers AlgorithmHandlers { get; set; }

        /// <summary>
        /// Empty implementation of the ILeanManager interface
        /// </summary>
        /// <param name="systemHandlers">Exposes lean engine system handlers running LEAN</param>
        /// <param name="algorithmHandlers">Exposes the lean algorithm handlers running lean</param>
        /// <param name="job">The job packet representing either a live or backtest Lean instance</param>
        /// <param name="algorithmManager">The Algorithm manager</param>
        public virtual void Initialize(LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job, AlgorithmManager algorithmManager)
        {
            AlgorithmHandlers = algorithmHandlers;
            SystemHandlers = systemHandlers;
            _job = job;
        }

        /// <summary>
        /// Sets the IAlgorithm instance in the ILeanManager
        /// </summary>
        /// <param name="algorithm">The IAlgorithm instance being run</param>
        public virtual void SetAlgorithm(IAlgorithm algorithm)
        {
            Algorithm = algorithm;
            algorithm.SetApi(SystemHandlers.Api);
            RemoteFileSubscriptionStreamReader.SetDownloadProvider((Api.Api)SystemHandlers.Api);
        }

        /// <summary>
        /// Execute the commands using the IAlgorithm instance
        /// </summary>
        public virtual void Update()
        {
            if(_commandHandler != null)
            {
                foreach (var commandResultPacket in _commandHandler.ProcessCommands())
                {
                    AlgorithmHandlers.Results.Messages.Enqueue(commandResultPacket);
                }
            }
        }

        /// <summary>
        /// This method is called after algorithm initialization
        /// </summary>
        public virtual void OnAlgorithmStart()
        {
            if (Algorithm.LiveMode)
            {
                _commandHandler = new FileCommandHandler();
                _commandHandler.Initialize(_job, Algorithm);
            }
        }

        /// <summary>
        /// This method is called before algorithm termination
        /// </summary>
        public virtual void OnAlgorithmEnd()
        {
            // NOP
        }

        /// <summary>
        /// Callback fired each time that we add/remove securities from the data feed
        /// </summary>
        public virtual void OnSecuritiesChanged(SecurityChanges changes)
        {
            // NOP
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            _commandHandler.DisposeSafely();
        }
    }
}
