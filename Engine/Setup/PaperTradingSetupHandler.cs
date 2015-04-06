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

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Brokerages.Paper;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Papertrading setup handler processes the algorithm initialize method and sets up the internal state of the algorithm class.
    /// </summary>
    public class PaperTradingSetupHandler : ISetupHandler
    {
        /******************************************************** 
        * PRIVATE VARIABLES
        *********************************************************/

        /******************************************************** 
        * PUBLIC PROPERTIES
        *********************************************************/
        /// <summary>
        /// Internal errors list from running the setup proceedures.
        /// </summary>
        public List<string> Errors { get;  set; }

        /// <summary>
        /// Maximum runtime of the algorithm in seconds.
        /// </summary>
        /// <remarks>Maximum runtime is a formula based on the number and resolution of symbols requested, and the days backtesting</remarks>
        public TimeSpan MaximumRuntime { get; private set; }

        /// <summary>
        /// Starting capital according to the users initialize routine.
        /// </summary>
        /// <remarks>Set from the user code.</remarks>
        /// <seealso cref="QCAlgorithm.SetCash(decimal)"/>
        public decimal StartingCapital { get; private set; }

        /// <summary>
        /// Start date for analysis loops to search for data.
        /// </summary>
        /// <seealso cref="QCAlgorithm.SetStartDate(DateTime)"/>
        public DateTime StartingDate { get; private set; }

        /// <summary>
        /// Maximum number of orders for this live paper trading algorithm. (int.MaxValue)
        /// </summary>
        /// <remarks>For live trading its almost impossible to limit the order number</remarks>
        public int MaxOrders { get; private set; }

        /******************************************************** 
        * PUBLIC CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Setup the algorithm data, cash, job start end date etc:
        /// </summary>
        public PaperTradingSetupHandler()
        {
            MaxOrders = int.MaxValue;
            StartingDate = new DateTime(1998, 01, 01);
            StartingCapital = 0;
            MaximumRuntime = TimeSpan.FromDays(10 * 365);
            Errors = new List<string>();
        }

        /******************************************************** 
        * PUBLIC METHODS
        *********************************************************/
        /// <summary>
        /// Creates a new algorithm instance. Verified there's only one defined in the assembly and requires
        /// instantiation to take less than 10 seconds
        /// </summary>
        public IAlgorithm CreateAlgorithmInstance(string assemblyPath)
        {
            string error;
            IAlgorithm algorithm;

            // limit load times to 10 seconds and force the assembly to have exactly one derived type
            var loader = new Loader(TimeSpan.FromSeconds(10), names => names.SingleOrDefault());
            var complete = loader.TryCreateAlgorithmInstanceWithIsolator(assemblyPath, out algorithm, out error);
            if (!complete) throw new Exception(error + " Try re-building algorithm.");

            return algorithm;
        }

        /// <summary>
        /// Setup the algorithm cash, dates and portfolio as desired.
        /// </summary>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="brokerage">Output new instance of the brokerage</param>
        /// <param name="job">Algorithm job/task we're running</param>
        /// <returns>Bool setup success</returns>
        public bool Setup(IAlgorithm algorithm, out IBrokerage brokerage, AlgorithmNodePacket job)
        {
            var initializeComplete = false;
            var liveJob = job as LiveNodePacket; 
            brokerage = new PaperBrokerage(algorithm);

            try
            {
                //Algorithm is live, not backtesting:
                algorithm.SetLiveMode(true);
                
                //Set the live trading level asset/ram allocation limits. 
                //Protects algorithm from linux killing the job by excess memory:
                switch (job.ServerType)
                {
                    case ServerType.Server1024:
                        algorithm.SetAssetLimits(100, 20, 10);
                        break;

                    case ServerType.Server2048:
                        algorithm.SetAssetLimits(400, 50, 30);
                        break;

                    default: //512
                        algorithm.SetAssetLimits(50, 10, 5);
                        break;
                }

                //Initialize the algorithm
                algorithm.Initialize();

                //Try and use the live job packet cash if exists, otherwise resort to the user algo cash:
                if (liveJob != null && liveJob.BrokerageData.ContainsKey("project-paper-equity"))
                {
                    var consistentCash = Convert.ToDecimal(liveJob.BrokerageData["project-paper-equity"]);
                    algorithm.SetCash(consistentCash);
                }
            }
            catch (Exception err)
            {
                Log.Error("PaperTradingSetupHandler.Setup(): " + err.Message);
                Errors.Add("Error setting up the paper trading algorithm; " + err.Message);
            }

            // Starting capital is portfolio cash:
            StartingCapital = algorithm.Portfolio.Cash;

            if (Errors.Count == 0)
            {
                initializeComplete = true;
            }
            return initializeComplete;
        }

        /// <summary>
        /// Setup error handlers.
        /// </summary>
        /// <param name="results">Result handler instance</param>
        /// <param name="brokerage">Brokerage instance</param>
        /// <returns></returns>
        public bool SetupErrorHandler(IResultHandler results, IBrokerage brokerage)
        {
            return true;
        }

    } // End Result Handler Thread:

} // End Namespace
