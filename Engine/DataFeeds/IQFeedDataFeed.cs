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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// IQFeed DataFeed By 
    /// </summary>
    public class IQFeedDataFeed : IDataFeed
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        // Set types in public area to speed up:
        private IAlgorithm _algorithm;
        private BacktestNodePacket _job;
        private bool _endOfStreams = false;
        private int _subscriptions = 0;
        private int _bridgeMax = 500000;
        private bool _exitTriggered = false;

        private DateTime[] _mySQLBridgeTime;
        private DateTime _endTime;

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// List of the subscription the algorithm has requested. Subscriptions contain the type, sourcing information and manage the enumeration of data.
        /// </summary>
        public List<SubscriptionDataConfig> Subscriptions { get; private set; }

        /// <summary>
        /// Prices of the datafeed this instant for dynamically updating security values (and calculation of the total portfolio value in realtime).
        /// </summary>
        /// <remarks>Indexed in order of the subscriptions</remarks>
        public List<decimal> RealtimePrices { get; private set; }

        /// <summary>
        /// Cross-threading queues so the datafeed pushes data into the queue and the primary algorithm thread reads it out.
        /// </summary>
        public ConcurrentQueue<List<BaseData>>[] Bridge { get; set; }

        /// <summary>
        /// Stream created from the configuration settings.
        /// </summary>
        public SubscriptionDataReader[] SubscriptionReaderManagers { get; set; }

        /// <summary>
        /// Set the source of the data we're requesting for the type-readers to know where to get data from.
        /// </summary>
        /// <remarks>Live or Backtesting Datafeed</remarks>
        public DataFeedEndpoint DataFeed { get; set; }

        /// <summary>
        /// Flag indicating the hander thread is completely finished and ready to dispose.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Flag indicating the file system has loaded all files.
        /// </summary>
        public bool LoadingComplete { get; private set; }

        /// <summary>
        /// Furthest point in time that the data has loaded into the bridges.
        /// </summary>
        public DateTime LoadedDataFrontier { get; private set; }

        /// <summary>
        /// Signifying no more data across all bridges
        /// </summary>
        public bool EndOfBridges
        {
            get
            {
                for (var i = 0; i < Bridge.Length; i++)
                {
                    if (Bridge[i].Count != 0 || EndOfBridge[i] != true || _endOfStreams != true)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// End of Stream for Each Bridge:
        /// </summary>
        public bool[] EndOfBridge { get; set; }


        /******************************************************** 
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Create and connect to IQFeed.
        /// </summary>
        public IQFeedDataFeed(IAlgorithm algorithm, BacktestNodePacket job)
        {
            //Save the data subscriptions
            Subscriptions = algorithm.SubscriptionManager.Subscriptions;
            _subscriptions = Subscriptions.Count;

            //Public Properties:
            IsActive = true;
            DataFeed = DataFeedEndpoint.FileSystem;
            EndOfBridge = new bool[_subscriptions];
            Bridge = new ConcurrentQueue<List<BaseData>>[_subscriptions];
            SubscriptionReaderManagers = new SubscriptionDataReader[_subscriptions];
            RealtimePrices = new List<decimal>(_subscriptions);

            //Class Privates:
            _job = job;
            _algorithm = algorithm;
            _endOfStreams = false;
            _bridgeMax = _bridgeMax / _subscriptions; //Set the bridge maximum count:
            _endTime = job.PeriodFinish;

            //Initialize arrays:
            for (var i = 0; i < _subscriptions; i++)
            {
                _mySQLBridgeTime[i] = job.PeriodStart;
                EndOfBridge[i] = false;
                Bridge[i] = new ConcurrentQueue<List<BaseData>>();
                SubscriptionReaderManagers[i] = new SubscriptionDataReader(Subscriptions[i], algorithm.Securities[Subscriptions[i].Symbol], DataFeedEndpoint.Database, job.PeriodStart, job.PeriodFinish);
            }
        }

        /// <summary>
        /// Crude implementation of IQFeed data connection.
        /// </summary>
        public void Run()
        {
            while (!_exitTriggered && IsActive && !EndOfBridges)
            {
                
            } 

            IsActive = false;
        }


        /// <summary>
        /// Send an exit signal to the thread.
        /// </summary>
        public void Exit()
        {
            _exitTriggered = true;
            PurgeData();
        }


        /// <summary>
        /// Loop over all the queues and clear them to fast-quit this thread and return to main.
        /// </summary>
        public void PurgeData()
        {
            foreach (var t in Bridge)
            {
                t.Clear();
            }
        }

    }
}
