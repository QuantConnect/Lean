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
 *
*/

using System;
using System.ComponentModel.Composition;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Server;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Provides a container for the system level handlers
    /// </summary>
    public class LeanEngineSystemHandlers : IDisposable
    {
        private readonly IApi _api;
        private readonly IMessagingHandler _notify;
        private readonly IJobQueueHandler _jobQueue;
        private readonly ILeanManager _leanManager;

        /// <summary>
        /// Gets the api instance used for communicating algorithm limits, status, and storing of log data
        /// </summary>
        public IApi Api
        {
            get { return _api; }
        }

        /// <summary>
        /// Gets the messaging handler instance used for communicating various packets to listeners, including
        /// debug/log messages, email/sms/web messages, as well as results and run time errors
        /// </summary>
        public IMessagingHandler Notify
        {
            get { return _notify; }
        }

        /// <summary>
        /// Gets the job queue responsible for acquiring and acknowledging an algorithm job
        /// </summary>
        public IJobQueueHandler JobQueue
        {
            get { return _jobQueue; }
        }

        /// <summary>
        /// Gets the ILeanManager implementation using to enhance the hosting environment
        /// </summary>
        public ILeanManager LeanManager
        {
            get { return _leanManager; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanEngineSystemHandlers"/> class with the specified handles
        /// </summary>
        /// <param name="jobQueue">The job queue used to acquire algorithm jobs</param>
        /// <param name="api">The api instance used for communicating limits and status</param>
        /// <param name="notify">The messaging handler user for passing messages from the algorithm to listeners</param>
        /// <param name="leanManager"></param>
        public LeanEngineSystemHandlers(IJobQueueHandler jobQueue, IApi api, IMessagingHandler notify, ILeanManager leanManager)
        {
            if (jobQueue == null)
            {
                throw new ArgumentNullException("jobQueue");
            }
            if (api == null)
            {
                throw new ArgumentNullException("api");
            }
            if (notify == null)
            {
                throw new ArgumentNullException("notify");
            }
            if (leanManager == null)
            {
                throw new ArgumentNullException("leanManager");
            }
            _api = api;
            _jobQueue = jobQueue;
            _notify = notify;
            _leanManager = leanManager;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="LeanEngineSystemHandlers"/> class from the specified composer using type names from configuration
        /// </summary>
        /// <param name="composer">The composer instance to obtain implementations from</param>
        /// <returns>A fully hydrates <see cref="LeanEngineSystemHandlers"/> instance.</returns>
        /// <exception cref="CompositionException">Throws a CompositionException during failure to load</exception>
        public static LeanEngineSystemHandlers FromConfiguration(Composer composer)
        {
            return new LeanEngineSystemHandlers(
                composer.GetExportedValueByTypeName<IJobQueueHandler>(Config.Get("job-queue-handler")),
                composer.GetExportedValueByTypeName<IApi>(Config.Get("api-handler")),
                composer.GetExportedValueByTypeName<IMessagingHandler>(Config.Get("messaging-handler")), 
                composer.GetExportedValueByTypeName<ILeanManager>(Config.Get("lean-manager-type", "LocalLeanManager")));
        }

        /// <summary>
        /// Initializes the Api, Messaging, and JobQueue components
        /// </summary>
        public void Initialize()
        {
            Api.Initialize(Config.GetInt("job-user-id", 0), Config.Get("api-access-token", ""), Config.Get("data-folder"));
            Notify.Initialize();
            JobQueue.Initialize(Api);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Api.Dispose();
            LeanManager.Dispose();
            Log.Trace("LeanEngineSystemHandlers.Dispose(): Disposed of system handlers.");
        }
    }
}