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

using NodaTime;
using QuantConnect.Benchmarks;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Python.Runtime;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// Wrapper for an IAlgorithm instance created in Python.
    /// All calls to python should be inside a "using (Py.GIL()) {/* Your code here */}" block.
    /// </summary>
    public class AlgorithmPythonWrapper : IAlgorithm
    {
        IAlgorithm _algorithm;
        IBenchmark _benchmark;
        IBrokerageModel _brokerageModel;
        IHistoryProvider _historyProvider;

        public AlgorithmPythonWrapper(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        public string AlgorithmId
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.AlgorithmId;
                }
            }
        }

        public IBenchmark Benchmark
        {
            get
            {
                using (Py.GIL())
                {
                    if (_benchmark == null)
                    {
                        _benchmark = new BenchmarkPythonWrapper(_algorithm.Benchmark);
                    }
                    return _benchmark;
                }
            }
        }

        public IBrokerageMessageHandler BrokerageMessageHandler
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.BrokerageMessageHandler;
                }
            }

            set
            {
                SetBrokerageMessageHandler(value);
            }
        }

        public IBrokerageModel BrokerageModel
        {
            get
            {
                using (Py.GIL())
                {
                    if (_brokerageModel == null)
                    {
                        _brokerageModel = new BrokerageModelPythonWrapper(_algorithm.BrokerageModel);
                    }
                    return _brokerageModel;
                }
            }
        }

        public ConcurrentQueue<string> DebugMessages
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.DebugMessages;
                }
            }
        }

        public DateTime EndDate
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.EndDate;
                }
            }
        }

        public ConcurrentQueue<string> ErrorMessages
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.ErrorMessages;
                }
            }
        }

        public IHistoryProvider HistoryProvider
        {
            get
            {
                using (Py.GIL())
                {
                    if (_historyProvider == null)
                    {
                        _historyProvider = new HistoryProviderPythonWrapper(_algorithm.HistoryProvider);
                    }
                    return _historyProvider;
                }
            }

            set
            {
                SetHistoryProvider(value);
            }
        }

        public bool IsWarmingUp
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.IsWarmingUp;
                }
            }
        }

        public bool LiveMode
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.LiveMode;
                }
            }
        }

        public ConcurrentQueue<string> LogMessages
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.LogMessages;
                }
            }
        }

        public string Name
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.Name;
                }
            }
        }

        public NotificationManager Notify
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.Notify;
                }
            }
        }

        public SecurityPortfolioManager Portfolio
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.Portfolio;
                }
            }
        }

        public Exception RunTimeError
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.RunTimeError;
                }
            }

            set
            {
                SetRunTimeError(value);
            }
        }

        public Dictionary<string, string> RuntimeStatistics
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.RuntimeStatistics;
                }
            }
        }

        public ScheduleManager Schedule
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.Schedule;
                }
            }
        }

        public SecurityManager Securities
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.Securities;
                }
            }
        }

        public ISecurityInitializer SecurityInitializer
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.SecurityInitializer;
                }
            }
        }

        public DateTime StartDate
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.StartDate;
                }
            }
        }

        public AlgorithmStatus Status
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.Status;
                }
            }

            set
            {
                SetStatus(value);
            }
        }

        public void SetStatus(AlgorithmStatus value)
        {
            using (Py.GIL())
            {
                _algorithm.SetStatus(value);
            }
        }

        public SubscriptionManager SubscriptionManager
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.SubscriptionManager;
                }
            }
        }

        public DateTime Time
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.Time;
                }
            }
        }

        public DateTimeZone TimeZone
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.TimeZone;
                }
            }
        }

        public TradeBuilder TradeBuilder
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.TradeBuilder;
                }
            }
        }

        public SecurityTransactionManager Transactions
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.Transactions;
                }
            }
        }

        public UniverseManager UniverseManager
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.UniverseManager;
                }
            }
        }

        public UniverseSettings UniverseSettings
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.UniverseSettings;
                }
            }
        }

        public DateTime UtcTime
        {
            get
            {
                using (Py.GIL())
                {
                    return _algorithm.UtcTime;
                }
            }
        }

        public Security AddSecurity(SecurityType securityType, string symbol, Resolution resolution, string market, bool fillDataForward, decimal leverage, bool extendedMarketHours)
        {
            using (Py.GIL())
            {
                return _algorithm.AddSecurity(securityType, symbol, resolution, market, fillDataForward, leverage, extendedMarketHours);
            }
        }

        public void Debug(string message)
        {
            using (Py.GIL())
            {
                _algorithm.Debug(message);
            }
        }

        public void Error(string message)
        {
            using (Py.GIL())
            {
                _algorithm.Error(message);
            }
        }

        public List<Chart> GetChartUpdates(bool clearChartData = false)
        {
            using (Py.GIL())
            {
                return _algorithm.GetChartUpdates(clearChartData);
            }
        }

        public bool GetLocked()
        {
            using (Py.GIL())
            {
                return _algorithm.GetLocked();
            }
        }

        public string GetParameter(string name)
        {
            using (Py.GIL())
            {
                return _algorithm.GetParameter(name);
            }
        }

        public IEnumerable<HistoryRequest> GetWarmupHistoryRequests()
        {
            using (Py.GIL())
            {
                return _algorithm.GetWarmupHistoryRequests().ToList();
            }
        }

        public void Initialize()
        {
            using (Py.GIL())
            {
                _algorithm.Initialize();
            }
        }

        public List<int> Liquidate(Symbol symbolToLiquidate = null)
        {
            using (Py.GIL())
            {
                return _algorithm.Liquidate(symbolToLiquidate);
            }
        }

        public void Log(string message)
        {
            using (Py.GIL())
            {
                _algorithm.Log(message);
            }
        }

        public void OnBrokerageDisconnect()
        {
            using (Py.GIL())
            {
                _algorithm.OnBrokerageDisconnect();
            }
        }

        public void OnBrokerageMessage(BrokerageMessageEvent messageEvent)
        {
            using (Py.GIL())
            {
                _algorithm.OnBrokerageMessage(messageEvent);
            }
        }

        public void OnBrokerageReconnect()
        {
            using (Py.GIL())
            {
                _algorithm.OnBrokerageReconnect();
            }
        }

        public void OnData(Slice slice)
        {
            using (Py.GIL())
            {
                _algorithm.OnData(slice);
            }
        }

        public void OnEndOfAlgorithm()
        {
            using (Py.GIL())
            {
                _algorithm.OnEndOfAlgorithm();
            }
        }

        public void OnEndOfDay()
        {
            using (Py.GIL())
            {
                _algorithm.OnEndOfDay();
            }
        }

        public void OnEndOfDay(Symbol symbol)
        {
            using (Py.GIL())
            {
                _algorithm.OnEndOfDay(symbol);
            }
        }

        public void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            using (Py.GIL())
            {
                _algorithm.OnMarginCall(requests);
            }
        }

        public void OnMarginCallWarning()
        {
            using (Py.GIL())
            {
                _algorithm.OnMarginCallWarning();
            }
        }

        public void OnOrderEvent(OrderEvent newEvent)
        {
            using (Py.GIL())
            {
                _algorithm.OnOrderEvent(newEvent);
            }
        }

        public void OnSecuritiesChanged(SecurityChanges changes)
        {
            using (Py.GIL())
            {
                _algorithm.OnSecuritiesChanged(changes);
            }
        }

        public void PostInitialize()
        {
            using (Py.GIL())
            {
                _algorithm.PostInitialize();
            }
        }

        public bool RemoveSecurity(Symbol symbol)
        {
            using (Py.GIL())
            {
                return _algorithm.RemoveSecurity(symbol);
            }
        }

        public void SetAlgorithmId(string algorithmId)
        {
            using (Py.GIL())
            {
                _algorithm.SetAlgorithmId(algorithmId);
            }
        }

        public void SetBrokerageMessageHandler(IBrokerageMessageHandler brokerageMessageHandler)
        {
            using (Py.GIL())
            {
                _algorithm.SetBrokerageMessageHandler(brokerageMessageHandler);
            }
        }

        public void SetBrokerageModel(IBrokerageModel brokerageModel)
        {
            using (Py.GIL())
            {
                _algorithm.SetBrokerageModel(new BrokerageModelPythonWrapper(brokerageModel));
            }
        }

        public void SetCash(decimal startingCash)
        {
            using (Py.GIL())
            {
                _algorithm.SetCash(startingCash);
            }
        }

        public void SetCash(string symbol, decimal startingCash, decimal conversionRate)
        {
            using (Py.GIL())
            {
                _algorithm.SetCash(symbol, startingCash, conversionRate);
            }
        }

        public void SetDateTime(DateTime time)
        {
            using (Py.GIL())
            {
                _algorithm.SetDateTime(time);
            }
        }

        public void SetRunTimeError(Exception exception)
        {
            using (Py.GIL())
            {
                _algorithm.SetRunTimeError(exception);
            }
        }

        public void SetFinishedWarmingUp()
        {
            using (Py.GIL())
            {
                _algorithm.SetFinishedWarmingUp();
            }
        }

        public void SetHistoryProvider(IHistoryProvider historyProvider)
        {
            using (Py.GIL())
            {
                _algorithm.SetHistoryProvider(new HistoryProviderPythonWrapper(historyProvider));
            }
        }

        public void SetLiveMode(bool live)
        {
            using (Py.GIL())
            {
                _algorithm.SetLiveMode(live);
            }
        }

        public void SetLocked()
        {
            using (Py.GIL())
            {
                _algorithm.SetLocked();
            }
        }

        public void SetMaximumOrders(int max)
        {
            using (Py.GIL())
            {
                _algorithm.SetMaximumOrders(max);
            }
        }

        public void SetParameters(Dictionary<string, string> parameters)
        {
            using (Py.GIL())
            {
                _algorithm.SetParameters(parameters);
            }
        }
    }
}