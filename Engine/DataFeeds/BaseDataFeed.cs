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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Common components of a data feed allowing the extender to implement only the parts which matter.
    /// </summary>
    public abstract class BaseDataFeed : IDataFeed
    {
        private bool _endOfStreams;
        private int _subscriptions;
        private int _bridgeMax = 500000;
        private bool _exitTriggered;

        private DateTime[] _frontierTime;

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
                    if (Bridge[i].Count != 0 || EndOfBridge[i] != true)
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

        /// <summary>
        /// Create an instance of the base datafeed.
        /// </summary>
        public virtual void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job)
        {
            //Save the data subscriptions
            Subscriptions = algorithm.SubscriptionManager.Subscriptions;
            _subscriptions = Subscriptions.Count;

            //Public Properties:
            DataFeed = DataFeedEndpoint.FileSystem;
            IsActive = true;
            Bridge = new ConcurrentQueue<List<BaseData>>[_subscriptions];
            EndOfBridge = new bool[_subscriptions];
            SubscriptionReaderManagers = new SubscriptionDataReader[_subscriptions];
            RealtimePrices = new List<decimal>(_subscriptions);
            _frontierTime = new DateTime[_subscriptions];

            //Class Privates:
            _endOfStreams = false;
            _bridgeMax = _bridgeMax / _subscriptions;

            //Initialize arrays:
            for (var i = 0; i < _subscriptions; i++)
            {
                _frontierTime[i] = algorithm.StartDate;
                EndOfBridge[i] = false;
                Bridge[i] = new ConcurrentQueue<List<BaseData>>();
                SubscriptionReaderManagers[i] = new SubscriptionDataReader(Subscriptions[i], algorithm.Securities[Subscriptions[i].Symbol], DataFeedEndpoint.Database, algorithm.StartDate, algorithm.EndDate);
            }
        }

        /// <summary>
        /// Launch the primary data thread.
        /// </summary>
        public virtual void Run()
        {
            while (!_exitTriggered && IsActive && !EndOfBridges)
            {
                for (var i = 0; i < Subscriptions.Count; i++)
                {
                    //With each subscription; fetch the next increment of data from the queues:
                    var subscription = Subscriptions[i];

                    if (Bridge[i].Count < 10000 && !EndOfBridge[i])
                    {
                        var data = GetData(subscription);

                        //Comment out for live databases, where we should continue asking even if no data.
                        if (data.Count == 0)
                        {
                            EndOfBridge[i] = true;
                            continue;
                        }

                        ////Insert data into bridge, each list is time-grouped. Assume all different time-groups.
                        foreach (var obj in data)
                        {
                            Bridge[i].Enqueue(new List<BaseData>() { obj });
                        }
                        
                        ////Record the furthest moment in time.
                        _frontierTime[i] = data.Max(bar => bar.Time);
                    }
                }
                //Set the most backward moment in time we've loaded
                LoadedDataFrontier = _frontierTime.Min();
            }

            IsActive = false;
        }

        /// <summary>
        /// Get the next set of data for this subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public abstract  List<BaseData> GetData(SubscriptionDataConfig subscription);

        /// <summary>
        /// Send an exit signal to the thread.
        /// </summary>
        public virtual void Exit()
        {
            _exitTriggered = true;
            PurgeData();
        }

        /// <summary>
        /// Loop over all the queues and clear them to fast-quit this thread and return to main.
        /// </summary>
        public virtual void PurgeData()
        {
            foreach (var t in Bridge)
            {
                t.Clear();
            }
        }
    }
}
