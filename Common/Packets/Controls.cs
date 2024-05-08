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

using System.IO;
using Newtonsoft.Json;
using QuantConnect.Interfaces;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Specifies values used to control algorithm limits
    /// </summary>
    public class Controls
    {
        /// <summary>
        /// The maximum runtime in minutes
        /// </summary>
        public int MaximumRuntimeMinutes;

        /// <summary>
        /// The maximum number of minute symbols
        /// </summary>
        public int MinuteLimit;

        /// <summary>
        /// The maximum number of second symbols
        /// </summary>
        public int SecondLimit;

        /// <summary>
        /// The maximum number of tick symbol
        /// </summary>
        public int TickLimit;

        /// <summary>
        /// Ram allocation for this algorithm in MB
        /// </summary>
        public int RamAllocation;

        /// <summary>
        /// CPU allocation for this algorithm
        /// </summary>
        public decimal CpuAllocation;

        /// <summary>
        /// The user live log limit
        /// </summary>
        public int LiveLogLimit;

        /// <summary>
        /// The user backtesting log limit
        /// </summary>
        public int BacktestLogLimit;

        /// <summary>
        /// The daily log limit of a user
        /// </summary>
        public int DailyLogLimit;

        /// <summary>
        /// The remaining log allowance for a user
        /// </summary>
        public int RemainingLogAllowance;

        /// <summary>
        /// Maximimum number of insights we'll store and score in a single backtest
        /// </summary>
        public int BacktestingMaxInsights;

        /// <summary>
        /// Maximimum number of orders we'll allow in a backtest.
        /// </summary>
        public int BacktestingMaxOrders { get; set; }

        /// <summary>
        /// Limits the amount of data points per chart series. Applies only for backtesting
        /// </summary>
        public int MaximumDataPointsPerChartSeries;

        /// <summary>
        /// Limits the amount of chart series. Applies only for backtesting
        /// </summary>
        public int MaximumChartSeries;

        /// <summary>
        /// The amount seconds used for timeout limits
        /// </summary>
        public int SecondTimeOut;

        /// <summary>
        /// Sets parameters used for determining the behavior of the leaky bucket algorithm that
        /// controls how much time is available for an algorithm to use the training feature.
        /// </summary>
        public LeakyBucketControlParameters TrainingLimits;

        /// <summary>
        /// Limits the total size of storage used by <see cref="IObjectStore"/>
        /// </summary>
        public long StorageLimit;

        /// <summary>
        /// Limits the number of files to be held under the <see cref="IObjectStore"/>
        /// </summary>
        public int StorageFileCount;

        /// <summary>
        /// Holds the permissions for the object store
        /// </summary>
        public FileAccess StoragePermissions;

        /// <summary>
        /// The interval over which the <see cref="IObjectStore"/> will persistence the contents of
        /// the object store
        /// </summary>
        public int PersistenceIntervalSeconds;

        /// <summary>
        /// The cost associated with running this job
        /// </summary>
        public decimal CreditCost;

        /// <summary>
        /// Initializes a new default instance of the <see cref="Controls"/> class
        /// </summary>
        public Controls()
        {
            MinuteLimit = 500;
            SecondLimit = 100;
            TickLimit = 30;
            RamAllocation = 1024;
            BacktestLogLimit = 10000;
            BacktestingMaxOrders = int.MaxValue;
            DailyLogLimit = 3000000;
            RemainingLogAllowance = 10000;
            MaximumRuntimeMinutes = 60 * 24 * 100; // 100 days default
            BacktestingMaxInsights = 10000;
            MaximumChartSeries = 10;
            MaximumDataPointsPerChartSeries = 4000;
            SecondTimeOut = 300;
            StorageLimit = 10737418240;
            StorageFileCount = 10000;
            PersistenceIntervalSeconds = 5;
            StoragePermissions = FileAccess.ReadWrite;

            // initialize to default leaky bucket values in case they're not specified
            TrainingLimits = new LeakyBucketControlParameters();
        }
    }
}
