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
/**********************************************************
* USING NAMESPACES
**********************************************************/

using System.Collections.Generic;
using System.IO;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Queues
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Implementation of local/desktop job request:
    /// </summary>
    public class JobQueue : IJobQueueHandler
    {
        // The type name of the QuantConnect.Brokerages.Paper.PaperBrokerage
        private const string PaperBrokerageTypeName = "PaperBrokerage";
        
        /******************************************************** 
        * CLASS METHODS
        *********************************************************/

        /// <summary>
        /// Configurations settings, lean runmode.
        /// </summary>
        private bool BacktestingMode
        {
            get { return !Config.GetBool("live-mode"); }
        }

        /// <summary>
        /// Physical location of Algorithm DLL.
        /// </summary>
        private string AlgorithmLocation
        {
            get
            {
                // we expect this dll to be copied into the output directory
                return "QuantConnect.Algorithm.dll";
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Initialize the job queue:
        /// </summary>
        public void Initialize()
        {
            //
        }
        
        /// <summary>
        /// Desktop/Local Get Next Task - Get task from the Algorithm folder of VS Solution.
        /// </summary>
        /// <returns></returns>
        public AlgorithmNodePacket NextJob(out string location)
        {
            location = AlgorithmLocation;

            //If this isn't a backtesting mode/request, attempt a live job.
            if (!BacktestingMode)
            {
                var liveJob = new LiveNodePacket
                {
                    Type = PacketType.LiveNode,
                    DataEndpoint = DataFeedEndpoint.LiveTrading,
                    RealTimeEndpoint = RealTimeEndpoint.LiveTrading,
                    ResultEndpoint = ResultHandlerEndpoint.LiveTrading,
                    SetupEndpoint = SetupHandlerEndpoint.Brokerage,
                    TransactionEndpoint = TransactionHandlerEndpoint.Brokerage,
                    Algorithm = File.ReadAllBytes(AlgorithmLocation),
                    Brokerage = Config.Get("live-mode-brokerage", PaperBrokerageTypeName),
                };
                
                // import the brokerage data for the configured brokerage
                liveJob.BrokerageData = Composer.Instance.GetExportWithMatchingMetadata<Dictionary<string, string>>("BrokerageData", liveJob.Brokerage);

                return liveJob;
            }

            //Default run a backtesting job.
            var backtestJob = new BacktestNodePacket(0, 0, "", new byte[] {}, 10000, "local")
            {
                Type = PacketType.BacktestNode,
                DataEndpoint = DataFeedEndpoint.FileSystem,
                SetupEndpoint = SetupHandlerEndpoint.Console,
                ResultEndpoint = ResultHandlerEndpoint.Console,
                RealTimeEndpoint = RealTimeEndpoint.Backtesting,
                TransactionEndpoint = TransactionHandlerEndpoint.Backtesting,
                Algorithm = File.ReadAllBytes(AlgorithmLocation)
            };
            return backtestJob;
        }

        /// <summary>
        /// Desktop/Local acknowledge the task processed. Nothing to do.
        /// </summary>
        /// <param name="job"></param>
        public void AcknowledgeJob(AlgorithmNodePacket job)
        {
            //
        }
    }

}
