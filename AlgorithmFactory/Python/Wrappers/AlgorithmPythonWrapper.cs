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
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Benchmarks;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace QuantConnect.AlgorithmFactory.Python.Wrappers
{
    /// <summary>
    /// Wrapper for an IAlgorithm instance created in Python.
    /// All calls to python should be inside a "using (Py.GIL()) {/* Your code here */}" block.
    /// </summary>
    public class AlgorithmPythonWrapper : IAlgorithm
    {
        private readonly PyObject _util;
        private readonly dynamic _algorithm;
        private readonly QCAlgorithm _baseAlgorithm;

        /// <summary>
        /// <see cref = "AlgorithmPythonWrapper"/> constructor.
        /// Creates and wraps the algorithm written in python.  
        /// </summary>
        /// <param name="module">Python module with the algorithm written in Python</param>
        public AlgorithmPythonWrapper(PyObject module)
        {
            _algorithm = null;

            try
            {
                using (Py.GIL())
                {
                    if (!module.HasAttr("QCAlgorithm"))
                    {
                        return;
                    }

                    var baseClass = module.GetAttr("QCAlgorithm");

                    // Load module with util methods
                    _util = ImportUtil();

                    var moduleName = module.Repr().Split('\'')[1];

                    foreach (var name in module.Dir())
                    {
                        var attr = module.GetAttr(name.ToString());

                        if (attr.IsSubclass(baseClass) && attr.Repr().Contains(moduleName))
                        {
                            attr.SetAttr("OnPythonData", _util.GetAttr("OnPythonData"));

                            _algorithm = attr.Invoke();

                            // QCAlgorithm reference for LEAN internal C# calls (without going from C# to Python and back)
                            _baseAlgorithm = (QCAlgorithm)_algorithm;

                            // Set pandas
                            _baseAlgorithm.SetPandas();

                            return; 
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Log.Error(e);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.AlgorithmId" /> in Python
        /// </summary>
        public string AlgorithmId
        {
            get
            {
                return _baseAlgorithm.AlgorithmId;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Benchmark" /> in Python
        /// </summary>
        public IBenchmark Benchmark
        {
            get
            {
                return _baseAlgorithm.Benchmark;
            }
        }

        /// <summary>
        /// Wrapper for <see cref="IAlgorithm.BrokerageMessageHandler" /> in Python
        /// </summary>
        public IBrokerageMessageHandler BrokerageMessageHandler
        {
            get
            {
                return _baseAlgorithm.BrokerageMessageHandler;
            }

            set
            {
                SetBrokerageMessageHandler(value);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.BrokerageModel" /> in Python
        /// </summary>
        public IBrokerageModel BrokerageModel
        {
            get
            {
                return _baseAlgorithm.BrokerageModel;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.DebugMessages" /> in Python
        /// </summary>
        public ConcurrentQueue<string> DebugMessages
        {
            get
            {
                return _baseAlgorithm.DebugMessages;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.EndDate" /> in Python
        /// </summary>
        public DateTime EndDate
        {
            get
            {
                return _baseAlgorithm.EndDate;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.ErrorMessages" /> in Python
        /// </summary>
        public ConcurrentQueue<string> ErrorMessages
        {
            get
            {
                return _baseAlgorithm.ErrorMessages;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.HistoryProvider" /> in Python
        /// </summary>
        public IHistoryProvider HistoryProvider
        {
            get
            {
                return _baseAlgorithm.HistoryProvider;
            }

            set
            {
                SetHistoryProvider(value);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.IsWarmingUp" /> in Python
        /// </summary>
        public bool IsWarmingUp
        {
            get
            {
                return _baseAlgorithm.IsWarmingUp;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.LiveMode" /> in Python
        /// </summary>
        public bool LiveMode
        {
            get
            {
                return _baseAlgorithm.LiveMode;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.LogMessages" /> in Python
        /// </summary>
        public ConcurrentQueue<string> LogMessages
        {
            get
            {
                return _baseAlgorithm.LogMessages;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Name" /> in Python
        /// </summary>
        public string Name
        {
            get
            {
                return _baseAlgorithm.Name;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Notify" /> in Python
        /// </summary>
        public NotificationManager Notify
        {
            get
            {
                return _baseAlgorithm.Notify;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Portfolio" /> in Python
        /// </summary>
        public SecurityPortfolioManager Portfolio
        {
            get
            {
                return _baseAlgorithm.Portfolio;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.RunTimeError" /> in Python
        /// </summary>
        public Exception RunTimeError
        {
            get
            {
                return _baseAlgorithm.RunTimeError;
            }

            set
            {
                SetRunTimeError(value);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.RuntimeStatistics" /> in Python
        /// </summary>
        public ConcurrentDictionary<string, string> RuntimeStatistics
        {
            get
            {
                return _baseAlgorithm.RuntimeStatistics;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Schedule" /> in Python
        /// </summary>
        public ScheduleManager Schedule
        {
            get
            {
                return _baseAlgorithm.Schedule;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Securities" /> in Python
        /// </summary>
        public SecurityManager Securities
        {
            get
            {
                return _baseAlgorithm.Securities;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SecurityInitializer" /> in Python
        /// </summary>
        public ISecurityInitializer SecurityInitializer
        {
            get
            {
                return _baseAlgorithm.SecurityInitializer;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.TradeBuilder" /> in Python
        /// </summary>
        public ITradeBuilder TradeBuilder
        {
            get
            {
                return _baseAlgorithm.TradeBuilder;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Settings" /> in Python
        /// </summary>
        public AlgorithmSettings Settings
        {
            get
            {
                return _baseAlgorithm.Settings;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.StartDate" /> in Python
        /// </summary>
        public DateTime StartDate
        {
            get
            {
                return _baseAlgorithm.StartDate;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Status" /> in Python
        /// </summary>
        public AlgorithmStatus Status
        {
            get
            {
                return _baseAlgorithm.Status;
            }

            set
            {
                SetStatus(value);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetStatus" /> in Python
        /// </summary>
        /// <param name="value"></param>
        public void SetStatus(AlgorithmStatus value)
        {
            _baseAlgorithm.SetStatus(value);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetAvailableDataTypes" /> in Python
        /// </summary>
        /// <param name="availableDataTypes"></param>
        public void SetAvailableDataTypes(Dictionary<SecurityType, List<TickType>> availableDataTypes)
        {
            _baseAlgorithm.SetAvailableDataTypes(availableDataTypes);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetOptionChainProvider" /> in Python
        /// </summary>
        /// <param name="optionChainProvider"></param>
        public void SetOptionChainProvider(IOptionChainProvider optionChainProvider)
        {
            _baseAlgorithm.SetOptionChainProvider(optionChainProvider);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SubscriptionManager" /> in Python
        /// </summary>
        public SubscriptionManager SubscriptionManager
        {
            get
            {
                return _baseAlgorithm.SubscriptionManager;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Time" /> in Python
        /// </summary>
        public DateTime Time
        {
            get
            {
                return _baseAlgorithm.Time;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.TimeZone" /> in Python
        /// </summary>
        public DateTimeZone TimeZone
        {
            get
            {
                return _baseAlgorithm.TimeZone;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Transactions" /> in Python
        /// </summary>
        public SecurityTransactionManager Transactions
        {
            get
            {
                return _baseAlgorithm.Transactions;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.UniverseManager" /> in Python
        /// </summary>
        public UniverseManager UniverseManager
        {
            get
            {
                return _baseAlgorithm.UniverseManager;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.UniverseSettings" /> in Python
        /// </summary>
        public UniverseSettings UniverseSettings
        {
            get
            {
                return _baseAlgorithm.UniverseSettings;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.UtcTime" /> in Python
        /// </summary>
        public DateTime UtcTime
        {
            get
            {
                return _baseAlgorithm.UtcTime;
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.AddSecurity" /> in Python
        /// </summary>
        /// <param name="securityType"></param>
        /// <param name="symbol"></param>
        /// <param name="resolution"></param>
        /// <param name="market"></param>
        /// <param name="fillDataForward"></param>
        /// <param name="leverage"></param>
        /// <param name="extendedMarketHours"></param>
        /// <returns></returns>
        public Security AddSecurity(SecurityType securityType, string symbol, Resolution resolution, string market, bool fillDataForward, decimal leverage, bool extendedMarketHours)
        {
            return _baseAlgorithm.AddSecurity(securityType, symbol, resolution, market, fillDataForward, leverage, extendedMarketHours);
        }

        /// <summary>
        /// Creates and adds a new single <see cref="Future"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The futures contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Future"/> security</returns>
        public Future AddFutureContract(Symbol symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true, decimal leverage = 0m)
        {
            return _baseAlgorithm.AddFutureContract(symbol, resolution, fillDataForward, leverage);
        }

        /// <summary>
        /// Creates and adds a new single <see cref="Option"/> contract to the algorithm
        /// </summary>
        /// <param name="symbol">The option contract symbol</param>
        /// <param name="resolution">The <see cref="Resolution"/> of market data, Tick, Second, Minute, Hour, or Daily. Default is <see cref="Resolution.Minute"/></param>
        /// <param name="fillDataForward">If true, returns the last available data even if none in that timeslice. Default is <value>true</value></param>
        /// <param name="leverage">The requested leverage for this equity. Default is set by <see cref="SecurityInitializer"/></param>
        /// <returns>The new <see cref="Option"/> security</returns>
        public Option AddOptionContract(Symbol symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true, decimal leverage = 0m)
        {
            return _baseAlgorithm.AddOptionContract(symbol, resolution, fillDataForward, leverage);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Debug" /> in Python
        /// </summary>
        /// <param name="message"></param>
        public void Debug(string message)
        {
            _baseAlgorithm.Debug(message);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Error" /> in Python
        /// </summary>
        /// <param name="message"></param>
        public void Error(string message)
        {
            _baseAlgorithm.Error(message);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.GetChartUpdates" /> in Python
        /// </summary>
        /// <param name="clearChartData"></param>
        /// <returns></returns>
        public List<Chart> GetChartUpdates(bool clearChartData = false)
        {
            return _baseAlgorithm.GetChartUpdates(clearChartData);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.GetLocked" /> in Python
        /// </summary>
        /// <returns></returns>
        public bool GetLocked()
        {
            return _baseAlgorithm.GetLocked();
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.GetParameter" /> in Python
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetParameter(string name)
        {
            return _baseAlgorithm.GetParameter(name);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.GetWarmupHistoryRequests" /> in Python
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HistoryRequest> GetWarmupHistoryRequests()
        {
            return _baseAlgorithm.GetWarmupHistoryRequests();
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Initialize" /> in Python
        /// </summary>
        public void Initialize()
        {
            using (Py.GIL())
            {
                _algorithm.Initialize();
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Liquidate" /> in Python
        /// </summary>
        /// <param name="symbolToLiquidate"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<int> Liquidate(Symbol symbolToLiquidate = null, string tag = "Liquidated")
        {
            return _baseAlgorithm.Liquidate(symbolToLiquidate, tag);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.Log" /> in Python
        /// </summary>
        /// <param name="message"></param>
        public void Log(string message)
        {
            _baseAlgorithm.Log(message);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnBrokerageDisconnect" /> in Python
        /// </summary>
        public void OnBrokerageDisconnect()
        {
            using (Py.GIL())
            {
                _algorithm.OnBrokerageDisconnect();
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnBrokerageMessage" /> in Python
        /// </summary>
        /// <param name="messageEvent"></param>
        public void OnBrokerageMessage(BrokerageMessageEvent messageEvent)
        {
            using (Py.GIL())
            {
                _algorithm.OnBrokerageMessage(messageEvent);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnBrokerageReconnect" /> in Python
        /// </summary>
        public void OnBrokerageReconnect()
        {
            using (Py.GIL())
            {
                _algorithm.OnBrokerageReconnect();
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnData" /> in Python
        /// </summary>
        public void OnData(Slice slice)
        {
            using (Py.GIL())
            {
                if (SubscriptionManager.HasCustomData)
                {
                    _algorithm.OnPythonData(slice);
                }
                else
                {
                    _algorithm.OnData(slice);
                }
            }
        }
        
        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnEndOfAlgorithm" /> in Python
        /// </summary>
        public void OnEndOfAlgorithm()
        {
            using (Py.GIL())
            {
                _algorithm.OnEndOfAlgorithm();
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnEndOfDay()" /> in Python
        /// </summary>
        public void OnEndOfDay()
        {
            using (Py.GIL())
            {
                _algorithm.OnEndOfDay();
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnEndOfDay(Symbol)" /> in Python
        /// </summary>
        /// <param name="symbol"></param>
        public void OnEndOfDay(Symbol symbol)
        {
            using (Py.GIL())
            {
                _algorithm.OnEndOfDay(symbol);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnMarginCall" /> in Python
        /// </summary>
        /// <param name="requests"></param>
        public void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            try
            {
                using (Py.GIL())
                {
                    _algorithm.OnMarginCall(requests);
                }
            }
            catch (PythonException pythonException)
            {
                // Pythonnet generated error due to List conversion 
                if (pythonException.Message.Equals("TypeError : No method matches given arguments"))
                {
                    _baseAlgorithm.OnMarginCall(requests);
                }
                // User code generated error
                else
                {
                    throw pythonException;
                }
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnMarginCallWarning" /> in Python
        /// </summary>
        public void OnMarginCallWarning()
        {
            using (Py.GIL())
            {
                _algorithm.OnMarginCallWarning();
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnOrderEvent" /> in Python
        /// </summary>
        /// <param name="newEvent"></param>
        public void OnOrderEvent(OrderEvent newEvent)
        {
            using (Py.GIL())
            {
                _algorithm.OnOrderEvent(newEvent);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnAssignmentOrderEvent" /> in Python
        /// </summary>
        /// <param name="newEvent"></param>
        public void OnAssignmentOrderEvent(OrderEvent newEvent)
        {
            using (Py.GIL())
            {
                _algorithm.OnAssignmentOrderEvent(newEvent);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.OnSecuritiesChanged" /> in Python
        /// </summary>
        /// <param name="changes"></param>
        public void OnSecuritiesChanged(SecurityChanges changes)
        {
            using (Py.GIL())
            {
                _algorithm.OnSecuritiesChanged(changes);
            }
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.PostInitialize" /> in Python
        /// </summary>
        public void PostInitialize()
        {
            _baseAlgorithm.PostInitialize();
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.RemoveSecurity" /> in Python
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public bool RemoveSecurity(Symbol symbol)
        {
            return _baseAlgorithm.RemoveSecurity(symbol);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetAlgorithmId" /> in Python
        /// </summary>
        /// <param name="algorithmId"></param>
        public void SetAlgorithmId(string algorithmId)
        {
            _baseAlgorithm.SetAlgorithmId(algorithmId);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetBrokerageMessageHandler" /> in Python
        /// </summary>
        /// <param name="brokerageMessageHandler"></param>
        public void SetBrokerageMessageHandler(IBrokerageMessageHandler brokerageMessageHandler)
        {
            _baseAlgorithm.SetBrokerageMessageHandler(brokerageMessageHandler);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetBrokerageModel" /> in Python
        /// </summary>
        /// <param name="brokerageModel"></param>
        public void SetBrokerageModel(IBrokerageModel brokerageModel)
        {
            _baseAlgorithm.SetBrokerageModel(brokerageModel);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetCash(decimal)" /> in Python
        /// </summary>
        /// <param name="startingCash"></param>
        public void SetCash(decimal startingCash)
        {
            _baseAlgorithm.SetCash(startingCash);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetCash(string, decimal, decimal)" /> in Python
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="startingCash"></param>
        /// <param name="conversionRate"></param>
        public void SetCash(string symbol, decimal startingCash, decimal conversionRate)
        {
            _baseAlgorithm.SetCash(symbol, startingCash, conversionRate);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetDateTime" /> in Python
        /// </summary>
        /// <param name="time"></param>
        public void SetDateTime(DateTime time)
        {
            _baseAlgorithm.SetDateTime(time);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetRunTimeError" /> in Python
        /// </summary>
        /// <param name="exception"></param>
        public void SetRunTimeError(Exception exception)
        {
            _baseAlgorithm.SetRunTimeError(exception);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetFinishedWarmingUp" /> in Python
        /// </summary>
        public void SetFinishedWarmingUp()
        {
            _baseAlgorithm.SetFinishedWarmingUp();
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetHistoryProvider" /> in Python
        /// </summary>
        /// <param name="historyProvider"></param>
        public void SetHistoryProvider(IHistoryProvider historyProvider)
        {
            _baseAlgorithm.SetHistoryProvider(historyProvider);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetLiveMode" /> in Python
        /// </summary>
        /// <param name="live"></param>
        public void SetLiveMode(bool live)
        {
            _baseAlgorithm.SetLiveMode(live);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetLocked" /> in Python
        /// </summary>
        public void SetLocked()
        {
            _baseAlgorithm.SetLocked();
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetMaximumOrders" /> in Python
        /// </summary>
        /// <param name="max"></param>
        public void SetMaximumOrders(int max)
        {
            _baseAlgorithm.SetMaximumOrders(max);
        }

        /// <summary>
        /// Wrapper for <see cref = "IAlgorithm.SetParameters" /> in Python
        /// </summary>
        /// <param name="parameters"></param>
        public void SetParameters(Dictionary<string, string> parameters)
        {
            _baseAlgorithm.SetParameters(parameters);
        }

        /// <summary>
        /// Creates Util module
        /// </summary>
        /// <returns>PyObject with utils</returns>
        private PyObject ImportUtil()
        {
            var code =
                "from clr import AddReference\n" +
                "AddReference(\"System\")\n" +
                "AddReference(\"QuantConnect.Common\")\n" +
                "import decimal\n" +

                // OnPythonData call OnData after converting the Slice object
                "def OnPythonData(self, data):\n" +
                "    self.OnData(PythonSlice(data))\n" +

                // PythonSlice class 
                "class PythonSlice(dict):\n" +
                "    def __init__(self, slice):\n" +
                "        for data in slice:\n" +
                "            self[data.Key] = Data(data.Value)\n" +
                "            self[data.Key.Value] = Data(data.Value)\n" +

                // Python Data class: Converts custom data (PythonData) into a python object'''
                "class Data(object):\n" +
                "    def __init__(self, data):\n" +
                "        members = [attr for attr in dir(data) if not callable(attr) and not attr.startswith(\"__\")]\n" +
                "        for member in members:\n" +
                "            setattr(self, member, getattr(data, member))\n" +

                "        if not hasattr(data, 'GetStorageDictionary'): return\n" +

                "        for kvp in data.GetStorageDictionary():\n" +
                "           name = kvp.Key.replace('-',' ').replace('.',' ').title().replace(' ', '')\n" +
                "           value = decimal.Decimal(kvp.Value) if isinstance(kvp.Value, float) else kvp.Value\n" +
                "           setattr(self, name, value)";

            using (Py.GIL())
            {
                return PythonEngine.ModuleFromString("AlgorithmPythonUtil", code);
            }
        }

        /// <summary>
        /// Returns a <see cref = "string"/> that represents the current <see cref = "AlgorithmPythonWrapper"/> object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _algorithm == null ? base.ToString() : _algorithm.Repr();
        }
    }
}
