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
using System.Data;
using System.Linq;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using MySql.Data.MySqlClient;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// ODBC Database Feed Connection
    /// </summary>
    public class DatabaseDataFeed : IDataFeed
    {
        // Set types in public area to speed up:
        private IAlgorithm _algorithm;
        private BacktestNodePacket _job;
        private bool _endOfStreams = false;
        private int _subscriptions = 0;
        private int _bridgeMax = 500000;
        private bool _exitTriggered = false;
        private MySqlConnection _connection;
        private string _table = Config.Get("database-table");

        private DateTime[] _mySQLBridgeTime;
        private DateTime _endTime;

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

        /// <summary>
        /// Flag for when we're connected:
        /// </summary>
        private bool Connected
        {
            get
            {
                if (_connection == null) return false;
                if (ConnectionState == ConnectionState.Open) return true;
                return false;
            }
        }

        /// <summary>
        /// Current State of the DB Connection.
        /// </summary>
        private ConnectionState ConnectionState
        {
            get { return _connection.State; }
        }

        /// <summary>
        /// Prepare and create the new MySQL Database connection datafeed.
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="job"></param>
        public DatabaseDataFeed(IAlgorithm algorithm, BacktestNodePacket job)
        {
            //Save the data subscriptions
            Subscriptions = algorithm.SubscriptionManager.Subscriptions;
            _subscriptions = Subscriptions.Count;

            //Public Properties:
            IsActive = true;
            DataFeed = DataFeedEndpoint.FileSystem;
            Bridge = new ConcurrentQueue<List<BaseData>>[_subscriptions];
            EndOfBridge = new bool[_subscriptions];
            SubscriptionReaderManagers = new SubscriptionDataReader[_subscriptions];
            RealtimePrices = new List<decimal>(_subscriptions);
            _mySQLBridgeTime = new DateTime[_subscriptions];

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
        /// Crude implementation to connect and pull required data from MYSQL. 
        /// This is not efficient at all but just seeks to provide 0.1 draft for others to build from.
        /// </summary>
        /// <remarks>
        ///     Currently the MYSQL datafeed doesn't support fillforward but will just feed the data from dBase into algorithm.
        /// </remarks>
        public void Run()
        {
            //Initialize MYSQL Connection:
            Connect();
            
            while (!_exitTriggered && IsActive && !EndOfBridges)
            {
                for (var i = 0; i < Subscriptions.Count; i++)
                {
                    //With each subscription; fetch the next increment of data from the queues:
                    var subscription = Subscriptions[i];

                    if (Bridge[i].Count < 10000 && !EndOfBridge[i])
                    {
                        ////Fetch our data from mysql
                        var data = Query("SELECT * FROM equity_" + subscription.Symbol + " WHERE time >= '" + _mySQLBridgeTime[i].ToString("u") + "' AND time <= '" + _endTime.ToString("u") + "' ORDER BY time ASC LIMIT 100");

                        //Comment out for live databases, where we should continue asking even if no data.
                        if (data.Count == 0)
                        {
                            EndOfBridge[i] = true;
                            _endOfStreams = true; // No more data, stop
                            continue;
                        } 

                        var bars = GenerateBars(subscription.Symbol, data);

                        ////Insert data into bridge, each list is time-grouped. Assume all different time-groups.
                        foreach (var bar in bars)
                        {
                            Bridge[i].Enqueue(new List<BaseData>() { bar });
                        }
                        
                        ////Record the furthest moment in time.
                        _mySQLBridgeTime[i] = bars.Max(bar => bar.Time);
                    }
                }
                //Set the most backward moment in time we've loaded
                LoadedDataFrontier = _mySQLBridgeTime.Min();
            }

            _connection.Close();
            IsActive = false;
        }

        /// <summary>
        /// Generate a list of TradeBars from the database query result.
        /// </summary>
        /// <param name="symbol">string - the symbol for which to Generate a list of TradeBars</param>
        /// <param name="data">A list of TradeBars for the symbol</param>
        /// <returns></returns>
        private List<TradeBar> GenerateBars(string symbol, IEnumerable<Dictionary<string, string>> data)
        {
            var bars = new List<TradeBar>();
            foreach (var dictionary in data)
            {
                var bar = new TradeBar()
                {
                    Time = DateTime.Parse(dictionary["time"]).AddHours(15.9), //Closing time roughly 4pm
                    DataType = MarketDataType.TradeBar,
                    Open = decimal.Parse(dictionary["open"]),
                    High = decimal.Parse(dictionary["high"]),
                    Low = decimal.Parse(dictionary["low"]),
                    Close = decimal.Parse(dictionary["close"]),
                    Symbol = symbol,
                    Value = decimal.Parse(dictionary["close"]),
                    Volume = 0
                };
                bars.Add(bar);
            }
            return bars;
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

        /// <summary>
        /// Connect to the database: if we're running locally connect to local host.
        /// </summary>
        private void Connect()
        {
            //Default to local for ease:
            var serverAddress = Config.Get("database-address");
            var databaseName = Config.Get("database-name");
            var userId = Config.Get("database-user");
            var password = Config.Get("database-password");
            var connectionString = "";

            try
            {
                //Create Connection String:
                connectionString = GetConnectionString(serverAddress, databaseName, userId, password);

                //Set and Open Connection:
                _connection = new MySqlConnection(connectionString);
                _connection.StateChange += ConnectionOnStateChange;
                _connection.Open();

                //Place a pause here if want to display graph,
                while (_connection.State != ConnectionState.Open) { Thread.Sleep(100); };
            }
            catch (Exception err)
            {
                Log.Error("DatabaseDataFeed.Connect(): " + connectionString + " | " + err.Message);
            }
        }

        /// <summary>
        /// Handler for state change:
        /// </summary>
        private void ConnectionOnStateChange(object sender, StateChangeEventArgs e)
        {
            if (e.CurrentState == ConnectionState.Broken || e.CurrentState == ConnectionState.Closed)
            {
                Connect();
            }
        }

        /// <summary>
        /// Generate a connection string to connect to the database:
        /// </summary>
        /// <returns>string completed for connection</returns>
        private string GetConnectionString(string serverAddress, string databaseName, string userId, string password)
        {
            return "Server=" + serverAddress + ";" +
                    "Database=" + databaseName + ";" +
                    "User ID=" + userId + ";" +
                    "Password=" + password + ";" +
                    "Pooling=false";
        }

        /// <summary>
        /// Send a SQL COmmand to the server:
        /// </summary>
        /// <returns>List of Key Arrays </returns>
        public List<Dictionary<string, string>> Query(string sql)
        {
            var results = new List<Dictionary<string, string>>();
            try
            {
                //Start the connection.
                if (!Connected) Connect();

                //Create the Command
                var dbCommand = _connection.CreateCommand();
                dbCommand.CommandText = sql;

                //Send Command, Start Reading:
                var reader = dbCommand.ExecuteReader();

                //Read out each row:
                while (reader.Read())
                {
                    //Initialise One Row:
                    var row = new Dictionary<string, string>();
                    //Get each column of the row.
                    for (var field = 0; field < reader.FieldCount; field++)
                    {
                        row.Add(reader.GetName(field), reader[reader.GetName(field)].ToString());
                    }
                    //Add the row to results:
                    results.Add(row);
                }

                // Clean Up variables:
                reader.Close();
                dbCommand.Dispose();

                //Clean up memory usage for garbage collector:
                reader = null;
                dbCommand = null;
            }
            catch (Exception err)
            {
                Log.Error("DatabaseDataFeed.Query(): " + err.Message + " SQL Length:" + sql.Length + " SQL: " + sql.Substring(0, 100));
            }
            return results;
        }

    }
}
