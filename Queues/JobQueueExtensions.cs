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
using System.IO;
using System.Linq;
using Fasterflect;
using QuantConnect.Util;
using QuantConnect.Data;
using System.Reflection;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using System.Collections.Generic;

namespace QuantConnect.Queues
{
    /// <summary>
    /// 
    /// </summary>
    public static class JobQueueExtensions
    {
        private const string DefaultDataQueueHandler = "LiveDataQueue";
        private const string PaperBrokerageTypeName = "PaperBrokerage";
        private const string DefaultHistoryProvider = "SubscriptionDataReaderHistoryProvider";
        private const string DefaultDataChannelProvider = "DataChannelProvider";
        private static readonly string Channel = Config.Get("data-channel");

        public static LiveNodePacket GetLiveNodeConfigurationWithAlgorithmConfiguration(string algorithmLocation, string algorithmId, Dictionary<string, string> algorithmParameters,
            Language algorithmLanguage)
        {
            var liveJob = GetLiveNodeConfigurations();

            liveJob.Algorithm = File.ReadAllBytes(algorithmLocation);
            liveJob.DeployId = algorithmId;
            liveJob.Parameters = algorithmParameters;
            liveJob.Language = algorithmLanguage;

            return liveJob;
        }

        public static BacktestNodePacket GetBacktestNodePacketConfiguration(string algorithmLocation, string algorithmId, Dictionary<string, string> algorithmParameters, Language algorithmLanguage)
        {
            var backtestingJob = new BacktestNodePacket(GetAlgorithmNodePacketConfigurations(PacketType.BacktestNode));

            backtestingJob.UserId = 0;
            backtestingJob.ProjectId = 0;
            backtestingJob.SessionId = "";
            backtestingJob.Algorithm = new byte[] { };
            backtestingJob.Name = Config.Get("backtest-name", "local");

            backtestingJob.Algorithm = File.ReadAllBytes(algorithmLocation);
            backtestingJob.BacktestId = algorithmId;
            backtestingJob.Parameters = algorithmParameters;
            backtestingJob.Language = algorithmLanguage;

            return backtestingJob;
        }

        public static LiveNodePacket GetLiveNodeConfigurations()
        {
            var dataHandlers = Config.Get("data-queue-handler", DefaultDataQueueHandler);

            var liveJob = new LiveNodePacket(GetAlgorithmNodePacketConfigurations(PacketType.LiveNode));

            liveJob.Brokerage = Config.Get("live-mode-brokerage", PaperBrokerageTypeName);
            liveJob.DataQueueHandler = dataHandlers;
            liveJob.DataChannelProvider = Config.Get("data-channel-provider", DefaultDataChannelProvider);

            Type brokerageName = null;
            try
            {
                // import the brokerage data for the configured brokerage
                var brokerageFactory = Composer.Instance.Single<IBrokerageFactory>(factory => factory.BrokerageType.MatchesTypeName(liveJob.Brokerage));
                brokerageName = brokerageFactory.BrokerageType;
                liveJob.BrokerageData = brokerageFactory.BrokerageData;
            }
            catch (Exception err)
            {
                Log.Error(err, $"Error resolving BrokerageData for live job for brokerage {liveJob.Brokerage}");
            }

            var brokerageBasedHistoryProvider = liveJob.HistoryProvider.DeserializeList().Select(x =>
            {
                HistoryExtensions.TryGetBrokerageName(x, out var brokerageName);
                return brokerageName;
            }).Where(x => x != null);

            foreach (var dataHandlerName in dataHandlers.DeserializeList().Concat(brokerageBasedHistoryProvider).Distinct())
            {
                var brokerageFactoryForDataHandler = GetFactoryFromDataQueueHandler(dataHandlerName);
                if (brokerageFactoryForDataHandler == null)
                {
                    Log.Trace($"JobQueue.NextJob(): Not able to fetch brokerage factory with name: {dataHandlerName}");
                    continue;
                }
                if (brokerageFactoryForDataHandler.BrokerageType == brokerageName)
                {
                    //Don't need to add brokearageData again if added by brokerage
                    continue;
                }
                foreach (var data in brokerageFactoryForDataHandler.BrokerageData)
                {
                    if (data.Key == "live-holdings" || data.Key == "live-cash-balance")
                    {
                        //live holdings & cash balance not required for data handler
                        continue;
                    }

                    liveJob.BrokerageData.TryAdd(data.Key, data.Value);
                }
            }
            return liveJob;
        }

        /// <summary>
        /// Gets Brokerage Factory for provided IDQH
        /// </summary>
        /// <param name="dataQueueHandler"></param>
        /// <returns>An Instance of Brokerage Factory if possible, otherwise null</returns>
        public static IBrokerageFactory GetFactoryFromDataQueueHandler(string dataQueueHandler)
        {
            IBrokerageFactory brokerageFactory = null;
            var dataQueueHandlerType = Composer.Instance.GetExportedTypes<IBrokerage>()
                .FirstOrDefault(x =>
                    x.FullName != null &&
                    x.FullName.EndsWith(dataQueueHandler, StringComparison.InvariantCultureIgnoreCase) &&
                    x.HasAttribute(typeof(BrokerageFactoryAttribute)));

            if (dataQueueHandlerType != null)
            {
                var attribute = dataQueueHandlerType.GetCustomAttribute<BrokerageFactoryAttribute>();
                brokerageFactory = (BrokerageFactory)Activator.CreateInstance(attribute.Type);
            }
            return brokerageFactory;
        }

        private static AlgorithmNodePacket GetAlgorithmNodePacketConfigurations(PacketType packetType)
        {
            var controls = new Controls()
            {
                MinuteLimit = Config.GetInt("symbol-minute-limit", 10000),
                SecondLimit = Config.GetInt("symbol-second-limit", 10000),
                TickLimit = Config.GetInt("symbol-tick-limit", 10000),
                RamAllocation = int.MaxValue,
                MaximumDataPointsPerChartSeries = Config.GetInt("maximum-data-points-per-chart-series", 1000000),
                MaximumChartSeries = Config.GetInt("maximum-chart-series", 30),
                StorageLimit = Config.GetValue("storage-limit", 10737418240L),
                StorageFileCount = Config.GetInt("storage-file-count", 10000),
                StoragePermissions = (FileAccess)Config.GetInt("storage-permissions", (int)FileAccess.ReadWrite)
            };

            return new AlgorithmNodePacket(packetType)
            {
                HistoryProvider = Config.Get("history-provider", DefaultHistoryProvider),
                Channel = Channel,
                UserToken = Globals.UserToken,
                UserId = Globals.UserId,
                ProjectId = Globals.ProjectId,
                OrganizationId = Globals.OrganizationID,
                Version = Globals.Version,
                Controls = controls,
                PythonVirtualEnvironment = Config.Get("python-venv"),
                DeploymentTarget = DeploymentTarget.LocalPlatform,
            };
        }
    }
}
