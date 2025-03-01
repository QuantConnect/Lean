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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NodaTime;
using ProtoBuf;
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Python;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Util;
using Timer = System.Timers.Timer;
using Microsoft.IO;
using NodaTime.TimeZones;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Exceptions;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;
using QuantConnect.Securities.Option;
using QuantConnect.Statistics;
using Newtonsoft.Json.Linq;
using QuantConnect.Orders.Fees;

namespace QuantConnect
{
    /// <summary>
    /// Extensions function collections - group all static extensions functions here.
    /// </summary>
    public static class Extensions
    {
        private static readonly Dictionary<string, bool> _emptyDirectories = new ();
        private static readonly HashSet<string> InvalidSecurityTypes = new HashSet<string>();
        private static readonly Regex DateCheck = new Regex(@"\d{8}", RegexOptions.Compiled);
        private static RecyclableMemoryStreamManager MemoryManager = new RecyclableMemoryStreamManager();
        private static readonly int DataUpdatePeriod = Config.GetInt("downloader-data-update-period", 7);

        private static readonly Dictionary<IntPtr, PythonActivator> PythonActivators
            = new Dictionary<IntPtr, PythonActivator>();

        /// <summary>
        /// Maintains old behavior of NodaTime's (&lt; 2.0) daylight savings mapping.
        /// We keep the old behavior to ensure the FillForwardEnumerator does not get stuck on an infinite loop.
        /// The test `ConvertToSkipsDiscontinuitiesBecauseOfDaylightSavingsStart_AddingOneHour` and other related tests
        /// assert the expected behavior, which is to ignore discontinuities in daylight savings resolving.
        ///
        /// More info can be found in the summary of the <see cref="Resolvers.LenientResolver"/> delegate.
        /// </summary>
        private static readonly ZoneLocalMappingResolver _mappingResolver = Resolvers.CreateMappingResolver(Resolvers.ReturnLater, Resolvers.ReturnStartOfIntervalAfter);

        /// <summary>
        /// The offset span from the market close to liquidate or exercise a security on the delisting date
        /// </summary>
        /// <remarks>Will no be used in live trading</remarks>
        /// <remarks>By default span is negative 15 minutes. We want to liquidate before market closes if not, in some cases
        /// like future options the market close would match the delisted event time and would cancel all orders and mark the security
        /// as non tradable and delisted.</remarks>
        public static TimeSpan DelistingMarketCloseOffsetSpan { get; set; } = TimeSpan.FromMinutes(-15);

        /// <summary>
        /// Helper method to get a property in a jobject if available
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="jObject">The jobject source</param>
        /// <param name="name">The property name</param>
        /// <returns>The property value if present or it's default value</returns>
        public static T TryGetPropertyValue<T>(this JObject jObject, string name)
        {
            T result = default;
            if (jObject == null)
            {
                return result;
            }

            var jValue = jObject[name];
            if (jValue != null && jValue.Type != JTokenType.Null)
            {
                result = jValue.Value<T>();
            }
            return result;
        }

        /// <summary>
        /// Determine if the file is out of date according to our download period.
        /// Date based files are never out of date (Files with YYYYMMDD)
        /// </summary>
        /// <param name="filepath">Path to the file</param>
        /// <returns>True if the file is out of date</returns>
        public static bool IsOutOfDate(this string filepath)
        {
            var fileName = Path.GetFileName(filepath);
            // helper to determine if file is date based using regex, matches a 8 digit value because we expect YYYYMMDD
            return !DateCheck.IsMatch(fileName) && DateTime.Now - TimeSpan.FromDays(DataUpdatePeriod) > File.GetLastWriteTime(filepath);
        }

        /// <summary>
        /// Helper method to check if a directory exists and is not empty
        /// </summary>
        /// <param name="directoryPath">The path to check</param>
        /// <returns>True if the directory does not exist or is empty</returns>
        /// <remarks>Will cache results</remarks>
        public static bool IsDirectoryEmpty(this string directoryPath)
        {
            lock (_emptyDirectories)
            {
                if(!_emptyDirectories.TryGetValue(directoryPath, out var result))
                {
                    // is empty unless it exists and it has at least 1 file or directory in it
                    result = true;
                    if (Directory.Exists(directoryPath))
                    {
                        try
                        {
                            result = !Directory.EnumerateFileSystemEntries(directoryPath).Any();
                        }
                        catch (Exception exception)
                        {
                            Log.Error(exception);
                        }
                    }

                    _emptyDirectories[directoryPath] = result;
                    if (result)
                    {
                        Log.Trace($"Extensions.IsDirectoryEmpty(): directory '{directoryPath}' not found or empty");
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Helper method to get a market hours entry
        /// </summary>
        /// <param name="marketHoursDatabase">The market hours data base instance</param>
        /// <param name="symbol">The symbol to get the entry for</param>
        /// <param name="dataTypes">For custom data types can optionally provide data type so that a new entry is added</param>
        public static MarketHoursDatabase.Entry GetEntry(this MarketHoursDatabase marketHoursDatabase, Symbol symbol, IEnumerable<Type> dataTypes)
        {
            if (symbol.SecurityType == SecurityType.Base)
            {
                if (!marketHoursDatabase.TryGetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType, out var entry))
                {
                    var type = dataTypes.Single();
                    var baseInstance = type.GetBaseDataInstance();
                    baseInstance.Symbol = symbol;
                    SecurityIdentifier.TryGetCustomDataType(symbol.ID.Symbol, out var customType);
                    // for custom types we will add an entry for that type
                    entry = marketHoursDatabase.SetEntryAlwaysOpen(symbol.ID.Market, customType != null ? $"TYPE.{customType}" : null, SecurityType.Base, baseInstance.DataTimeZone());
                }
                return entry;
            }

            var result = marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType);

            // For the OptionUniverse type, the exchange and data time zones are set to the same value (exchange tz).
            // This is not actual options data, just option chains/universe selection, so we don't want any offsets
            // between the exchange and data time zones.
            // If the MHDB were data type dependent as well, this would be taken care in there.
            if (result != null && dataTypes.Any(dataType => dataType == typeof(OptionUniverse)))
            {
                result = new MarketHoursDatabase.Entry(result.ExchangeHours.TimeZone, result.ExchangeHours);
            }

            return result;
        }

        /// <summary>
        /// Helper method to deserialize a json array into a list also handling single json values
        /// </summary>
        /// <param name="jsonArray">The value to deserialize</param>
        public static List<string> DeserializeList(this string jsonArray)
        {
            return DeserializeList<string>(jsonArray);
        }

        /// <summary>
        /// Helper method to deserialize a json array into a list also handling single json values
        /// </summary>
        /// <param name="jsonArray">The value to deserialize</param>
        public static List<T> DeserializeList<T>(this string jsonArray)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonArray))
                {
                    return new();
                }
                return JsonConvert.DeserializeObject<List<T>>(jsonArray);
            }
            catch (Exception ex)
            {
                if (ex is not JsonReaderException && ex is not JsonSerializationException)
                {
                    throw;
                }

                if (typeof(T) == typeof(string))
                {
                    return new List<T> { (T)Convert.ChangeType(jsonArray, typeof(T), CultureInfo.InvariantCulture) };
                }
                return new List<T> { JsonConvert.DeserializeObject<T>(jsonArray) };
            }
        }

        /// <summary>
        /// Helper method to download a provided url as a string
        /// </summary>
        /// <param name="client">The http client to use</param>
        /// <param name="url">The url to download data from</param>
        /// <param name="headers">Add custom headers for the request</param>
        public static string DownloadData(this HttpClient client, string url, Dictionary<string, string> headers = null)
        {
            if (headers != null)
            {
                foreach (var kvp in headers)
                {
                    client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);
                }
            }
            try
            {
                using (var response = client.GetAsync(url).Result)
                {
                    using (var content = response.Content)
                    {
                        return content.ReadAsStringAsync().Result;
                    }
                }
            }
            catch (WebException ex)
            {
                Log.Error(ex, $"DownloadData(): {Messages.Extensions.DownloadDataFailed(url)}");
                return null;
            }
        }

        /// <summary>
        /// Helper method to download a provided url as a string
        /// </summary>
        /// <param name="url">The url to download data from</param>
        /// <param name="headers">Add custom headers for the request</param>
        public static string DownloadData(this string url, Dictionary<string, string> headers = null)
        {
            using var client = new HttpClient();
            return client.DownloadData(url, headers);
        }

        /// <summary>
        /// Helper method to download a provided url as a byte array
        /// </summary>
        /// <param name="url">The url to download data from</param>
        public static byte[] DownloadByteArray(this string url)
        {
            using (var wc = new HttpClient())
            {
                try
                {
                    return wc.GetByteArrayAsync(url).Result;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"DownloadByteArray(): {Messages.Extensions.DownloadDataFailed(url)}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Safe multiplies a decimal by 100
        /// </summary>
        /// <param name="value">The decimal to multiply</param>
        /// <returns>The result, maxed out at decimal.MaxValue</returns>
        public static decimal SafeMultiply100(this decimal value)
        {
            const decimal max = decimal.MaxValue / 100m;
            if (value >= max) return decimal.MaxValue;
            return value * 100m;
        }

        /// <summary>
        /// Will return a memory stream using the <see cref="RecyclableMemoryStreamManager"/> instance.
        /// </summary>
        /// <param name="guid">Unique guid</param>
        /// <returns>A memory stream</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemoryStream GetMemoryStream(Guid guid)
        {
            return MemoryManager.GetStream(guid);
        }

        /// <summary>
        /// Serialize a list of ticks using protobuf
        /// </summary>
        /// <param name="ticks">The list of ticks to serialize</param>
        /// <param name="guid">Unique guid</param>
        /// <returns>The resulting byte array</returns>
        public static byte[] ProtobufSerialize(this List<Tick> ticks, Guid guid)
        {
            byte[] result;
            using (var stream = GetMemoryStream(guid))
            {
                Serializer.Serialize(stream, ticks);
                result = stream.ToArray();
            }
            return result;
        }

        /// <summary>
        /// Serialize a base data instance using protobuf
        /// </summary>
        /// <param name="baseData">The data point to serialize</param>
        /// <param name="guid">Unique guid</param>
        /// <returns>The resulting byte array</returns>
        public static byte[] ProtobufSerialize(this IBaseData baseData, Guid guid)
        {
            byte[] result;
            using (var stream = GetMemoryStream(guid))
            {
                baseData.ProtobufSerialize(stream);
                result = stream.ToArray();
            }

            return result;
        }

        /// <summary>
        /// Serialize a base data instance using protobuf
        /// </summary>
        /// <param name="baseData">The data point to serialize</param>
        /// <param name="stream">The destination stream</param>
        public static void ProtobufSerialize(this IBaseData baseData, Stream stream)
        {
            switch (baseData.DataType)
            {
                case MarketDataType.Tick:
                    Serializer.SerializeWithLengthPrefix(stream, baseData as Tick, PrefixStyle.Base128, 1);
                    break;
                case MarketDataType.QuoteBar:
                    Serializer.SerializeWithLengthPrefix(stream, baseData as QuoteBar, PrefixStyle.Base128, 1);
                    break;
                case MarketDataType.TradeBar:
                    Serializer.SerializeWithLengthPrefix(stream, baseData as TradeBar, PrefixStyle.Base128, 1);
                    break;
                default:
                    Serializer.SerializeWithLengthPrefix(stream, baseData as BaseData, PrefixStyle.Base128, 1);
                    break;
            }
        }

        /// <summary>
        /// Extension method to get security price is 0 messages for users
        /// </summary>
        /// <remarks>The value of this method is normalization</remarks>
        public static string GetZeroPriceMessage(this Symbol symbol)
        {
            return Messages.Extensions.ZeroPriceForSecurity(symbol);
        }

        /// <summary>
        /// Converts the provided string into camel case notation
        /// </summary>
        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (value.Length == 1)
            {
                return value.ToLowerInvariant();
            }
            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        /// <summary>
        /// Helper method to batch a collection of <see cref="AlphaResultPacket"/> into 1 single instance.
        /// Will return null if the provided list is empty. Will keep the last Order instance per order id,
        /// which is the latest. Implementations trusts the provided 'resultPackets' list to batch is in order
        /// </summary>
        public static AlphaResultPacket Batch(this List<AlphaResultPacket> resultPackets)
        {
            AlphaResultPacket resultPacket = null;

            // batch result packets into a single packet
            if (resultPackets.Count > 0)
            {
                // we will batch results into the first packet
                resultPacket = resultPackets[0];
                for (var i = 1; i < resultPackets.Count; i++)
                {
                    var newerPacket = resultPackets[i];

                    // only batch current packet if there actually is data
                    if (newerPacket.Insights != null)
                    {
                        if (resultPacket.Insights == null)
                        {
                            // initialize the collection if it isn't there
                            resultPacket.Insights = new List<Insight>();
                        }
                        resultPacket.Insights.AddRange(newerPacket.Insights);
                    }

                    // only batch current packet if there actually is data
                    if (newerPacket.OrderEvents != null)
                    {
                        if (resultPacket.OrderEvents == null)
                        {
                            // initialize the collection if it isn't there
                            resultPacket.OrderEvents = new List<OrderEvent>();
                        }
                        resultPacket.OrderEvents.AddRange(newerPacket.OrderEvents);
                    }

                    // only batch current packet if there actually is data
                    if (newerPacket.Orders != null)
                    {
                        if (resultPacket.Orders == null)
                        {
                            // initialize the collection if it isn't there
                            resultPacket.Orders = new List<Order>();
                        }
                        resultPacket.Orders.AddRange(newerPacket.Orders);

                        // GroupBy guarantees to respect original order, so we want to get the last order instance per order id
                        // this way we only keep the most updated version
                        resultPacket.Orders = resultPacket.Orders.GroupBy(order => order.Id)
                            .Select(ordersGroup => ordersGroup.Last()).ToList();
                    }
                }
            }
            return resultPacket;
        }

        /// <summary>
        /// Helper method to safely stop a running thread
        /// </summary>
        /// <param name="thread">The thread to stop</param>
        /// <param name="timeout">The timeout to wait till the thread ends after which abort will be called</param>
        /// <param name="token">Cancellation token source to use if any</param>
        public static void StopSafely(this Thread thread, TimeSpan timeout, CancellationTokenSource token = null)
        {
            if (thread != null)
            {
                try
                {
                    if (token != null && !token.IsCancellationRequested)
                    {
                        token.Cancel(false);
                    }
                    Log.Trace($"StopSafely(): {Messages.Extensions.WaitingForThreadToStopSafely(thread.Name)}");
                    // just in case we add a time out
                    if (!thread.Join(timeout))
                    {
                        Log.Error($"StopSafely(): {Messages.Extensions.TimeoutWaitingForThreadToStopSafely(thread.Name)}");
                    }
                }
                catch (Exception exception)
                {
                    // just in case catch any exceptions
                    Log.Error(exception);
                }
            }
        }

        /// <summary>
        /// Generates a hash code from a given collection of orders
        /// </summary>
        /// <param name="orders">The order collection</param>
        /// <returns>The hash value</returns>
        public static string GetHash(this IDictionary<int, Order> orders)
        {
            var joinedOrders = string.Join(
                ",",
                orders
                    .OrderBy(pair => pair.Key)
                    .Select(pair =>
                        {
                            // this is required to avoid any small differences between python and C#
                            var order = pair.Value;
                            order.Price = order.Price.SmartRounding();
                            var limit = order as LimitOrder;
                            if (limit != null)
                            {
                                limit.LimitPrice = limit.LimitPrice.SmartRounding();
                            }
                            var stopLimit = order as StopLimitOrder;
                            if (stopLimit != null)
                            {
                                stopLimit.LimitPrice = stopLimit.LimitPrice.SmartRounding();
                                stopLimit.StopPrice = stopLimit.StopPrice.SmartRounding();
                            }
                            var trailingStop = order as TrailingStopOrder;
                            if (trailingStop != null)
                            {
                                trailingStop.TrailingAmount = trailingStop.TrailingAmount.SmartRounding();
                            }
                            var stopMarket = order as StopMarketOrder;
                            if (stopMarket != null)
                            {
                                stopMarket.StopPrice = stopMarket.StopPrice.SmartRounding();
                            }
                            var limitIfTouched = order as LimitIfTouchedOrder;
                            if (limitIfTouched != null)
                            {
                                limitIfTouched.LimitPrice = limitIfTouched.LimitPrice.SmartRounding();
                                limitIfTouched.TriggerPrice = limitIfTouched.TriggerPrice.SmartRounding();
                            }
                            return JsonConvert.SerializeObject(pair.Value, Formatting.None);
                        }
                    )
            );

            return joinedOrders.ToMD5();
        }

        /// <summary>
        /// Converts a date rule into a function that receives current time
        /// and returns the next date.
        /// </summary>
        /// <param name="dateRule">The date rule to convert</param>
        /// <returns>A function that will enumerate the provided date rules</returns>
        public static Func<DateTime, DateTime?> ToFunc(this IDateRule dateRule)
        {
            IEnumerator<DateTime> dates = null;
            return timeUtc =>
            {
                if (dates == null)
                {
                    dates = dateRule.GetDates(timeUtc, Time.EndOfTime).GetEnumerator();
                    if (!dates.MoveNext())
                    {
                        return Time.EndOfTime;
                    }
                }

                try
                {
                    // only advance enumerator if provided time is past or at our current
                    if (timeUtc >= dates.Current)
                    {
                        if (!dates.MoveNext())
                        {
                            return Time.EndOfTime;
                        }
                    }
                    return dates.Current;
                }
                catch (InvalidOperationException)
                {
                    // enumeration ended
                    return Time.EndOfTime;
                }
            };
        }

        /// <summary>
        /// Returns true if the specified <see cref="BaseSeries"/> instance holds no <see cref="ISeriesPoint"/>
        /// </summary>
        public static bool IsEmpty(this BaseSeries series)
        {
            return series.Values.Count == 0;
        }

        /// <summary>
        /// Returns if the specified <see cref="Chart"/> instance holds no <see cref="Series"/>
        /// or they are all empty <see cref="Extensions.IsEmpty(BaseSeries)"/>
        /// </summary>
        public static bool IsEmpty(this Chart chart)
        {
            return chart.Series.Values.All(IsEmpty);
        }

        /// <summary>
        /// Gets a python method by name
        /// </summary>
        /// <param name="instance">The object instance to search the method in</param>
        /// <param name="name">The name of the method</param>
        /// <returns>The python method or null if not defined or CSharp implemented</returns>
        public static dynamic GetPythonMethod(this PyObject instance, string name)
        {
            using (Py.GIL())
            {
                PyObject method;

                // Let's try first with snake-case style in case the user is using it
                var snakeCasedNamed = name.ToSnakeCase();
                if (snakeCasedNamed != name)
                {
                    method = instance.GetPythonMethodWithChecks(snakeCasedNamed);
                    if (method != null)
                    {
                        return method;
                    }
                }

                method = instance.GetAttr(name);
                var pythonType = method.GetPythonType();
                var isPythonDefined = pythonType.Repr().Equals("<class \'method\'>", StringComparison.Ordinal);

                if (isPythonDefined)
                {
                    return method;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets a python property by name
        /// </summary>
        /// <param name="instance">The object instance to search the property in</param>
        /// <param name="name">The name of the property</param>
        /// <returns>The python property or null if not defined or CSharp implemented</returns>
        public static dynamic GetPythonBoolProperty(this PyObject instance, string name)
        {
            using (Py.GIL())
            {
                var objectType = instance.GetPythonType();
                if (!objectType.HasAttr(name))
                {
                    return null;
                }

                var property = instance.GetAttr(name);
                var pythonType = property.GetPythonType();
                var isPythonDefined = pythonType.Repr().Equals("<class \'bool\'>", StringComparison.Ordinal);

                if (isPythonDefined)
                {
                    return property;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets a python property by name
        /// </summary>
        /// <param name="instance">The object instance to search the property in</param>
        /// <param name="name">The name of the method</param>
        /// <returns>The python property or null if not defined or CSharp implemented</returns>
        public static dynamic GetPythonBoolPropertyWithChecks(this PyObject instance, string name)
        {
            using (Py.GIL())
            {
                if (!instance.HasAttr(name))
                {
                    return null;
                }

                return instance.GetPythonBoolProperty(name);
            }
        }

        /// <summary>
        /// Gets a python method by name
        /// </summary>
        /// <param name="instance">The object instance to search the method in</param>
        /// <param name="name">The name of the method</param>
        /// <returns>The python method or null if not defined or CSharp implemented</returns>
        public static dynamic GetPythonMethodWithChecks(this PyObject instance, string name)
        {
            using (Py.GIL())
            {
                if (!instance.HasAttr(name))
                {
                    return null;
                }

                return instance.GetPythonMethod(name);
            }
        }

        /// <summary>
        /// Gets a method from a <see cref="PyObject"/> instance by name.
        /// First, it tries to get the snake-case version of the method name, in case the user is using that style.
        /// Else, it tries to get the method with the original name, regardless of whether the class has a Python overload or not.
        /// </summary>
        /// <param name="instance">The object instance to search the method in</param>
        /// <param name="name">The name of the method</param>
        /// <returns>The method matching the name</returns>
        public static dynamic GetMethod(this PyObject instance, string name)
        {
            using var _ = Py.GIL();
            return instance.GetPythonMethodWithChecks(name.ToSnakeCase()) ?? instance.GetAttr(name);
        }

        /// <summary>
        /// Get a python methods arg count
        /// </summary>
        /// <param name="method">The Python method</param>
        /// <returns>Count of arguments</returns>
        public static int GetPythonArgCount(this PyObject method)
        {
            using (Py.GIL())
            {
                int argCount;
                var pyArgCount = PyModule.FromString(Guid.NewGuid().ToString(),
                    "from inspect import signature\n" +
                    "def GetArgCount(method):\n" +
                    "   return len(signature(method).parameters)\n"
                ).GetAttr("GetArgCount").Invoke(method);
                pyArgCount.TryConvert(out argCount);

                return argCount;
            }
        }

        /// <summary>
        /// Returns an ordered enumerable where position reducing orders are executed first
        /// and the remaining orders are executed in decreasing order value.
        /// Will NOT return targets during algorithm warmup.
        /// Will NOT return targets for securities that have no data yet.
        /// Will NOT return targets for which current holdings + open orders quantity, sum up to the target quantity
        /// </summary>
        /// <param name="targets">The portfolio targets to order by margin</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targetIsDelta">True if the target quantity is the delta between the
        /// desired and existing quantity</param>
        public static IEnumerable<IPortfolioTarget> OrderTargetsByMarginImpact(
            this IEnumerable<IPortfolioTarget> targets,
            IAlgorithm algorithm,
            bool targetIsDelta = false)
        {
            if (algorithm.IsWarmingUp)
            {
                return Enumerable.Empty<IPortfolioTarget>();
            }

            return targets.Select(x =>
                {
                    var security = algorithm.Securities[x.Symbol];
                    return new
                    {
                        PortfolioTarget = x,
                        TargetQuantity = OrderSizing.AdjustByLotSize(security, x.Quantity),
                        ExistingQuantity = security.Holdings.Quantity
                            + algorithm.Transactions.GetOpenOrderTickets(x.Symbol)
                                .Aggregate(0m, (d, t) => d + t.Quantity - t.QuantityFilled),
                        Security = security
                    };
                })
                .Where(x => x.Security.HasData
                            && x.Security.IsTradable
                            && (targetIsDelta ? Math.Abs(x.TargetQuantity) : Math.Abs(x.TargetQuantity - x.ExistingQuantity))
                            >= x.Security.SymbolProperties.LotSize
                )
                .Select(x => new {
                    x.PortfolioTarget,
                    OrderValue = Math.Abs((targetIsDelta ? x.TargetQuantity : (x.TargetQuantity - x.ExistingQuantity)) * x.Security.Price),
                    IsReducingPosition = x.ExistingQuantity != 0
                                         && Math.Abs((targetIsDelta ? (x.TargetQuantity + x.ExistingQuantity) : x.TargetQuantity)) < Math.Abs(x.ExistingQuantity)
                })
                .OrderByDescending(x => x.IsReducingPosition)
                .ThenByDescending(x => x.OrderValue)
                .Select(x => x.PortfolioTarget);
        }

        /// <summary>
        /// Given a type will create a new instance using the parameterless constructor
        /// and assert the type implements <see cref="BaseData"/>
        /// </summary>
        /// <remarks>One of the objectives of this method is to normalize the creation of the
        /// BaseData instances while reducing code duplication</remarks>
        public static BaseData GetBaseDataInstance(this Type type)
        {
            var objectActivator = ObjectActivator.GetActivator(type);
            if (objectActivator == null)
            {
                throw new ArgumentException(Messages.Extensions.DataTypeMissingParameterlessConstructor(type));
            }

            var instance = objectActivator.Invoke(new object[] { type });
            if(instance == null)
            {
                // shouldn't happen but just in case...
                throw new ArgumentException(Messages.Extensions.FailedToCreateInstanceOfType(type));
            }

            // we expect 'instance' to inherit BaseData in most cases so we use 'as' versus 'IsAssignableFrom'
            // since it is slightly cheaper
            var result = instance as BaseData;
            if (result == null)
            {
                throw new ArgumentException(Messages.Extensions.TypeIsNotBaseData(type));
            }
            return result;
        }

        /// <summary>
        /// Helper method that will cast the provided <see cref="PyObject"/>
        /// to a T type and dispose of it.
        /// </summary>
        /// <typeparam name="T">The target type</typeparam>
        /// <param name="instance">The <see cref="PyObject"/> instance to cast and dispose</param>
        /// <returns>The instance of type T. Will return default value if
        /// provided instance is null</returns>
        public static T GetAndDispose<T>(this PyObject instance)
        {
            if (instance == null)
            {
                return default(T);
            }
            var returnInstance = instance.As<T>();
            // will reduce ref count
            instance.Dispose();
            return returnInstance;
        }

        /// <summary>
        /// Extension to move one element from list from A to position B.
        /// </summary>
        /// <typeparam name="T">Type of list</typeparam>
        /// <param name="list">List we're operating on.</param>
        /// <param name="oldIndex">Index of variable we want to move.</param>
        /// <param name="newIndex">New location for the variable</param>
        public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
        {
            var oItem = list[oldIndex];
            list.RemoveAt(oldIndex);
            if (newIndex > oldIndex) newIndex--;
            list.Insert(newIndex, oItem);
        }

        /// <summary>
        /// Extension method to convert a string into a byte array
        /// </summary>
        /// <param name="str">String to convert to bytes.</param>
        /// <returns>Byte array</returns>
        public static byte[] GetBytes(this string str)
        {
            var bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Reads the entire content of a stream and returns it as a byte array.
        /// </summary>
        /// <param name="stream">Stream to read bytes from</param>
        /// <returns>The bytes read from the stream</returns>
        public static byte[] GetBytes(this Stream stream)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Extentsion method to clear all items from a thread safe queue
        /// </summary>
        /// <remarks>Small risk of race condition if a producer is adding to the list.</remarks>
        /// <typeparam name="T">Queue type</typeparam>
        /// <param name="queue">queue object</param>
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            T item;
            while (queue.TryDequeue(out item)) {
                // NOP
            }
        }

        /// <summary>
        /// Extension method to convert a byte array into a string.
        /// </summary>
        /// <param name="bytes">Byte array to convert.</param>
        /// <param name="encoding">The encoding to use for the conversion. Defaults to Encoding.ASCII</param>
        /// <returns>String from bytes.</returns>
        public static string GetString(this byte[] bytes, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.ASCII;

            return encoding.GetString(bytes);
        }

        /// <summary>
        /// Extension method to convert a string to a MD5 hash.
        /// </summary>
        /// <param name="str">String we want to MD5 encode.</param>
        /// <returns>MD5 hash of a string</returns>
        public static string ToMD5(this string str)
        {
            var builder = new StringBuilder(32);
            var data = MD5.HashData(Encoding.UTF8.GetBytes(str));
            for (var i = 0; i < 16; i++)
            {
                builder.Append(data[i].ToStringInvariant("x2"));
            }
            return builder.ToString();
        }

        /// <summary>
        /// Encrypt the token:time data to make our API hash.
        /// </summary>
        /// <param name="data">Data to be hashed by SHA256</param>
        /// <returns>Hashed string.</returns>
        public static string ToSHA256(this string data)
        {
            var hash = new StringBuilder(64);
            var crypto = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            for (var i = 0; i < 32; i++)
            {
                hash.Append(crypto[i].ToStringInvariant("x2"));
            }
            return hash.ToString();
        }

        /// <summary>
        /// Converts a long to an uppercase alpha numeric string
        /// </summary>
        public static string EncodeBase36(this ulong data)
        {
            var stack = new Stack<char>(15);
            while (data != 0)
            {
                var value = data % 36;
                var c = value < 10
                    ? (char)(value + '0')
                    : (char)(value - 10 + 'A');

                stack.Push(c);
                data /= 36;
            }
            return new string(stack.ToArray());
        }

        /// <summary>
        /// Converts an upper case alpha numeric string into a long
        /// </summary>
        public static ulong DecodeBase36(this string symbol)
        {
            var result = 0ul;
            var baseValue = 1ul;
            for (var i = symbol.Length - 1; i > -1; i--)
            {
                var c = symbol[i];

                // assumes alpha numeric upper case only strings
                var value = (uint)(c <= 57
                    ? c - '0'
                    : c - 'A' + 10);

                result += baseValue * value;
                baseValue *= 36;
            }

            return result;
        }

        /// <summary>
        /// Convert a string to Base64 Encoding
        /// </summary>
        /// <param name="text">Text to encode</param>
        /// <returns>Encoded result</returns>
        public static string EncodeBase64(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(textBytes);
        }

        /// <summary>
        /// Decode a Base64 Encoded string
        /// </summary>
        /// <param name="base64EncodedText">Text to decode</param>
        /// <returns>Decoded result</returns>
        public static string DecodeBase64(this string base64EncodedText)
        {
            if (string.IsNullOrEmpty(base64EncodedText))
            {
                return base64EncodedText;
            }

            byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedText);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /// <summary>
        /// Lazy string to upper implementation.
        /// Will first verify the string is not already upper and avoid
        /// the call to <see cref="string.ToUpperInvariant()"/> if possible.
        /// </summary>
        /// <param name="data">The string to upper</param>
        /// <returns>The upper string</returns>
        public static string LazyToUpper(this string data)
        {
            // for performance only call to upper if required
            var alreadyUpper = true;
            for (int i = 0; i < data.Length && alreadyUpper; i++)
            {
                alreadyUpper = char.IsUpper(data[i]);
            }
            return alreadyUpper ? data : data.ToUpperInvariant();
        }

        /// <summary>
        /// Lazy string to lower implementation.
        /// Will first verify the string is not already lower and avoid
        /// the call to <see cref="string.ToLowerInvariant()"/> if possible.
        /// </summary>
        /// <param name="data">The string to lower</param>
        /// <returns>The lower string</returns>
        public static string LazyToLower(this string data)
        {
            // for performance only call to lower if required
            var alreadyLower = true;
            for (int i = 0; i < data.Length && alreadyLower; i++)
            {
                alreadyLower = char.IsLower(data[i]);
            }
            return alreadyLower ? data : data.ToLowerInvariant();
        }

        /// <summary>
        /// Extension method to automatically set the update value to same as "add" value for TryAddUpdate.
        /// This makes the API similar for traditional and concurrent dictionaries.
        /// </summary>
        /// <typeparam name="K">Key type for dictionary</typeparam>
        /// <typeparam name="V">Value type for dictonary</typeparam>
        /// <param name="dictionary">Dictionary object we're operating on</param>
        /// <param name="key">Key we want to add or update.</param>
        /// <param name="value">Value we want to set.</param>
        public static void AddOrUpdate<K, V>(this ConcurrentDictionary<K, V> dictionary, K key, V value)
        {
            dictionary.AddOrUpdate(key, value, (oldkey, oldvalue) => value);
        }

        /// <summary>
        /// Extension method to automatically add/update lazy values in concurrent dictionary.
        /// </summary>
        /// <typeparam name="TKey">Key type for dictionary</typeparam>
        /// <typeparam name="TValue">Value type for dictonary</typeparam>
        /// <param name="dictionary">Dictionary object we're operating on</param>
        /// <param name="key">Key we want to add or update.</param>
        /// <param name="addValueFactory">The function used to generate a value for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on the key's existing value</param>
        public static TValue AddOrUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, Lazy<TValue>> dictionary, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            var result = dictionary.AddOrUpdate(key, new Lazy<TValue>(() => addValueFactory(key)), (key2, old) => new Lazy<TValue>(() => updateValueFactory(key2, old.Value)));
            return result.Value;
        }

        /// <summary>
        /// Adds the specified element to the collection with the specified key. If an entry does not exist for the
        /// specified key then one will be created.
        /// </summary>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TElement">The collection element type</typeparam>
        /// <typeparam name="TCollection">The collection type</typeparam>
        /// <param name="dictionary">The source dictionary to be added to</param>
        /// <param name="key">The key</param>
        /// <param name="element">The element to be added</param>
        public static void Add<TKey, TElement, TCollection>(this IDictionary<TKey, TCollection> dictionary, TKey key, TElement element)
            where TCollection : ICollection<TElement>, new()
        {
            TCollection list;
            if (!dictionary.TryGetValue(key, out list))
            {
                list = new TCollection();
                dictionary.Add(key, list);
            }
            list.Add(element);
        }

        /// <summary>
        /// Adds the specified element to the collection with the specified key. If an entry does not exist for the
        /// specified key then one will be created.
        /// </summary>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TElement">The collection element type</typeparam>
        /// <param name="dictionary">The source dictionary to be added to</param>
        /// <param name="key">The key</param>
        /// <param name="element">The element to be added</param>
        public static ImmutableDictionary<TKey, ImmutableHashSet<TElement>> Add<TKey, TElement>(
            this ImmutableDictionary<TKey, ImmutableHashSet<TElement>> dictionary,
            TKey key,
            TElement element
            )
        {
            ImmutableHashSet<TElement> set;
            if (!dictionary.TryGetValue(key, out set))
            {
                set = ImmutableHashSet<TElement>.Empty.Add(element);
                return dictionary.Add(key, set);
            }

            return dictionary.SetItem(key, set.Add(element));
        }

        /// <summary>
        /// Adds the specified element to the collection with the specified key. If an entry does not exist for the
        /// specified key then one will be created.
        /// </summary>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TElement">The collection element type</typeparam>
        /// <param name="dictionary">The source dictionary to be added to</param>
        /// <param name="key">The key</param>
        /// <param name="element">The element to be added</param>
        public static ImmutableSortedDictionary<TKey, ImmutableHashSet<TElement>> Add<TKey, TElement>(
            this ImmutableSortedDictionary<TKey, ImmutableHashSet<TElement>> dictionary,
            TKey key,
            TElement element
            )
        {
            ImmutableHashSet<TElement> set;
            if (!dictionary.TryGetValue(key, out set))
            {
                set = ImmutableHashSet<TElement>.Empty.Add(element);
                return dictionary.Add(key, set);
            }

            return dictionary.SetItem(key, set.Add(element));
        }

        /// <summary>
        /// Adds the specified Tick to the Ticks collection. If an entry does not exist for the specified key then one will be created.
        /// </summary>
        /// <param name="dictionary">The ticks dictionary</param>
        /// <param name="key">The symbol</param>
        /// <param name="tick">The tick to add</param>
        /// <remarks>For performance we implement this method based on <see cref="Add{TKey,TElement,TCollection}"/></remarks>
        public static void Add(this Ticks dictionary, Symbol key, Tick tick)
        {
            List<Tick> list;
            if (!dictionary.TryGetValue(key, out list))
            {
                dictionary[key] = list = new List<Tick>(1);
            }
            list.Add(tick);
        }

        /// <summary>
        /// Extension method to round a double value to a fixed number of significant figures instead of a fixed decimal places.
        /// </summary>
        /// <param name="d">Double we're rounding</param>
        /// <param name="digits">Number of significant figures</param>
        /// <returns>New double rounded to digits-significant figures</returns>
        public static decimal RoundToSignificantDigits(this decimal d, int digits)
        {
            if (d == 0) return 0;
            var scale = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double) Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
        }

        /// <summary>
        /// Converts a decimal into a rounded number ending with K (thousands), M (millions), B (billions), etc.
        /// </summary>
        /// <param name="number">Number to convert</param>
        /// <returns>Formatted number with figures written in shorthand form</returns>
        public static string ToFinancialFigures(this decimal number)
        {
            if (number < 1000)
            {
                return number.ToStringInvariant();
            }

            // Subtract by multiples of 5 to round down to nearest round number
            if (number < 10000)
            {
                return (number - 5m).ToString("#,.##", CultureInfo.InvariantCulture) + "K";
            }

            if (number < 100000)
            {
                return (number - 50m).ToString("#,.#", CultureInfo.InvariantCulture) + "K";
            }

            if (number < 1000000)
            {
                return (number - 500m).ToString("#,.", CultureInfo.InvariantCulture) + "K";
            }

            if (number < 10000000)
            {
                return (number - 5000m).ToString("#,,.##", CultureInfo.InvariantCulture) + "M";
            }

            if (number < 100000000)
            {
                return (number - 50000m).ToString("#,,.#", CultureInfo.InvariantCulture) + "M";
            }

            if (number < 1000000000)
            {
                return (number - 500000m).ToString("#,,.", CultureInfo.InvariantCulture) + "M";
            }

            return (number - 5000000m).ToString("#,,,.##", CultureInfo.InvariantCulture) + "B";
        }

        /// <summary>
        /// Discretizes the <paramref name="value"/> to a maximum precision specified by <paramref name="quanta"/>. Quanta
        /// can be an arbitrary positive number and represents the step size. Consider a quanta equal to 0.15 and rounding
        /// a value of 1.0. Valid values would be 0.9 (6 quanta) and 1.05 (7 quanta) which would be rounded up to 1.05.
        /// </summary>
        /// <param name="value">The value to be rounded by discretization</param>
        /// <param name="quanta">The maximum precision allowed by the value</param>
        /// <param name="mode">Specifies how to handle the rounding of half value, defaulting to away from zero.</param>
        /// <returns></returns>
        public static decimal DiscretelyRoundBy(this decimal value, decimal quanta, MidpointRounding mode = MidpointRounding.AwayFromZero)
        {
            if (quanta == 0m)
            {
                return value;
            }

            // away from zero is the 'common sense' rounding.
            // +0.5 rounded by 1 yields +1
            // -0.5 rounded by 1 yields -1
            var multiplicand = Math.Round(value / quanta, mode);
            return quanta * multiplicand;
        }

        /// <summary>
        /// Will truncate the provided decimal, without rounding, to 3 decimal places
        /// </summary>
        /// <param name="value">The value to truncate</param>
        /// <returns>New instance with just 3 decimal places</returns>
        public static decimal TruncateTo3DecimalPlaces(this decimal value)
        {
            // we will multiply by 1k bellow, if its bigger it will stack overflow
            if (value >= decimal.MaxValue / 1000
                || value <= decimal.MinValue / 1000
                || value == 0)
            {
                return value;
            }

            return Math.Truncate(1000 * value) / 1000;
        }

        /// <summary>
        /// Provides global smart rounding, numbers larger than 1000 will round to 4 decimal places,
        /// while numbers smaller will round to 7 significant digits
        /// </summary>
        public static decimal? SmartRounding(this decimal? input)
        {
            if (!input.HasValue)
            {
                return null;
            }
            return input.Value.SmartRounding();
        }

        /// <summary>
        /// Provides global smart rounding, numbers larger than 1000 will round to 4 decimal places,
        /// while numbers smaller will round to 7 significant digits
        /// </summary>
        public static decimal SmartRounding(this decimal input)
        {
            input = Normalize(input);

            // any larger numbers we still want some decimal places
            if (input > 1000)
            {
                return Math.Round(input, 4);
            }

            // this is good for forex and other small numbers
            return input.RoundToSignificantDigits(7).Normalize();
        }

        /// <summary>
        /// Provides global smart rounding to a shorter version
        /// </summary>
        public static decimal SmartRoundingShort(this decimal input)
        {
            input = Normalize(input);
            if (input <= 1)
            {
                // 0.99 > input
                return input;
            }
            else if (input <= 10)
            {
                // 1.01 to 9.99
                return Math.Round(input, 2);
            }
            else if (input <= 100)
            {
                // 99.9 to 10.1
                return Math.Round(input, 1);
            }
            // 100 to inf
            return Math.Truncate(input);
        }

        /// <summary>
        /// Casts the specified input value to a decimal while acknowledging the overflow conditions
        /// </summary>
        /// <param name="input">The value to be cast</param>
        /// <returns>The input value as a decimal, if the value is too large or to small to be represented
        /// as a decimal, then the closest decimal value will be returned</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal SafeDecimalCast(this double input)
        {
            if (input.IsNaNOrInfinity())
            {
                throw new ArgumentException(
                    Messages.Extensions.CannotCastNonFiniteFloatingPointValueToDecimal(input),
                    nameof(input),
                    new NotFiniteNumberException(input)
                );
            }

            if (input <= (double) decimal.MinValue) return decimal.MinValue;
            if (input >= (double) decimal.MaxValue) return decimal.MaxValue;
            return (decimal) input;
        }

        /// <summary>
        /// Will remove any trailing zeros for the provided decimal input
        /// </summary>
        /// <param name="input">The <see cref="decimal"/> to remove trailing zeros from</param>
        /// <returns>Provided input with no trailing zeros</returns>
        /// <remarks>Will not have the expected behavior when called from Python,
        /// since the returned <see cref="decimal"/> will be converted to python float,
        /// <see cref="NormalizeToStr"/></remarks>
        public static decimal Normalize(this decimal input)
        {
            // http://stackoverflow.com/a/7983330/1582922
            return input / 1.000000000000000000000000000000000m;
        }

        /// <summary>
        /// Will remove any trailing zeros for the provided decimal and convert to string.
        /// Uses <see cref="Normalize(decimal)"/>.
        /// </summary>
        /// <param name="input">The <see cref="decimal"/> to convert to <see cref="string"/></param>
        /// <returns>Input converted to <see cref="string"/> with no trailing zeros</returns>
        public static string NormalizeToStr(this decimal input)
        {
            return Normalize(input).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Helper method to determine the amount of decimal places associated with the given decimal
        /// </summary>
        /// <param name="input">The value to get the decimal count from</param>
        /// <returns>The quantity of decimal places</returns>
        public static int GetDecimalPlaces(this decimal input)
        {
            return BitConverter.GetBytes(decimal.GetBits(input)[3])[2];
        }

        /// <summary>
        /// Extension method for faster string to decimal conversion.
        /// </summary>
        /// <param name="str">String to be converted to positive decimal value</param>
        /// <remarks>
        /// Leading and trailing whitespace chars are ignored
        /// </remarks>
        /// <returns>Decimal value of the string</returns>
        public static decimal ToDecimal(this string str)
        {
            long value = 0;
            var decimalPlaces = 0;
            var hasDecimals = false;
            var index = 0;
            var length = str.Length;

            while (index < length && char.IsWhiteSpace(str[index]))
            {
                index++;
            }

            var isNegative = index < length && str[index] == '-';
            if (isNegative)
            {
                index++;
            }

            while (index < length)
            {
                var ch = str[index++];
                if (ch == '.')
                {
                    hasDecimals = true;
                    decimalPlaces = 0;
                }
                else if (char.IsWhiteSpace(ch))
                {
                    break;
                }
                else
                {
                    value = value * 10 + (ch - '0');
                    decimalPlaces++;
                }
            }

            var lo = (int)value;
            var mid = (int)(value >> 32);
            return new decimal(lo, mid, 0, isNegative, (byte)(hasDecimals ? decimalPlaces : 0));
        }

        /// <summary>
        /// Extension method for faster string to normalized decimal conversion, i.e. 20.0% should be parsed into 0.2
        /// </summary>
        /// <param name="str">String to be converted to positive decimal value</param>
        /// <remarks>
        /// Leading and trailing whitespace chars are ignored
        /// </remarks>
        /// <returns>Decimal value of the string</returns>
        public static decimal ToNormalizedDecimal(this string str)
        {
            var trimmed = str.Trim();
            var value = str.TrimEnd('%').ToDecimal();
            if (trimmed.EndsWith("%"))
            {
                value /= 100;
            }

            return value;
        }

        /// <summary>
        /// Extension method for string to decimal conversion where string can represent a number with exponent xe-y
        /// </summary>
        /// <param name="str">String to be converted to decimal value</param>
        /// <returns>Decimal value of the string</returns>
        public static decimal ToDecimalAllowExponent(this string str)
        {
            return decimal.Parse(str, NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Extension method for faster string to Int32 conversion.
        /// </summary>
        /// <param name="str">String to be converted to positive Int32 value</param>
        /// <remarks>Method makes some assuptions - always numbers, no "signs" +,- etc.</remarks>
        /// <returns>Int32 value of the string</returns>
        public static int ToInt32(this string str)
        {
            int value = 0;
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] == '.')
                    break;

                value = value * 10 + (str[i] - '0');
            }
            return value;
        }

        /// <summary>
        /// Extension method for faster string to Int64 conversion.
        /// </summary>
        /// <param name="str">String to be converted to positive Int64 value</param>
        /// <remarks>Method makes some assuptions - always numbers, no "signs" +,- etc.</remarks>
        /// <returns>Int32 value of the string</returns>
        public static long ToInt64(this string str)
        {
            long value = 0;
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] == '.')
                    break;

                value = value * 10 + (str[i] - '0');
            }
            return value;
        }

        /// <summary>
        /// Helper method to determine if a data type implements the Stream reader method
        /// </summary>
        public static bool ImplementsStreamReader(this Type baseDataType)
        {
            // we know these type implement the streamReader interface lets avoid dynamic reflection call to figure it out
            if (baseDataType == typeof(TradeBar) || baseDataType == typeof(QuoteBar) || baseDataType == typeof(Tick))
            {
                return true;
            }

            var method = baseDataType.GetMethod("Reader",
                new[] { typeof(SubscriptionDataConfig), typeof(StreamReader), typeof(DateTime), typeof(bool) });
            if (method != null && method.DeclaringType == baseDataType)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Breaks the specified string into csv components, all commas are considered separators
        /// </summary>
        /// <param name="str">The string to be broken into csv</param>
        /// <param name="size">The expected size of the output list</param>
        /// <returns>A list of the csv pieces</returns>
        public static List<string> ToCsv(this string str, int size = 4)
        {
            int last = 0;
            var csv = new List<string>(size);
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ',')
                {
                    if (last != 0) last = last + 1;
                    csv.Add(str.Substring(last, i - last));
                    last = i;
                }
            }
            if (last != 0) last = last + 1;
            csv.Add(str.Substring(last));
            return csv;
        }

        /// <summary>
        /// Breaks the specified string into csv components, works correctly with commas in data fields
        /// </summary>
        /// <param name="str">The string to be broken into csv</param>
        /// <param name="size">The expected size of the output list</param>
        /// <param name="delimiter">The delimiter used to separate entries in the line</param>
        /// <returns>A list of the csv pieces</returns>
        public static List<string> ToCsvData(this string str, int size = 4, char delimiter = ',')
        {
            var csv = new List<string>(size);

            var last = -1;
            var count = 0;
            var textDataField = false;

            for (var i = 0; i < str.Length; i++)
            {
                var current = str[i];
                if (current == '"')
                {
                    textDataField = !textDataField;
                }
                else if (!textDataField && current == delimiter)
                {
                    csv.Add(str.Substring(last + 1, (i - last)).Trim(' ', ','));
                    last = i;
                    count++;
                }
            }

            if (last != 0)
            {
                csv.Add(str.Substring(last + 1).Trim());
            }

            return csv;
        }

        /// <summary>
        /// Gets the value at the specified index from a CSV line.
        /// </summary>
        /// <param name="csvLine">The CSV line</param>
        /// <param name="index">The index of the value to be extracted from the CSV line</param>
        /// <param name="result">The value at the given index</param>
        /// <returns>Whether there was a value at the given index and could be extracted</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetFromCsv(this string csvLine, int index, out ReadOnlySpan<char> result)
        {
            result = ReadOnlySpan<char>.Empty;
            if (string.IsNullOrEmpty(csvLine) || index < 0)
            {
                return false;
            }

            var span = csvLine.AsSpan();
            for (int i = 0; i < index; i++)
            {
                var commaIndex = span.IndexOf(',');
                if (commaIndex == -1)
                {
                    return false;
                }
                span = span.Slice(commaIndex + 1);
            }

            var nextCommaIndex = span.IndexOf(',');
            if (nextCommaIndex == -1)
            {
                nextCommaIndex = span.Length;
            }

            result = span.Slice(0, nextCommaIndex);
            return true;
        }

        /// <summary>
        /// Gets the value at the specified index from a CSV line, converted into a decimal.
        /// </summary>
        /// <param name="csvLine">The CSV line</param>
        /// <param name="index">The index of the value to be extracted from the CSV line</param>
        /// <param name="value">The decimal value at the given index</param>
        /// <returns>Whether there was a value at the given index and could be extracted and converted into a decimal</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetDecimalFromCsv(this string csvLine, int index, out decimal value)
        {
            value = decimal.Zero;
            if (!csvLine.TryGetFromCsv(index, out var csvValue))
            {
                return false;
            }

            return decimal.TryParse(csvValue, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Gets the value at the specified index from a CSV line, converted into a decimal.
        /// </summary>
        /// <param name="csvLine">The CSV line</param>
        /// <param name="index">The index of the value to be extracted from the CSV line</param>
        /// <returns>The decimal value at the given index. If the index is invalid or conversion fails, it will return zero</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetDecimalFromCsv(this string csvLine, int index)
        {
            csvLine.TryGetDecimalFromCsv(index, out var value);
            return value;
        }

        /// <summary>
        /// Check if a number is NaN or infinity
        /// </summary>
        /// <param name="value">The double value to check</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaNOrInfinity(this double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value);
        }

        /// <summary>
        /// Check if a number is NaN or equal to zero
        /// </summary>
        /// <param name="value">The double value to check</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaNOrZero(this double value)
        {
            return double.IsNaN(value) || Math.Abs(value) < double.Epsilon;
        }

        /// <summary>
        /// Gets the smallest positive number that can be added to a decimal instance and return
        /// a new value that does not == the old value
        /// </summary>
        public static decimal GetDecimalEpsilon()
        {
            return new decimal(1, 0, 0, false, 27); //1e-27m;
        }

        /// <summary>
        /// Extension method to extract the extension part of this file name if it matches a safe list, or return a ".custom" extension for ones which do not match.
        /// </summary>
        /// <param name="str">String we're looking for the extension for.</param>
        /// <returns>Last 4 character string of string.</returns>
        public static string GetExtension(this string str) {
            var ext = str.Substring(Math.Max(0, str.Length - 4));
            var allowedExt = new List<string> { ".zip", ".csv", ".json", ".tsv" };
            if (!allowedExt.Contains(ext))
            {
                ext = ".custom";
            }
            return ext;
        }

        /// <summary>
        /// Extension method to convert strings to stream to be read.
        /// </summary>
        /// <param name="str">String to convert to stream</param>
        /// <returns>Stream instance</returns>
        public static Stream ToStream(this string str)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Extension method to round a timeSpan to nearest timespan period.
        /// </summary>
        /// <param name="time">TimeSpan To Round</param>
        /// <param name="roundingInterval">Rounding Unit</param>
        /// <param name="roundingType">Rounding method</param>
        /// <returns>Rounded timespan</returns>
        public static TimeSpan Round(this TimeSpan time, TimeSpan roundingInterval, MidpointRounding roundingType)
        {
            if (roundingInterval == TimeSpan.Zero)
            {
                // divide by zero exception
                return time;
            }

            return new TimeSpan(
                Convert.ToInt64(Math.Round(
                    time.Ticks / (decimal)roundingInterval.Ticks,
                    roundingType
                )) * roundingInterval.Ticks
            );
        }


        /// <summary>
        /// Extension method to round timespan to nearest timespan period.
        /// </summary>
        /// <param name="time">Base timespan we're looking to round.</param>
        /// <param name="roundingInterval">Timespan period we're rounding.</param>
        /// <returns>Rounded timespan period</returns>
        public static TimeSpan Round(this TimeSpan time, TimeSpan roundingInterval)
        {
            return Round(time, roundingInterval, MidpointRounding.ToEven);
        }

        /// <summary>
        /// Extension method to round a datetime down by a timespan interval.
        /// </summary>
        /// <param name="dateTime">Base DateTime object we're rounding down.</param>
        /// <param name="interval">Timespan interval to round to</param>
        /// <returns>Rounded datetime</returns>
        /// <remarks>Using this with timespans greater than 1 day may have unintended
        /// consequences. Be aware that rounding occurs against ALL time, so when using
        /// timespan such as 30 days we will see 30 day increments but it will be based
        /// on 30 day increments from the beginning of time.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime RoundDown(this DateTime dateTime, TimeSpan interval)
        {
            if (interval == TimeSpan.Zero)
            {
                // divide by zero exception
                return dateTime;
            }

            var amount = dateTime.Ticks % interval.Ticks;
            if (amount > 0)
            {
                return dateTime.AddTicks(-amount);
            }
            return dateTime;
        }

        /// <summary>
        /// Rounds the specified date time in the specified time zone. Careful with calling this method in a loop while modifying dateTime, check unit tests.
        /// </summary>
        /// <param name="dateTime">Date time to be rounded</param>
        /// <param name="roundingInterval">Timespan rounding period</param>
        /// <param name="sourceTimeZone">Time zone of the date time</param>
        /// <param name="roundingTimeZone">Time zone in which the rounding is performed</param>
        /// <returns>The rounded date time in the source time zone</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime RoundDownInTimeZone(this DateTime dateTime, TimeSpan roundingInterval, DateTimeZone sourceTimeZone, DateTimeZone roundingTimeZone)
        {
            var dateTimeInRoundingTimeZone = dateTime.ConvertTo(sourceTimeZone, roundingTimeZone);
            var roundedDateTimeInRoundingTimeZone = dateTimeInRoundingTimeZone.RoundDown(roundingInterval);
            return roundedDateTimeInRoundingTimeZone.ConvertTo(roundingTimeZone, sourceTimeZone);
        }

        /// <summary>
        /// Extension method to round a datetime down by a timespan interval until it's
        /// within the specified exchange's open hours. This works by first rounding down
        /// the specified time using the interval, then producing a bar between that
        /// rounded time and the interval plus the rounded time and incrementally walking
        /// backwards until the exchange is open
        /// </summary>
        /// <param name="dateTime">Time to be rounded down</param>
        /// <param name="interval">Timespan interval to round to.</param>
        /// <param name="exchangeHours">The exchange hours to determine open times</param>
        /// <param name="extendedMarketHours">True for extended market hours, otherwise false</param>
        /// <returns>Rounded datetime</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ExchangeRoundDown(this DateTime dateTime, TimeSpan interval, SecurityExchangeHours exchangeHours, bool extendedMarketHours)
        {
            // can't round against a zero interval
            if (interval == TimeSpan.Zero) return dateTime;

            var rounded = dateTime.RoundDown(interval);
            while (!exchangeHours.IsOpen(rounded, rounded + interval, extendedMarketHours))
            {
                rounded -= interval;
            }
            return rounded;
        }

        /// <summary>
        /// Extension method to round a datetime down by a timespan interval until it's
        /// within the specified exchange's open hours. The rounding is performed in the
        /// specified time zone
        /// </summary>
        /// <param name="dateTime">Time to be rounded down</param>
        /// <param name="interval">Timespan interval to round to.</param>
        /// <param name="exchangeHours">The exchange hours to determine open times</param>
        /// <param name="roundingTimeZone">The time zone to perform the rounding in</param>
        /// <param name="extendedMarketHours">True for extended market hours, otherwise false</param>
        /// <returns>Rounded datetime</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ExchangeRoundDownInTimeZone(this DateTime dateTime, TimeSpan interval, SecurityExchangeHours exchangeHours, DateTimeZone roundingTimeZone, bool extendedMarketHours)
        {
            // can't round against a zero interval
            if (interval == TimeSpan.Zero) return dateTime;

            var dateTimeInRoundingTimeZone = dateTime.ConvertTo(exchangeHours.TimeZone, roundingTimeZone);
            var roundedDateTimeInRoundingTimeZone = dateTimeInRoundingTimeZone.RoundDown(interval);
            var rounded = roundedDateTimeInRoundingTimeZone.ConvertTo(roundingTimeZone, exchangeHours.TimeZone);

            while (!exchangeHours.IsOpen(rounded, rounded + interval, extendedMarketHours))
            {
                // Will subtract interval to 'dateTime' in the roundingTimeZone (using the same value type instance) to avoid issues with daylight saving time changes.
                // GH issue 2368: subtracting interval to 'dateTime' in exchangeHours.TimeZone and converting back to roundingTimeZone
                // caused the substraction to be neutralized by daylight saving time change, which caused an infinite loop situation in this loop.
                // The issue also happens if substracting in roundingTimeZone and converting back to exchangeHours.TimeZone.

                dateTimeInRoundingTimeZone -= interval;
                roundedDateTimeInRoundingTimeZone = dateTimeInRoundingTimeZone.RoundDown(interval);
                rounded = roundedDateTimeInRoundingTimeZone.ConvertTo(roundingTimeZone, exchangeHours.TimeZone);
            }
            return rounded;
        }

        /// <summary>
        /// Helper method to determine if a specific market is open
        /// </summary>
        /// <param name="security">The target security</param>
        /// <param name="extendedMarketHours">True if should consider extended market hours</param>
        /// <returns>True if the market is open</returns>
        public static bool IsMarketOpen(this Security security, bool extendedMarketHours)
        {
            return security.Exchange.Hours.IsOpen(security.LocalTime, extendedMarketHours);
        }

        /// <summary>
        /// Helper method to determine if a specific market is open
        /// </summary>
        /// <param name="symbol">The target symbol</param>
        /// <param name="utcTime">The current UTC time</param>
        /// <param name="extendedMarketHours">True if should consider extended market hours</param>
        /// <returns>True if the market is open</returns>
        public static bool IsMarketOpen(this Symbol symbol, DateTime utcTime, bool extendedMarketHours)
        {
            var exchangeHours = MarketHoursDatabase.FromDataFolder()
                .GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);

            var time = utcTime.ConvertFromUtc(exchangeHours.TimeZone);

            return exchangeHours.IsOpen(time, extendedMarketHours);
        }

        /// <summary>
        /// Extension method to round a datetime to the nearest unit timespan.
        /// </summary>
        /// <param name="datetime">Datetime object we're rounding.</param>
        /// <param name="roundingInterval">Timespan rounding period.</param>
        /// <returns>Rounded datetime</returns>
        public static DateTime Round(this DateTime datetime, TimeSpan roundingInterval)
        {
            return new DateTime((datetime - DateTime.MinValue).Round(roundingInterval).Ticks);
        }

        /// <summary>
        /// Extension method to explicitly round up to the nearest timespan interval.
        /// </summary>
        /// <param name="time">Base datetime object to round up.</param>
        /// <param name="interval">Timespan interval to round to</param>
        /// <returns>Rounded datetime</returns>
        /// <remarks>Using this with timespans greater than 1 day may have unintended
        /// consequences. Be aware that rounding occurs against ALL time, so when using
        /// timespan such as 30 days we will see 30 day increments but it will be based
        /// on 30 day increments from the beginning of time.</remarks>
        public static DateTime RoundUp(this DateTime time, TimeSpan interval)
        {
            if (interval == TimeSpan.Zero)
            {
                // divide by zero exception
                return time;
            }

            return new DateTime(((time.Ticks + interval.Ticks - 1) / interval.Ticks) * interval.Ticks);
        }

        /// <summary>
        /// Converts the specified time from the <paramref name="from"/> time zone to the <paramref name="to"/> time zone
        /// </summary>
        /// <param name="time">The time to be converted in terms of the <paramref name="from"/> time zone</param>
        /// <param name="from">The time zone the specified <paramref name="time"/> is in</param>
        /// <param name="to">The time zone to be converted to</param>
        /// <param name="strict">True for strict conversion, this will throw during ambiguitities, false for lenient conversion</param>
        /// <returns>The time in terms of the to time zone</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ConvertTo(this DateTime time, DateTimeZone from, DateTimeZone to, bool strict = false)
        {
            if (strict)
            {
                return from.AtStrictly(LocalDateTime.FromDateTime(time)).WithZone(to).ToDateTimeUnspecified();
            }

            // `InZone` sets the LocalDateTime's timezone, `WithZone` is the tz the time will be converted into.
            return LocalDateTime.FromDateTime(time)
                .InZone(from, _mappingResolver)
                .WithZone(to)
                .ToDateTimeUnspecified();
        }

        /// <summary>
        /// Converts the specified time from UTC to the <paramref name="to"/> time zone
        /// </summary>
        /// <param name="time">The time to be converted expressed in UTC</param>
        /// <param name="to">The destinatio time zone</param>
        /// <param name="strict">True for strict conversion, this will throw during ambiguitities, false for lenient conversion</param>
        /// <returns>The time in terms of the <paramref name="to"/> time zone</returns>
        public static DateTime ConvertFromUtc(this DateTime time, DateTimeZone to, bool strict = false)
        {
            return time.ConvertTo(TimeZones.Utc, to, strict);
        }

        /// <summary>
        /// Converts the specified time from the <paramref name="from"/> time zone to <see cref="TimeZones.Utc"/>
        /// </summary>
        /// <param name="time">The time to be converted in terms of the <paramref name="from"/> time zone</param>
        /// <param name="from">The time zone the specified <paramref name="time"/> is in</param>
        /// <param name="strict">True for strict conversion, this will throw during ambiguitities, false for lenient conversion</param>
        /// <returns>The time in terms of the to time zone</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ConvertToUtc(this DateTime time, DateTimeZone from, bool strict = false)
        {
            if (strict)
            {
                return from.AtStrictly(LocalDateTime.FromDateTime(time)).ToDateTimeUtc();
            }

            // Set the local timezone with `InZone` and convert to UTC
            return LocalDateTime.FromDateTime(time)
                .InZone(from, _mappingResolver)
                .ToDateTimeUtc();
        }

        /// <summary>
        /// Business day here is defined as any day of the week that is not saturday or sunday
        /// </summary>
        /// <param name="date">The date to be examined</param>
        /// <returns>A bool indicating wether the datetime is a weekday or not</returns>
        public static bool IsCommonBusinessDay(this DateTime date)
        {
            return (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday);
        }

        /// <summary>
        /// Add the reset method to the System.Timer class.
        /// </summary>
        /// <param name="timer">System.timer object</param>
        public static void Reset(this Timer timer)
        {
            timer.Stop();
            timer.Start();
        }

        /// <summary>
        /// Function used to match a type against a string type name. This function compares on the AssemblyQualfiedName,
        /// the FullName, and then just the Name of the type.
        /// </summary>
        /// <param name="type">The type to test for a match</param>
        /// <param name="typeName">The name of the type to match</param>
        /// <returns>True if the specified type matches the type name, false otherwise</returns>
        public static bool MatchesTypeName(this Type type, string typeName)
        {
            if (type.AssemblyQualifiedName == typeName)
            {
                return true;
            }
            if (type.FullName == typeName)
            {
                return true;
            }
            if (type.Name == typeName)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks the specified type to see if it is a subclass of the <paramref name="possibleSuperType"/>. This method will
        /// crawl up the inheritance heirarchy to check for equality using generic type definitions (if exists)
        /// </summary>
        /// <param name="type">The type to be checked as a subclass of <paramref name="possibleSuperType"/></param>
        /// <param name="possibleSuperType">The possible superclass of <paramref name="type"/></param>
        /// <returns>True if <paramref name="type"/> is a subclass of the generic type definition <paramref name="possibleSuperType"/></returns>
        public static bool IsSubclassOfGeneric(this Type type, Type possibleSuperType)
        {
            while (type != null && type != typeof(object))
            {
                Type cur;
                if (type.IsGenericType && possibleSuperType.IsGenericTypeDefinition)
                {
                    cur = type.GetGenericTypeDefinition();
                }
                else
                {
                    cur = type;
                }
                if (possibleSuperType == cur)
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }

        /// <summary>
        /// Gets a type's name with the generic parameters filled in the way they would look when
        /// defined in code, such as converting Dictionary&lt;`1,`2&gt; to Dictionary&lt;string,int&gt;
        /// </summary>
        /// <param name="type">The type who's name we seek</param>
        /// <returns>A better type name</returns>
        public static string GetBetterTypeName(this Type type)
        {
            string name = type.Name;
            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                var toBeReplaced = "`" + (genericArguments.Length);
                name = name.Replace(toBeReplaced, $"<{string.Join(", ", genericArguments.Select(x => x.GetBetterTypeName()))}>");
            }
            return name;
        }

        /// <summary>
        /// Converts the Resolution instance into a TimeSpan instance
        /// </summary>
        /// <param name="resolution">The resolution to be converted</param>
        /// <returns>A TimeSpan instance that represents the resolution specified</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan ToTimeSpan(this Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                    // ticks can be instantaneous
                    return TimeSpan.Zero;
                case Resolution.Second:
                    return Time.OneSecond;
                case Resolution.Minute:
                    return Time.OneMinute;
                case Resolution.Hour:
                    return Time.OneHour;
                case Resolution.Daily:
                    return Time.OneDay;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution));
            }
        }

        /// <summary>
        /// Converts the specified time span into a resolution enum value. If an exact match
        /// is not found and `requireExactMatch` is false, then the higher resoluion will be
        /// returned. For example, timeSpan=5min will return Minute resolution.
        /// </summary>
        /// <param name="timeSpan">The time span to convert to resolution</param>
        /// <param name="requireExactMatch">True to throw an exception if an exact match is not found</param>
        /// <returns>The resolution</returns>
        public static Resolution ToHigherResolutionEquivalent(this TimeSpan timeSpan, bool requireExactMatch)
        {
            if (requireExactMatch)
            {
                if (TimeSpan.Zero == timeSpan)  return Resolution.Tick;
                if (Time.OneSecond == timeSpan) return Resolution.Second;
                if (Time.OneMinute == timeSpan) return Resolution.Minute;
                if (Time.OneHour   == timeSpan) return Resolution.Hour;
                if (Time.OneDay    == timeSpan) return Resolution.Daily;
                throw new InvalidOperationException(Messages.Extensions.UnableToConvertTimeSpanToResolution(timeSpan));
            }

            // for non-perfect matches
            if (Time.OneSecond > timeSpan) return Resolution.Tick;
            if (Time.OneMinute > timeSpan) return Resolution.Second;
            if (Time.OneHour   > timeSpan) return Resolution.Minute;
            if (Time.OneDay    > timeSpan) return Resolution.Hour;

            return Resolution.Daily;
        }

        /// <summary>
        /// Attempts to convert the string into a <see cref="SecurityType"/> enum value
        /// </summary>
        /// <param name="value">string value to convert to SecurityType</param>
        /// <param name="securityType">SecurityType output</param>
        /// <param name="ignoreCase">Ignore casing</param>
        /// <returns>true if parsed into a SecurityType successfully, false otherwise</returns>
        /// <remarks>
        /// Logs once if we've encountered an invalid SecurityType
        /// </remarks>
        public static bool TryParseSecurityType(this string value, out SecurityType securityType, bool ignoreCase = true)
        {
            if (Enum.TryParse(value, ignoreCase, out securityType))
            {
                return true;
            }

            if (InvalidSecurityTypes.Add(value))
            {
                Log.Error($"Extensions.TryParseSecurityType(): {Messages.Extensions.UnableToParseUnknownSecurityType(value)}");
            }

            return false;

        }

        /// <summary>
        /// Converts the specified string value into the specified type
        /// </summary>
        /// <typeparam name="T">The output type</typeparam>
        /// <param name="value">The string value to be converted</param>
        /// <returns>The converted value</returns>
        public static T ConvertTo<T>(this string value)
        {
            return (T) value.ConvertTo(typeof (T));
        }

        /// <summary>
        /// Converts the specified string value into the specified type
        /// </summary>
        /// <param name="value">The string value to be converted</param>
        /// <param name="type">The output type</param>
        /// <returns>The converted value</returns>
        public static object ConvertTo(this string value, Type type)
        {
            if (type.IsEnum)
            {
                return Enum.Parse(type, value, true);
            }

            if (typeof (IConvertible).IsAssignableFrom(type))
            {
                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }

            // try and find a static parse method
            var parse = type.GetMethod("Parse", new[] {typeof (string)});
            if (parse != null)
            {
                var result = parse.Invoke(null, new object[] {value});
                return result;
            }

            return JsonConvert.DeserializeObject(value, type);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="T:System.Threading.WaitHandle"/> receives a signal, while observing a <see cref="T:System.Threading.CancellationToken"/>.
        /// </summary>
        /// <param name="waitHandle">The wait handle to wait on</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken"/> to observe.</param>
        /// <exception cref="T:System.InvalidOperationException">The maximum number of waiters has been exceeded.</exception>
        /// <exception cref="T:System.OperationCanceledExcepton"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The object has already been disposed or the <see cref="T:System.Threading.CancellationTokenSource"/> that created <paramref name="cancellationToken"/> has been disposed.</exception>
        public static bool WaitOne(this WaitHandle waitHandle, CancellationToken cancellationToken)
        {
            return waitHandle.WaitOne(Timeout.Infinite, cancellationToken);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="T:System.Threading.WaitHandle"/> is set, using a <see cref="T:System.TimeSpan"/> to measure the time interval, while observing a <see cref="T:System.Threading.CancellationToken"/>.
        /// </summary>
        ///
        /// <returns>
        /// true if the <see cref="T:System.Threading.WaitHandle"/> was set; otherwise, false.
        /// </returns>
        /// <param name="waitHandle">The wait handle to wait on</param>
        /// <param name="timeout">A <see cref="T:System.TimeSpan"/> that represents the number of milliseconds to wait, or a <see cref="T:System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken"/> to observe.</param>
        /// <exception cref="T:System.Threading.OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than <see cref="F:System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.InvalidOperationException">The maximum number of waiters has been exceeded. </exception><exception cref="T:System.ObjectDisposedException">The object has already been disposed or the <see cref="T:System.Threading.CancellationTokenSource"/> that created <paramref name="cancellationToken"/> has been disposed.</exception>
        public static bool WaitOne(this WaitHandle waitHandle, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return waitHandle.WaitOne((int) timeout.TotalMilliseconds, cancellationToken);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="T:System.Threading.WaitHandle"/> is set, using a 32-bit signed integer to measure the time interval, while observing a <see cref="T:System.Threading.CancellationToken"/>.
        /// </summary>
        ///
        /// <returns>
        /// true if the <see cref="T:System.Threading.WaitHandle"/> was set; otherwise, false.
        /// </returns>
        /// <param name="waitHandle">The wait handle to wait on</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see cref="F:System.Threading.Timeout.Infinite"/>(-1) to wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken"/> to observe.</param>
        /// <exception cref="T:System.Threading.OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.InvalidOperationException">The maximum number of waiters has been exceeded.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The object has already been disposed or the <see cref="T:System.Threading.CancellationTokenSource"/> that created <paramref name="cancellationToken"/> has been disposed.</exception>
        public static bool WaitOne(this WaitHandle waitHandle, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return WaitHandle.WaitAny(new[] { waitHandle, cancellationToken.WaitHandle }, millisecondsTimeout) == 0;
        }

        /// <summary>
        /// Gets the MD5 hash from a stream
        /// </summary>
        /// <param name="stream">The stream to compute a hash for</param>
        /// <returns>The MD5 hash</returns>
        public static byte[] GetMD5Hash(this Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(stream);
            }
        }

        /// <summary>
        /// Convert a string into the same string with a URL! :)
        /// </summary>
        /// <param name="source">The source string to be converted</param>
        /// <returns>The same source string but with anchor tags around substrings matching a link regex</returns>
        public static string WithEmbeddedHtmlAnchors(this string source)
        {
            var regx = new Regex("http(s)?://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*([a-zA-Z0-9\\?\\#\\=\\/]){1})?", RegexOptions.IgnoreCase);
            var matches = regx.Matches(source);
            foreach (Match match in matches)
            {
                source = source.Replace(match.Value, $"<a href=\'{match.Value}\' target=\'blank\'>{match.Value}</a>");
            }
            return source;
        }

        /// <summary>
        /// Get the first occurence of a string between two characters from another string
        /// </summary>
        /// <param name="value">The original string</param>
        /// <param name="left">Left bound of the substring</param>
        /// <param name="right">Right bound of the substring</param>
        /// <returns>Substring from original string bounded by the two characters</returns>
        public static string GetStringBetweenChars(this string value, char left, char right)
        {
            var startIndex = 1 + value.IndexOf(left);
            var length = value.IndexOf(right, startIndex) - startIndex;
            if (length > 0)
            {
                value = value.Substring(startIndex, length);
                startIndex = 1 + value.IndexOf(left);
                return value.Substring(startIndex).Trim();
            }
            return string.Empty;
        }

        /// <summary>
        /// Return the first in the series of names, or find the one that matches the configured algorithmTypeName
        /// </summary>
        /// <param name="names">The list of class names</param>
        /// <param name="algorithmTypeName">The configured algorithm type name from the config</param>
        /// <returns>The name of the class being run</returns>
        public static string SingleOrAlgorithmTypeName(this List<string> names, string algorithmTypeName)
        {
            // If there's only one name use that guy
            if (names.Count == 1) { return names.Single(); }

            // If we have multiple names we need to search the names based on the given algorithmTypeName
            // If the given name already contains dots (fully named) use it as it is
            // otherwise add a dot to the beginning to avoid matching any subsets of other names
            var searchName = algorithmTypeName.Contains('.', StringComparison.InvariantCulture) ? algorithmTypeName : "." + algorithmTypeName;
            return names.SingleOrDefault(x => x.EndsWith(searchName));
        }

        /// <summary>
        /// Converts the specified <paramref name="enum"/> value to its corresponding lower-case string representation
        /// </summary>
        /// <param name="enum">The enumeration value</param>
        /// <returns>A lower-case string representation of the specified enumeration value</returns>
        public static string ToLower(this Enum @enum)
        {
            return @enum.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Asserts the specified <paramref name="securityType"/> value is valid
        /// </summary>
        /// <remarks>This method provides faster performance than <see cref="Enum.IsDefined"/> which uses reflection</remarks>
        /// <param name="securityType">The SecurityType value</param>
        /// <returns>True if valid security type value</returns>
        public static bool IsValid(this SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Base:
                case SecurityType.Equity:
                case SecurityType.Option:
                case SecurityType.FutureOption:
                case SecurityType.Commodity:
                case SecurityType.Forex:
                case SecurityType.Future:
                case SecurityType.Cfd:
                case SecurityType.Crypto:
                case SecurityType.CryptoFuture:
                case SecurityType.Index:
                case SecurityType.IndexOption:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if the provided SecurityType is a type of Option.
        /// Valid option types are: Equity Options, Futures Options, and Index Options.
        /// </summary>
        /// <param name="securityType">The SecurityType to check if it's an option asset</param>
        /// <returns>
        /// true if the asset has the makings of an option (exercisable, expires, and is a derivative of some underlying),
        /// false otherwise.
        /// </returns>
        public static bool IsOption(this SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Option:
                case SecurityType.FutureOption:
                case SecurityType.IndexOption:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if the provided SecurityType has a matching option SecurityType, used to represent
        /// the current SecurityType as a derivative.
        /// </summary>
        /// <param name="securityType">The SecurityType to check if it has options available</param>
        /// <returns>true if there are options for the SecurityType, false otherwise</returns>
        public static bool HasOptions(this SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Equity:
                case SecurityType.Future:
                case SecurityType.Index:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the default <see cref="OptionStyle"/> for the provided <see cref="SecurityType"/>
        /// </summary>
        /// <param name="securityType">SecurityType to get default OptionStyle for</param>
        /// <returns>Default OptionStyle for the SecurityType</returns>
        /// <exception cref="ArgumentException">The SecurityType has no options available for it or it is not an option</exception>
        public static OptionStyle DefaultOptionStyle(this SecurityType securityType)
        {
            if (!securityType.HasOptions() && !securityType.IsOption())
            {
                throw new ArgumentException(Messages.Extensions.NoDefaultOptionStyleForSecurityType(securityType));
            }

            switch (securityType)
            {
                case SecurityType.Index:
                case SecurityType.IndexOption:
                    return OptionStyle.European;

                default:
                    return OptionStyle.American;
            }
        }

        /// <summary>
        /// Converts the specified string to its corresponding OptionStyle
        /// </summary>
        /// <remarks>This method provides faster performance than enum parse</remarks>
        /// <param name="optionStyle">The OptionStyle string value</param>
        /// <returns>The OptionStyle value</returns>
        public static OptionStyle ParseOptionStyle(this string optionStyle)
        {
            switch (optionStyle.LazyToLower())
            {
                case "american":
                    return OptionStyle.American;
                case "european":
                    return OptionStyle.European;
                default:
                    throw new ArgumentException(Messages.Extensions.UnknownOptionStyle(optionStyle));
            }
        }

        /// <summary>
        /// Converts the specified string to its corresponding OptionRight
        /// </summary>
        /// <remarks>This method provides faster performance than enum parse</remarks>
        /// <param name="optionRight">The optionRight string value</param>
        /// <returns>The OptionRight value</returns>
        public static OptionRight ParseOptionRight(this string optionRight)
        {
            switch (optionRight.LazyToLower())
            {
                case "call":
                    return OptionRight.Call;
                case "put":
                    return OptionRight.Put;
                default:
                    throw new ArgumentException(Messages.Extensions.UnknownOptionRight(optionRight));
            }
        }

        /// <summary>
        /// Converts the specified <paramref name="optionRight"/> value to its corresponding string representation
        /// </summary>
        /// <remarks>This method provides faster performance than enum <see cref="Object.ToString"/></remarks>
        /// <param name="optionRight">The optionRight value</param>
        /// <returns>A string representation of the specified OptionRight value</returns>
        public static string ToStringPerformance(this OptionRight optionRight)
        {
            switch (optionRight)
            {
                case OptionRight.Call:
                    return "Call";
                case OptionRight.Put:
                    return "Put";
                default:
                    // just in case
                    return optionRight.ToString();
            }
        }

        /// <summary>
        /// Converts the specified <paramref name="optionRight"/> value to its corresponding lower-case string representation
        /// </summary>
        /// <remarks>This method provides faster performance than <see cref="ToLower"/></remarks>
        /// <param name="optionRight">The optionRight value</param>
        /// <returns>A lower case string representation of the specified OptionRight value</returns>
        public static string OptionRightToLower(this OptionRight optionRight)
        {
            switch (optionRight)
            {
                case OptionRight.Call:
                    return "call";
                case OptionRight.Put:
                    return "put";
                default:
                    throw new ArgumentException(Messages.Extensions.UnknownOptionRight(optionRight));
            }
        }

        /// <summary>
        /// Converts the specified <paramref name="optionStyle"/> value to its corresponding lower-case string representation
        /// </summary>
        /// <remarks>This method provides faster performance than <see cref="ToLower"/></remarks>
        /// <param name="optionStyle">The optionStyle value</param>
        /// <returns>A lower case string representation of the specified optionStyle value</returns>
        public static string OptionStyleToLower(this OptionStyle optionStyle)
        {
            switch (optionStyle)
            {
                case OptionStyle.American:
                    return "american";
                case OptionStyle.European:
                    return "european";
                default:
                    throw new ArgumentException(Messages.Extensions.UnknownOptionStyle(optionStyle));
            }
        }

        /// <summary>
        /// Converts the specified string to its corresponding DataMappingMode
        /// </summary>
        /// <remarks>This method provides faster performance than enum parse</remarks>
        /// <param name="dataMappingMode">The dataMappingMode string value</param>
        /// <returns>The DataMappingMode value</returns>
        public static DataMappingMode? ParseDataMappingMode(this string dataMappingMode)
        {
            if (string.IsNullOrEmpty(dataMappingMode))
            {
                return null;
            }
            switch (dataMappingMode.LazyToLower())
            {
                case "0":
                case "lasttradingday":
                    return DataMappingMode.LastTradingDay;
                case "1":
                case "firstdaymonth":
                    return DataMappingMode.FirstDayMonth;
                case "2":
                case "openinterest":
                    return DataMappingMode.OpenInterest;
                case "3":
                case "openinterestannual":
                    return DataMappingMode.OpenInterestAnnual;
                default:
                    throw new ArgumentException(Messages.Extensions.UnknownDataMappingMode(dataMappingMode));
            }
        }

        /// <summary>
        /// Converts the specified <paramref name="securityType"/> value to its corresponding lower-case string representation
        /// </summary>
        /// <remarks>This method provides faster performance than <see cref="ToLower"/></remarks>
        /// <param name="securityType">The SecurityType value</param>
        /// <returns>A lower-case string representation of the specified SecurityType value</returns>
        public static string SecurityTypeToLower(this SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Base:
                    return "base";
                case SecurityType.Equity:
                    return "equity";
                case SecurityType.Option:
                    return "option";
                case SecurityType.FutureOption:
                    return "futureoption";
                case SecurityType.IndexOption:
                    return "indexoption";
                case SecurityType.Commodity:
                    return "commodity";
                case SecurityType.Forex:
                    return "forex";
                case SecurityType.Future:
                    return "future";
                case SecurityType.Index:
                    return "index";
                case SecurityType.Cfd:
                    return "cfd";
                case SecurityType.Crypto:
                    return "crypto";
                case SecurityType.CryptoFuture:
                    return "cryptofuture";
                default:
                    // just in case
                    return securityType.ToLower();
            }
        }

        /// <summary>
        /// Converts the specified <paramref name="tickType"/> value to its corresponding lower-case string representation
        /// </summary>
        /// <remarks>This method provides faster performance than <see cref="ToLower"/></remarks>
        /// <param name="tickType">The tickType value</param>
        /// <returns>A lower-case string representation of the specified tickType value</returns>
        public static string TickTypeToLower(this TickType tickType)
        {
            switch (tickType)
            {
                case TickType.Trade:
                    return "trade";
                case TickType.Quote:
                    return "quote";
                case TickType.OpenInterest:
                    return "openinterest";
                default:
                    // just in case
                    return tickType.ToLower();
            }
        }

        /// <summary>
        /// Converts the specified <paramref name="resolution"/> value to its corresponding lower-case string representation
        /// </summary>
        /// <remarks>This method provides faster performance than <see cref="ToLower"/></remarks>
        /// <param name="resolution">The resolution value</param>
        /// <returns>A lower-case string representation of the specified resolution value</returns>
        public static string ResolutionToLower(this Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                    return "tick";
                case Resolution.Second:
                    return "second";
                case Resolution.Minute:
                    return "minute";
                case Resolution.Hour:
                    return "hour";
                case Resolution.Daily:
                    return "daily";
                default:
                    // just in case
                    return resolution.ToLower();
            }
        }

        /// <summary>
        /// Turn order into an order ticket
        /// </summary>
        /// <param name="order">The <see cref="Order"/> being converted</param>
        /// <param name="transactionManager">The transaction manager, <see cref="SecurityTransactionManager"/></param>
        /// <returns></returns>
        public static OrderTicket ToOrderTicket(this Order order, SecurityTransactionManager transactionManager)
        {
            var limitPrice = 0m;
            var stopPrice = 0m;
            var triggerPrice = 0m;
            var trailingAmount = 0m;
            var trailingAsPercentage = false;

            switch (order.Type)
            {
                case OrderType.Limit:
                    var limitOrder = order as LimitOrder;
                    limitPrice = limitOrder.LimitPrice;
                    break;
                case OrderType.StopMarket:
                    var stopMarketOrder = order as StopMarketOrder;
                    stopPrice = stopMarketOrder.StopPrice;
                    break;
                case OrderType.StopLimit:
                    var stopLimitOrder = order as StopLimitOrder;
                    stopPrice = stopLimitOrder.StopPrice;
                    limitPrice = stopLimitOrder.LimitPrice;
                    break;
                case OrderType.TrailingStop:
                    var trailingStopOrder = order as TrailingStopOrder;
                    stopPrice = trailingStopOrder.StopPrice;
                    trailingAmount = trailingStopOrder.TrailingAmount;
                    trailingAsPercentage = trailingStopOrder.TrailingAsPercentage;
                    break;
                case OrderType.LimitIfTouched:
                    var limitIfTouched = order as LimitIfTouchedOrder;
                    triggerPrice = limitIfTouched.TriggerPrice;
                    limitPrice = limitIfTouched.LimitPrice;
                    break;
                case OrderType.OptionExercise:
                case OrderType.Market:
                case OrderType.MarketOnOpen:
                case OrderType.MarketOnClose:
                case OrderType.ComboMarket:
                    limitPrice = order.Price;
                    stopPrice = order.Price;
                    break;
                case OrderType.ComboLimit:
                    limitPrice = order.GroupOrderManager.LimitPrice;
                    break;
                case OrderType.ComboLegLimit:
                    var legLimitOrder = order as ComboLegLimitOrder;
                    limitPrice = legLimitOrder.LimitPrice;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var submitOrderRequest = new SubmitOrderRequest(order.Type,
                order.SecurityType,
                order.Symbol,
                order.Quantity,
                stopPrice,
                limitPrice,
                triggerPrice,
                trailingAmount,
                trailingAsPercentage,
                order.Time,
                order.Tag,
                order.Properties,
                order.GroupOrderManager);

            submitOrderRequest.SetOrderId(order.Id);
            var orderTicket = new OrderTicket(transactionManager, submitOrderRequest);
            orderTicket.SetOrder(order);
            return orderTicket;
        }

        /// <summary>
        /// Process all items in collection through given handler
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Collection to process</param>
        /// <param name="handler">Handler to process those items with</param>
        public static void ProcessUntilEmpty<T>(this IProducerConsumerCollection<T> collection, Action<T> handler)
        {
            T item;
            while (collection.TryTake(out item))
            {
                handler(item);
            }
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="PyObject"/>
        /// </summary>
        /// <param name="pyObject">The <see cref="PyObject"/> being converted</param>
        /// <returns>string that represents the current PyObject</returns>
        public static string ToSafeString(this PyObject pyObject)
        {
            using (Py.GIL())
            {
                var value = "";
                // PyObject objects that have the to_string method, like some pandas objects,
                // can use this method to convert them into string objects
                if (pyObject.HasAttr("to_string"))
                {
                    var pyValue = pyObject.InvokeMethod("to_string");
                    value = Environment.NewLine + pyValue;
                    pyValue.Dispose();
                }
                else
                {
                    value = pyObject.ToString();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        var pythonType = pyObject.GetPythonType();
                        if (pythonType.GetType() == typeof(PyObject))
                        {
                            value = pythonType.ToString();
                        }
                        else
                        {
                            var type = pythonType.As<Type>();
                            value = pyObject.AsManagedObject(type).ToString();
                        }
                        pythonType.Dispose();
                    }
                }
                return value;
            }
        }

        /// <summary>
        /// Tries to convert a <see cref="PyObject"/> into a managed object
        /// </summary>
        /// <remarks>This method is not working correctly for a wrapped <see cref="TimeSpan"/> instance,
        /// probably because it is a struct, using <see cref="PyObject.As{T}"/> is a valid work around.
        /// Not used here because it caused errors
        /// </remarks>
        /// <typeparam name="T">Target type of the resulting managed object</typeparam>
        /// <param name="pyObject">PyObject to be converted</param>
        /// <param name="result">Managed object </param>
        /// <param name="allowPythonDerivative">True will convert python subclasses of T</param>
        /// <returns>True if successful conversion</returns>
        public static bool TryConvert<T>(this PyObject pyObject, out T result, bool allowPythonDerivative = false)
        {
            result = default(T);
            var type = typeof(T);

            if (pyObject == null)
            {
                return true;
            }

            using (Py.GIL())
            {
                try
                {
                    // We must first check if allowPythonDerivative is true to then only return true
                    // when the PyObject is assignable from Type or IEnumerable and is a C# type
                    // wrapped in PyObject
                    if (allowPythonDerivative)
                    {
                        result = (T)pyObject.AsManagedObject(type);
                        return true;
                    }

                    // Special case: Type
                    if (typeof(Type).IsAssignableFrom(type))
                    {
                        result = (T)pyObject.AsManagedObject(type);
                        // pyObject is a C# object wrapped in PyObject, in this case return true
                        if(!pyObject.HasAttr("__name__"))
                        {
                            return true;
                        }
                        // Otherwise, pyObject is a python object that subclass a C# class, only return true if 'allowPythonDerivative'
                        var castedResult = (Type)pyObject.AsManagedObject(type);
                        var pythonName = pyObject.GetAttr("__name__").GetAndDispose<string>();
                        return pythonName == castedResult.Name;
                    }

                    // Special case: IEnumerable
                    if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        result = (T)pyObject.AsManagedObject(type);
                        return true;
                    }

                    using var pythonType = pyObject.GetPythonType();
                    var csharpType = pythonType.As<Type>();

                    if (!type.IsAssignableFrom(csharpType))
                    {
                        return false;
                    }

                    result = (T)pyObject.AsManagedObject(type);

                    // The PyObject is a Python object of a Python class that is a subclass of a C# class.
                    // In this case, we return false just because we want the actual Python object
                    // so it gets wrapped in a python wrapper, not the C# object.
                    if (result is IPythonDerivedType)
                    {
                        return false;
                    }

                    // If the python type object is just a representation of the C# type, the conversion is direct,
                    // the python object is an instance of the C# class.
                    // We can compare by reference because pythonnet caches the PyTypes and because the behavior of
                    // PyObject.Equals is not exactly what we want:
                    // e.g. type(class PyClass(CSharpClass)) == type(CSharpClass) is true in Python
                    if (PythonReferenceComparer.Instance.Equals(PyType.Get(csharpType), pythonType))
                    {
                        return true;
                    }

                    // If the PyObject type and the managed object names are the same,
                    // pyObject is a C# object wrapped in PyObject, in this case return true
                    // Otherwise, pyObject is a python object that subclass a C# class, only return true if 'allowPythonDerivative'
                    var name = (((dynamic)pythonType).__name__ as PyObject).GetAndDispose<string>();
                    return name == result.GetType().Name;
                }
                catch
                {
                    // Do not throw or log the exception.
                    // Return false as an exception means that the conversion could not be made.
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to convert a <see cref="PyObject"/> into a managed object
        /// </summary>
        /// <typeparam name="T">Target type of the resulting managed object</typeparam>
        /// <param name="pyObject">PyObject to be converted</param>
        /// <param name="result">Managed object </param>
        /// <returns>True if successful conversion</returns>
        public static bool TryConvertToDelegate<T>(this PyObject pyObject, out T result)
        {
            var type = typeof(T);

            // The PyObject is a C# object wrapped
            if (TryConvert<T>(pyObject, out result))
            {
                return true;
            }

            if (!typeof(MulticastDelegate).IsAssignableFrom(type))
            {
                throw new ArgumentException(Messages.Extensions.ConvertToDelegateCannotConverPyObjectToType("TryConvertToDelegate", type));
            }

            result = default(T);

            if (pyObject == null)
            {
                return true;
            }

            var code = string.Empty;
            var types = type.GetGenericArguments();

            using (Py.GIL())
            {
                var locals = new PyDict();
                try
                {
                    for (var i = 0; i < types.Length; i++)
                    {
                        var iString = i.ToStringInvariant();
                        code += $",t{iString}";
                        locals.SetItem($"t{iString}", types[i].ToPython());
                    }

                    locals.SetItem("pyObject", pyObject);

                    var name = type.FullName.Substring(0, type.FullName.IndexOf('`'));
                    code = $"import System; delegate = {name}[{code.Substring(1)}](pyObject)";

                    PythonEngine.Exec(code, null, locals);
                    result = (T)locals.GetItem("delegate").AsManagedObject(typeof(T));
                    locals.Dispose();
                    return true;
                }
                catch
                {
                    // Do not throw or log the exception.
                    // Return false as an exception means that the conversion could not be made.
                }
                locals.Dispose();
            }
            return false;
        }

        /// <summary>
        /// Safely convert PyObject to ManagedObject using Py.GIL Lock
        /// If no type is given it will convert the PyObject's Python Type to a ManagedObject Type
        /// in a attempt to resolve the target type to convert to.
        /// </summary>
        /// <param name="pyObject">PyObject to convert to managed</param>
        /// <param name="typeToConvertTo">The target type to convert to</param>
        /// <returns>The resulting ManagedObject</returns>
        public static dynamic SafeAsManagedObject(this PyObject pyObject, Type typeToConvertTo = null)
        {
            using (Py.GIL())
            {
                if (typeToConvertTo == null)
                {
                    typeToConvertTo = pyObject.GetPythonType().AsManagedObject(typeof(Type)) as Type;
                }

                return pyObject.AsManagedObject(typeToConvertTo);
            }
        }

        /// <summary>
        /// Converts a Python function to a managed function returning a Symbol
        /// </summary>
        /// <param name="universeFilterFunc">Universe filter function from Python</param>
        /// <returns>Function that provides <typeparamref name="T"/> and returns an enumerable of Symbols</returns>
        public static Func<IEnumerable<T>, IEnumerable<Symbol>> ConvertPythonUniverseFilterFunction<T>(this PyObject universeFilterFunc) where T : BaseData
        {
            Func<IEnumerable<T>, object> convertedFunc;
            Func<IEnumerable<T>, IEnumerable<Symbol>> filterFunc = null;

            if (universeFilterFunc != null && universeFilterFunc.TryConvertToDelegate(out convertedFunc))
            {
                filterFunc = convertedFunc.ConvertToUniverseSelectionSymbolDelegate();
            }

            return filterFunc;
        }

        /// <summary>
        /// Wraps the provided universe selection selector checking if it returned <see cref="Universe.Unchanged"/>
        /// and returns it instead, else enumerates result as <see cref="IEnumerable{Symbol}"/>
        /// </summary>
        /// <remarks>This method is a work around for the fact that currently we can not create a delegate which returns
        /// an <see cref="IEnumerable{Symbol}"/> from a python method returning an array, plus the fact that
        /// <see cref="Universe.Unchanged"/> can not be cast to an array</remarks>
        public static Func<IEnumerable<T>, IEnumerable<Symbol>> ConvertToUniverseSelectionSymbolDelegate<T>(this Func<IEnumerable<T>, object> selector) where T : BaseData
        {
            if (selector == null)
            {
                return (dataPoints) => dataPoints.Select(x => x.Symbol);
            }
            return selector.ConvertSelectionSymbolDelegate();
        }

        /// <summary>
        /// Wraps the provided universe selection selector checking if it returned <see cref="Universe.Unchanged"/>
        /// and returns it instead, else enumerates result as <see cref="IEnumerable{Symbol}"/>
        /// </summary>
        /// <remarks>This method is a work around for the fact that currently we can not create a delegate which returns
        /// an <see cref="IEnumerable{Symbol}"/> from a python method returning an array, plus the fact that
        /// <see cref="Universe.Unchanged"/> can not be cast to an array</remarks>
        public static Func<T, IEnumerable<Symbol>> ConvertSelectionSymbolDelegate<T>(this Func<T, object> selector)
        {
            return data =>
            {
                var result = selector(data);
                return ReferenceEquals(result, Universe.Unchanged)
                    ? Universe.Unchanged
                    : ((object[])result).Select(x =>
                    {
                        if (x is Symbol castedSymbol)
                        {
                            return castedSymbol;
                        }
                        return SymbolCache.TryGetSymbol((string)x, out var symbol) ? symbol : null;
                    });
            };
        }

        /// <summary>
        /// Wraps the provided universe selection selector checking if it returned <see cref="Universe.Unchanged"/>
        /// and returns it instead, else enumerates result as <see cref="IEnumerable{String}"/>
        /// </summary>
        /// <remarks>This method is a work around for the fact that currently we can not create a delegate which returns
        /// an <see cref="IEnumerable{String}"/> from a python method returning an array, plus the fact that
        /// <see cref="Universe.Unchanged"/> can not be cast to an array</remarks>
        public static Func<T, IEnumerable<string>> ConvertToUniverseSelectionStringDelegate<T>(this Func<T, object> selector)
        {
            return data =>
            {
                var result = selector(data);
                return ReferenceEquals(result, Universe.Unchanged)
                    ? Universe.Unchanged : ((object[])result).Select(x => (string)x);
            };
        }

        /// <summary>
        /// Convert a <see cref="PyObject"/> into a managed object
        /// </summary>
        /// <typeparam name="T">Target type of the resulting managed object</typeparam>
        /// <param name="pyObject">PyObject to be converted</param>
        /// <returns>Instance of type T</returns>
        public static T ConvertToDelegate<T>(this PyObject pyObject)
        {
            T result;
            if (pyObject.TryConvertToDelegate(out result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException(Messages.Extensions.ConvertToDelegateCannotConverPyObjectToType("ConvertToDelegate", typeof(T)));
            }
        }

        /// <summary>
        /// Convert a <see cref="PyObject"/> into a managed dictionary
        /// </summary>
        /// <typeparam name="TKey">Target type of the resulting dictionary key</typeparam>
        /// <typeparam name="TValue">Target type of the resulting dictionary value</typeparam>
        /// <param name="pyObject">PyObject to be converted</param>
        /// <returns>Dictionary of TValue keyed by TKey</returns>
        public static Dictionary<TKey, TValue> ConvertToDictionary<TKey, TValue>(this PyObject pyObject)
        {
            var result = new List<KeyValuePair<TKey, TValue>>();
            using (Py.GIL())
            {
                var inputType = pyObject.GetPythonType().ToString();
                var targetType = nameof(PyDict);

                try
                {
                    using (var pyDict = new PyDict(pyObject))
                    {
                        targetType = $"{typeof(TKey).Name}: {typeof(TValue).Name}";

                        foreach (PyObject item in pyDict.Items())
                        {
                            inputType = $"{item[0].GetPythonType()}: {item[1].GetPythonType()}";

                            var key = item[0].As<TKey>();
                            var value = item[1].As<TValue>();

                            result.Add(new KeyValuePair<TKey, TValue>(key, value));
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new ArgumentException(Messages.Extensions.ConvertToDictionaryFailed(inputType, targetType, e.Message), e);
                }
            }

            return result.ToDictionary();
        }

        /// <summary>
        /// Gets Enumerable of <see cref="Symbol"/> from a PyObject
        /// </summary>
        /// <param name="pyObject">PyObject containing Symbol or Array of Symbol</param>
        /// <returns>Enumerable of Symbol</returns>
        public static IEnumerable<Symbol> ConvertToSymbolEnumerable(this PyObject pyObject)
        {
            using (Py.GIL())
            {
                Exception exception = null;
                if (!PyList.IsListType(pyObject))
                {
                    // it's not a pylist try to conver directly
                    Symbol result = null;
                    try
                    {
                        // we shouldn't dispose of an object we haven't created
                        result = ConvertToSymbol(pyObject, dispose: false);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    if (result != null)
                    {
                        // happy case
                        yield return result;
                    }
                }
                else
                {
                    using var iterator = pyObject.GetIterator();
                    foreach (PyObject item in iterator)
                    {
                        Symbol result;
                        try
                        {
                            result = ConvertToSymbol(item, dispose: true);
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                            break;
                        }
                        yield return result;
                    }
                }

                // let's give it once last try, relying on pythonnet internal conversions, else throw
                if (exception != null)
                {
                    if (pyObject.TryConvert(out IEnumerable<Symbol> symbols))
                    {
                        foreach (var symbol in symbols)
                        {
                            yield return symbol;
                        }
                    }
                    else
                    {
                        throw exception;
                    }
                }
            }
        }

        /// <summary>
        /// Converts an IEnumerable to a PyList
        /// </summary>
        /// <param name="enumerable">IEnumerable object to convert</param>
        /// <returns>PyList</returns>
        public static PyList ToPyList(this IEnumerable enumerable)
        {
            using (Py.GIL())
            {
                return enumerable.ToPyListUnSafe();
            }
        }

        /// <summary>
        /// Converts an IEnumerable to a PyList
        /// </summary>
        /// <param name="enumerable">IEnumerable object to convert</param>
        /// <remarks>Requires the caller to own the GIL</remarks>
        /// <returns>PyList</returns>
        public static PyList ToPyListUnSafe(this IEnumerable enumerable)
        {
            var pyList = new PyList();
            foreach (var item in enumerable)
            {
                using (var pyObject = item.ToPython())
                {
                    pyList.Append(pyObject);
                }
            }

            return pyList;
        }

        /// <summary>
        /// Converts the numeric value of one or more enumerated constants to an equivalent enumerated string.
        /// </summary>
        /// <param name="value">Numeric value</param>
        /// <param name="pyObject">Python object that encapsulated a Enum Type</param>
        /// <returns>String that represents the enumerated object</returns>
        public static string GetEnumString(this int value, PyObject pyObject)
        {
            Type type;
            if (pyObject.TryConvert(out type))
            {
                return value.ToStringInvariant().ConvertTo(type).ToString();
            }
            else
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"GetEnumString(): {Messages.Extensions.ObjectFromPythonIsNotACSharpType(pyObject.Repr())}");
                }
            }
        }

        /// <summary>
        /// Try to create a type with a given name, if PyObject is not a CLR type. Otherwise, convert it.
        /// </summary>
        /// <param name="pyObject">Python object representing a type.</param>
        /// <param name="type">Type object</param>
        /// <returns>True if was able to create the type</returns>
        public static bool TryCreateType(this PyObject pyObject, out Type type)
        {
            if (pyObject.TryConvert(out type))
            {
                // handles pure C# types
                return true;
            }

            if (!PythonActivators.TryGetValue(pyObject.Handle, out var pythonType))
            {
                // Some examples:
                // pytype: "<class 'DropboxBaseDataUniverseSelectionAlgorithm.StockDataSource'>"
                // method: "<bound method CoarseFineFundamentalComboAlgorithm.CoarseSelectionFunction of <CoarseFineFunda..."
                // array: "[<QuantConnect.Symbol object at 0x000001EEF21ED480>]"
                if (pyObject.ToString().StartsWith("<class '", StringComparison.InvariantCulture))
                {
                    type = CreateType(pyObject);
                    return true;
                }
                return false;
            }
            type = pythonType.Type;
            return true;
        }


        /// <summary>
        /// Creates a type with a given name, if PyObject is not a CLR type. Otherwise, convert it.
        /// </summary>
        /// <param name="pyObject">Python object representing a type.</param>
        /// <returns>Type object</returns>
        public static Type CreateType(this PyObject pyObject)
        {
            Type type;
            if (pyObject.TryConvert(out type))
            {
                return type;
            }

            PythonActivator pythonType;
            if (!PythonActivators.TryGetValue(pyObject.Handle, out pythonType))
            {
                var assemblyName = pyObject.GetAssemblyName();
                var typeBuilder = AssemblyBuilder
                    .DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                    .DefineDynamicModule("MainModule")
                    // creating the type as public is required to allow 'dynamic' to be able to bind at runtime
                    .DefineType(assemblyName.Name, TypeAttributes.Class | TypeAttributes.Public, type);

                pythonType = new PythonActivator(typeBuilder.CreateType(), pyObject);

                ObjectActivator.AddActivator(pythonType.Type, pythonType.Factory);

                // Save to prevent future additions
                PythonActivators.Add(pyObject.Handle, pythonType);
            }
            return pythonType.Type;
        }

        /// <summary>
        /// Helper method to get the assembly name from a python type
        /// </summary>
        /// <param name="pyObject">Python object pointing to the python type. <see cref="PyObject.GetPythonType"/></param>
        /// <returns>The python type assembly name</returns>
        public static AssemblyName GetAssemblyName(this PyObject pyObject)
        {
            using (Py.GIL())
            {
                return new AssemblyName(pyObject.Repr().Split('\'')[1]);
            }
        }

        /// <summary>
        /// Performs on-line batching of the specified enumerator, emitting chunks of the requested batch size
        /// </summary>
        /// <typeparam name="T">The enumerable item type</typeparam>
        /// <param name="enumerable">The enumerable to be batched</param>
        /// <param name="batchSize">The number of items per batch</param>
        /// <returns>An enumerable of lists</returns>
        public static IEnumerable<List<T>> BatchBy<T>(this IEnumerable<T> enumerable, int batchSize)
        {
            using (var enumerator = enumerable.GetEnumerator())
            {
                List<T> list = null;
                while (enumerator.MoveNext())
                {
                    if (list == null)
                    {
                        list = new List<T> {enumerator.Current};
                    }
                    else if (list.Count < batchSize)
                    {
                        list.Add(enumerator.Current);
                    }
                    else
                    {
                        yield return list;
                        list = new List<T> {enumerator.Current};
                    }
                }

                if (list?.Count > 0)
                {
                    yield return list;
                }
            }
        }

        /// <summary>
        /// Safely blocks until the specified task has completed executing
        /// </summary>
        /// <typeparam name="TResult">The task's result type</typeparam>
        /// <param name="task">The task to be awaited</param>
        /// <returns>The result of the task</returns>
        public static TResult SynchronouslyAwaitTaskResult<TResult>(this Task<TResult> task)
        {
            return task.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Safely blocks until the specified task has completed executing
        /// </summary>
        /// <param name="task">The task to be awaited</param>
        /// <returns>The result of the task</returns>
        public static void SynchronouslyAwaitTask(this Task task)
        {
            task.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Safely blocks until the specified task has completed executing
        /// </summary>
        /// <param name="task">The task to be awaited</param>
        /// <returns>The result of the task</returns>
        public static T SynchronouslyAwaitTask<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Convert dictionary to query string
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public static string ToQueryString(this IDictionary<string, object> pairs)
        {
            return string.Join("&", pairs.Select(pair => $"{pair.Key}={pair.Value}"));
        }

        /// <summary>
        /// Returns a new string in which specified ending in the current instance is removed.
        /// </summary>
        /// <param name="s">original string value</param>
        /// <param name="ending">the string to be removed</param>
        /// <returns></returns>
        public static string RemoveFromEnd(this string s, string ending)
        {
            if (s.EndsWith(ending, StringComparison.InvariantCulture))
            {
                return s.Substring(0, s.Length - ending.Length);
            }
            else
            {
                return s;
            }
        }

        /// <summary>
        /// Returns a new string in which specified start in the current instance is removed.
        /// </summary>
        /// <param name="s">original string value</param>
        /// <param name="start">the string to be removed</param>
        /// <returns>Substring with start removed</returns>
        public static string RemoveFromStart(this string s, string start)
        {
            if (!string.IsNullOrEmpty(s) && !string.IsNullOrEmpty(start) && s.StartsWith(start, StringComparison.InvariantCulture))
            {
                return s.Substring(start.Length);
            }
            else
            {
                return s;
            }
        }

        /// <summary>
        /// Helper method to determine symbol for a live subscription
        /// </summary>
        /// <remarks>Useful for continuous futures where we subscribe to the underlying</remarks>
        public static bool TryGetLiveSubscriptionSymbol(this Symbol symbol, out Symbol mapped)
        {
            mapped = null;
            if (symbol.SecurityType == SecurityType.Future && symbol.IsCanonical() && symbol.HasUnderlying)
            {
                mapped = symbol.Underlying;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the delisting date for the provided Symbol
        /// </summary>
        /// <param name="symbol">The symbol to lookup the last trading date</param>
        /// <param name="mapFile">Map file to use for delisting date. Defaults to SID.DefaultDate if no value is passed and is equity.</param>
        /// <returns></returns>
        public static DateTime GetDelistingDate(this Symbol symbol, MapFile mapFile = null)
        {
            if (symbol.IsCanonical())
            {
                return Time.EndOfTime;
            }
            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Option:
                    return OptionSymbol.GetLastDayOfTrading(symbol);
                case SecurityType.FutureOption:
                    return FutureOptionSymbol.GetLastDayOfTrading(symbol);
                case SecurityType.Future:
                case SecurityType.IndexOption:
                    return symbol.ID.Date;
                default:
                    return mapFile?.DelistingDate ?? Time.EndOfTime;
            }
        }

        /// <summary>
        /// Helper method to determine if a given symbol is of custom data
        /// </summary>
        public static bool IsCustomDataType<T>(this Symbol symbol)
        {
            return symbol.SecurityType == SecurityType.Base
                && SecurityIdentifier.TryGetCustomDataType(symbol.ID.Symbol, out var type)
                && type.Equals(typeof(T).Name, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Helper method that will return a back month, with future expiration, future contract based on the given offset
        /// </summary>
        /// <param name="symbol">The none canonical future symbol</param>
        /// <param name="offset">The quantity of contracts to move into the future expiration chain</param>
        /// <returns>A new future expiration symbol instance</returns>
        public static Symbol AdjustSymbolByOffset(this Symbol symbol, uint offset)
        {
            if (symbol.SecurityType != SecurityType.Future || symbol.IsCanonical())
            {
                throw new InvalidOperationException(Messages.Extensions.ErrorAdjustingSymbolByOffset);
            }

            var expiration = symbol.ID.Date;
            for (var i = 0; i < offset; i++)
            {
                var expiryFunction = FuturesExpiryFunctions.FuturesExpiryFunction(symbol);
                DateTime newExpiration;
                // for the current expiration we add a month to get the next one
                var monthOffset = 0;
                do
                {
                    monthOffset++;
                    newExpiration = expiryFunction(expiration.AddMonths(monthOffset)).Date;
                } while (newExpiration <= expiration);

                expiration = newExpiration;
                symbol = Symbol.CreateFuture(symbol.ID.Symbol, symbol.ID.Market, newExpiration);
            }

            return symbol;
        }

        /// <summary>
        /// Helper method to unsubscribe a given configuration, handling any required mapping
        /// </summary>
        public static void UnsubscribeWithMapping(this IDataQueueHandler dataQueueHandler, SubscriptionDataConfig dataConfig)
        {
            if (dataConfig.Symbol.TryGetLiveSubscriptionSymbol(out var mappedSymbol))
            {
                dataConfig = new SubscriptionDataConfig(dataConfig, symbol: mappedSymbol, mappedConfig: true);
            }
            dataQueueHandler.Unsubscribe(dataConfig);
        }

        /// <summary>
        /// Helper method to subscribe a given configuration, handling any required mapping
        /// </summary>
        public static IEnumerator<BaseData> SubscribeWithMapping(this IDataQueueHandler dataQueueHandler,
            SubscriptionDataConfig dataConfig,
            EventHandler newDataAvailableHandler,
            Func<SubscriptionDataConfig, bool> isExpired,
            out SubscriptionDataConfig subscribedConfig)
        {
            subscribedConfig = dataConfig;
            if (dataConfig.Symbol.TryGetLiveSubscriptionSymbol(out var mappedSymbol))
            {
                subscribedConfig = new SubscriptionDataConfig(dataConfig, symbol: mappedSymbol, mappedConfig: true);
            }

            // during warmup we might get requested to add some asset which has already expired in which case the live enumerator will be empty
            IEnumerator<BaseData> result = null;
            if (!isExpired(subscribedConfig))
            {
                result = dataQueueHandler.Subscribe(subscribedConfig, newDataAvailableHandler);
            }
            else
            {
                Log.Trace($"SubscribeWithMapping(): skip live subscription for expired asset {subscribedConfig}");
            }
            return result ?? Enumerable.Empty<BaseData>().GetEnumerator();
        }

        /// <summary>
        /// Helper method to stream read lines from a file
        /// </summary>
        /// <param name="dataProvider">The data provider to use</param>
        /// <param name="file">The file path to read from</param>
        /// <returns>Enumeration of lines in file</returns>
        public static IEnumerable<string> ReadLines(this IDataProvider dataProvider, string file)
        {
            if(dataProvider == null)
            {
                throw new ArgumentException(Messages.Extensions.NullDataProvider);
            }
            var stream = dataProvider.Fetch(file);
            if (stream == null)
            {
                yield break;
            }

            using (var streamReader = new StreamReader(stream))
            {
                string line;
                do
                {
                    line = streamReader.ReadLine();
                    if (line != null)
                    {
                        yield return line;
                    }
                }
                while (line != null);
            }
        }

        /// <summary>
        /// Scale data based on factor function
        /// </summary>
        /// <param name="data">Data to Adjust</param>
        /// <param name="factorFunc">Function to factor prices by</param>
        /// <param name="volumeFactor">Factor to multiply volume/askSize/bidSize/quantity by</param>
        /// <param name="factor">Price scale</param>
        /// <param name="sumOfDividends">The current dividend sum</param>
        /// <remarks>Volume values are rounded to the nearest integer, lot size purposefully not considered
        /// as scaling only applies to equities</remarks>
        public static BaseData Scale(this BaseData data, Func<decimal, decimal, decimal, decimal> factorFunc, decimal volumeFactor, decimal factor, decimal sumOfDividends)
        {
            switch (data.DataType)
            {
                case MarketDataType.TradeBar:
                    var tradeBar = data as TradeBar;
                    if (tradeBar != null)
                    {
                        tradeBar.Open = factorFunc(tradeBar.Open, factor, sumOfDividends);
                        tradeBar.High = factorFunc(tradeBar.High, factor, sumOfDividends);
                        tradeBar.Low = factorFunc(tradeBar.Low, factor, sumOfDividends);
                        tradeBar.Close = factorFunc(tradeBar.Close, factor, sumOfDividends);
                        tradeBar.Volume = Math.Round(tradeBar.Volume * volumeFactor);
                    }
                    break;
                case MarketDataType.Tick:
                    var securityType = data.Symbol.SecurityType;
                    if (securityType != SecurityType.Equity &&
                        securityType != SecurityType.Future &&
                        !securityType.IsOption())
                    {
                        break;
                    }

                    var tick = data as Tick;
                    if (tick == null || tick.TickType == TickType.OpenInterest)
                    {
                        break;
                    }

                    if (tick.TickType == TickType.Trade)
                    {
                        tick.Value = factorFunc(tick.Value, factor, sumOfDividends);
                        tick.Quantity = Math.Round(tick.Quantity * volumeFactor);
                        break;
                    }

                    tick.BidPrice = tick.BidPrice != 0 ? factorFunc(tick.BidPrice, factor, sumOfDividends) : 0;
                    tick.BidSize = Math.Round(tick.BidSize * volumeFactor);
                    tick.AskPrice = tick.AskPrice != 0 ? factorFunc(tick.AskPrice, factor, sumOfDividends) : 0;
                    tick.AskSize = Math.Round(tick.AskSize * volumeFactor);

                    if (tick.BidPrice == 0)
                    {
                        tick.Value = tick.AskPrice;
                        break;
                    }
                    if (tick.AskPrice == 0)
                    {
                        tick.Value = tick.BidPrice;
                        break;
                    }

                    tick.Value = (tick.BidPrice + tick.AskPrice) / 2m;
                    break;
                case MarketDataType.QuoteBar:
                    var quoteBar = data as QuoteBar;
                    if (quoteBar != null)
                    {
                        if (quoteBar.Ask != null)
                        {
                            quoteBar.Ask.Open = factorFunc(quoteBar.Ask.Open, factor, sumOfDividends);
                            quoteBar.Ask.High = factorFunc(quoteBar.Ask.High, factor, sumOfDividends);
                            quoteBar.Ask.Low = factorFunc(quoteBar.Ask.Low, factor, sumOfDividends);
                            quoteBar.Ask.Close = factorFunc(quoteBar.Ask.Close, factor, sumOfDividends);
                        }
                        if (quoteBar.Bid != null)
                        {
                            quoteBar.Bid.Open = factorFunc(quoteBar.Bid.Open, factor, sumOfDividends);
                            quoteBar.Bid.High = factorFunc(quoteBar.Bid.High, factor, sumOfDividends);
                            quoteBar.Bid.Low = factorFunc(quoteBar.Bid.Low, factor, sumOfDividends);
                            quoteBar.Bid.Close = factorFunc(quoteBar.Bid.Close, factor, sumOfDividends);
                        }
                        quoteBar.Value = quoteBar.Close;
                        quoteBar.LastAskSize = Math.Round(quoteBar.LastAskSize * volumeFactor);
                        quoteBar.LastBidSize = Math.Round(quoteBar.LastBidSize * volumeFactor);
                    }
                    break;
                case MarketDataType.Auxiliary:
                case MarketDataType.Base:
                case MarketDataType.OptionChain:
                case MarketDataType.FuturesChain:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return data;
        }

        /// <summary>
        /// Normalize prices based on configuration
        /// </summary>
        /// <param name="data">Data to be normalized</param>
        /// <param name="factor">Price scale</param>
        /// <param name="normalizationMode">The price scaling normalization mode</param>
        /// <param name="sumOfDividends">The current dividend sum</param>
        /// <returns>The provided data point adjusted</returns>
        public static BaseData Normalize(this BaseData data, decimal factor, DataNormalizationMode normalizationMode, decimal sumOfDividends)
        {
            switch (normalizationMode)
            {
                case DataNormalizationMode.Adjusted:
                case DataNormalizationMode.SplitAdjusted:
                case DataNormalizationMode.ScaledRaw:
                    return data?.Scale(TimesFactor, 1 / factor, factor, decimal.Zero);
                case DataNormalizationMode.TotalReturn:
                    return data.Scale(TimesFactor, 1 / factor, factor, sumOfDividends);

                case DataNormalizationMode.BackwardsRatio:
                    return data.Scale(TimesFactor, 1, factor, decimal.Zero);
                case DataNormalizationMode.BackwardsPanamaCanal:
                    return data.Scale(AdditionFactor, 1, factor, decimal.Zero);
                case DataNormalizationMode.ForwardPanamaCanal:
                    return data.Scale(AdditionFactor, 1, factor, decimal.Zero);

                case DataNormalizationMode.Raw:
                default:
                    return data;
            }
        }

        /// <summary>
        /// Applies a times factor. We define this so we don't need to create it constantly
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal TimesFactor(decimal target, decimal factor, decimal sumOfDividends)
        {
            return target * factor + sumOfDividends;
        }

        /// <summary>
        /// Applies an addition factor. We define this so we don't need to create it constantly
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal AdditionFactor(decimal target, decimal factor, decimal _)
        {
            return target + factor;
        }

        /// <summary>
        /// Helper method to determine if price scales need an update based on the given data point
        /// </summary>
        public static DateTime GetUpdatePriceScaleFrontier(this BaseData data)
        {
            if (data != null)
            {
                var priceScaleFrontier = data.Time;
                if (data.Time.Date != data.EndTime.Date && data.EndTime.TimeOfDay > TimeSpan.Zero)
                {
                    // if the data point goes from one day to another after midnight we use EndTime, this is due to differences between 'data' and 'exchage' time zone,
                    // for example: NYMEX future CL 'data' TZ is UTC while 'exchange' TZ is NY, so daily bars go from 8PM 'X day' to 8PM 'X+1 day'. Note that the data
                    // in the daily bar itself is filtered by exchange open, so it has data from 09:30 'X+1 day' to 17:00 'X+1 day' as expected.
                    // A potential solution to avoid the need of this check is to adjust the daily data time zone to match the exchange time zone, following this example above
                    // the daily bar would go from midnight X+1 day to midnight X+2
                    // TODO: see related issue https://github.com/QuantConnect/Lean/issues/6964 which would avoid the need for this
                    priceScaleFrontier = data.EndTime;
                }
                return priceScaleFrontier;
            }
            return DateTime.MinValue;
        }

        /// <summary>
        /// Thread safe concurrent dictionary order by implementation by using <see cref="SafeEnumeration{TSource,TKey}"/>
        /// </summary>
        /// <remarks>See https://stackoverflow.com/questions/47630824/is-c-sharp-linq-orderby-threadsafe-when-used-with-concurrentdictionarytkey-tva</remarks>
        public static IOrderedEnumerable<KeyValuePair<TSource, TKey>> OrderBySafe<TSource, TKey>(
            this ConcurrentDictionary<TSource, TKey> source, Func<KeyValuePair<TSource, TKey>, TSource> keySelector
            )
        {
            return source.SafeEnumeration().OrderBy(keySelector);
        }

        /// <summary>
        /// Thread safe concurrent dictionary order by implementation by using <see cref="SafeEnumeration{TSource,TKey}"/>
        /// </summary>
        /// <remarks>See https://stackoverflow.com/questions/47630824/is-c-sharp-linq-orderby-threadsafe-when-used-with-concurrentdictionarytkey-tva</remarks>
        public static IOrderedEnumerable<KeyValuePair<TSource, TKey>> OrderBySafe<TSource, TKey>(
            this ConcurrentDictionary<TSource, TKey> source, Func<KeyValuePair<TSource, TKey>, TKey> keySelector
            )
        {
            return source.SafeEnumeration().OrderBy(keySelector);
        }

        /// <summary>
        /// Force concurrent dictionary enumeration using a thread safe implementation
        /// </summary>
        /// <remarks>See https://stackoverflow.com/questions/47630824/is-c-sharp-linq-orderby-threadsafe-when-used-with-concurrentdictionarytkey-tva</remarks>
        public static IEnumerable<KeyValuePair<TSource, TKey>> SafeEnumeration<TSource, TKey>(
            this ConcurrentDictionary<TSource, TKey> source)
        {
            foreach (var kvp in source)
            {
                yield return kvp;
            }
        }

        /// <summary>
        /// Helper method to determine the right data mapping mode to use by default
        /// </summary>
        public static DataMappingMode GetUniverseNormalizationModeOrDefault(this UniverseSettings universeSettings, SecurityType securityType, string market)
        {
            switch (securityType)
            {
                case SecurityType.Future:
                    if ((universeSettings.DataMappingMode == DataMappingMode.OpenInterest
                        || universeSettings.DataMappingMode == DataMappingMode.OpenInterestAnnual)
                        && (market == Market.HKFE || market == Market.EUREX || market == Market.ICE))
                    {
                        // circle around default OI for currently no OI available data
                        return DataMappingMode.LastTradingDay;
                    }
                    return universeSettings.DataMappingMode;
                default:
                    return universeSettings.DataMappingMode;
            }
        }

        /// <summary>
        /// Helper method to determine the right data normalization mode to use by default
        /// </summary>
        public static DataNormalizationMode GetUniverseNormalizationModeOrDefault(this UniverseSettings universeSettings, SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Future:
                    if (universeSettings.DataNormalizationMode is DataNormalizationMode.BackwardsRatio
                        or DataNormalizationMode.BackwardsPanamaCanal or DataNormalizationMode.ForwardPanamaCanal
                        or DataNormalizationMode.Raw)
                    {
                        return universeSettings.DataNormalizationMode;
                    }
                    return DataNormalizationMode.BackwardsRatio;
                default:
                    return universeSettings.DataNormalizationMode;
            }
        }

        /// <summary>
        /// Returns a hex string of the byte array.
        /// </summary>
        /// <param name="source">the byte array to be represented as string</param>
        /// <returns>A new string containing the items in the enumerable</returns>
        public static string ToHexString(this byte[] source)
        {
            if (source == null || source.Length == 0)
            {
                throw new ArgumentException(Messages.Extensions.NullOrEmptySourceToConvertToHexString);
            }

            var hex = new StringBuilder(source.Length * 2);
            foreach (var b in source)
            {
                hex.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
            }

            return hex.ToString();
        }

        /// <summary>
        /// Gets the option exercise order direction resulting from the specified <paramref name="right"/> and
        /// whether or not we wrote the option (<paramref name="isShort"/> is <code>true</code>) or bought to
        /// option (<paramref name="isShort"/> is <code>false</code>)
        /// </summary>
        /// <param name="right">The option right</param>
        /// <param name="isShort">True if we wrote the option, false if we purchased the option</param>
        /// <returns>The order direction resulting from an exercised option</returns>
        public static OrderDirection GetExerciseDirection(this OptionRight right, bool isShort)
        {
            switch (right)
            {
                case OptionRight.Call:
                    return isShort ? OrderDirection.Sell : OrderDirection.Buy;
                default:
                    return isShort ? OrderDirection.Buy : OrderDirection.Sell;
            }
        }

        /// <summary>
        /// Gets the <see cref="OrderDirection"/> for the specified <paramref name="quantity"/>
        /// </summary>
        public static OrderDirection GetOrderDirection(decimal quantity)
        {
            var sign = Math.Sign(quantity);
            switch (sign)
            {
                case 1: return OrderDirection.Buy;
                case 0: return OrderDirection.Hold;
                case -1: return OrderDirection.Sell;
                default:
                    throw new ApplicationException(
                        $"The skies are falling and the oceans are rising! Math.Sign({quantity}) returned {sign} :/"
                    );
            }
        }

        /// <summary>
        /// Helper method to process an algorithms security changes, will add and remove securities according to them
        /// </summary>
        public static void ProcessSecurityChanges(this IAlgorithm algorithm, SecurityChanges securityChanges)
        {
            foreach (var security in securityChanges.AddedSecurities)
            {
                // uses TryAdd, so don't need to worry about duplicates here
                algorithm.Securities.Add(security);

                if (security.Type == SecurityType.Index && !(security as Securities.Index.Index).ManualSetIsTradable)
                {
                    continue;
                }

                security.IsTradable = true;
            }

            var activeSecurities = algorithm.UniverseManager.ActiveSecurities;
            foreach (var security in securityChanges.RemovedSecurities)
            {
                if (!activeSecurities.ContainsKey(security.Symbol))
                {
                    security.IsTradable = false;
                }
            }
        }

        /// <summary>
        /// Helper method to set an algorithm runtime exception in a normalized fashion
        /// </summary>
        public static void SetRuntimeError(this IAlgorithm algorithm, Exception exception, string context)
        {
            Log.Error(exception, $"Extensions.SetRuntimeError(): {Messages.Extensions.RuntimeError(algorithm, context)}");
            exception = StackExceptionInterpreter.Instance.Value.Interpret(exception);
            algorithm.RunTimeError = exception;
            algorithm.SetStatus(AlgorithmStatus.RuntimeError);
        }

        /// <summary>
        /// Creates a <see cref="OptionChainUniverse"/> for a given symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <param name="symbol">Symbol of the option</param>
        /// <param name="filter">The option filter to use</param>
        /// <param name="universeSettings">The universe settings, will use algorithm settings if null</param>
        /// <returns><see cref="OptionChainUniverse"/> for the given symbol</returns>
        public static OptionChainUniverse CreateOptionChain(this IAlgorithm algorithm, Symbol symbol, PyObject filter, UniverseSettings universeSettings = null)
        {
            var result = CreateOptionChain(algorithm, symbol, out var option, universeSettings);
            option.SetFilter(filter);
            return result;
        }

        /// <summary>
        /// Creates a <see cref="OptionChainUniverse"/> for a given symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <param name="symbol">Symbol of the option</param>
        /// <param name="filter">The option filter to use</param>
        /// <param name="universeSettings">The universe settings, will use algorithm settings if null</param>
        /// <returns><see cref="OptionChainUniverse"/> for the given symbol</returns>
        public static OptionChainUniverse CreateOptionChain(this IAlgorithm algorithm, Symbol symbol, Func<OptionFilterUniverse, OptionFilterUniverse> filter, UniverseSettings universeSettings = null)
        {
            var result = CreateOptionChain(algorithm, symbol, out var option, universeSettings);
            option.SetFilter(filter);
            return result;
        }

        /// <summary>
        /// Creates a <see cref="OptionChainUniverse"/> for a given symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <param name="symbol">Symbol of the option</param>
        /// <param name="universeSettings">The universe settings, will use algorithm settings if null</param>
        /// <returns><see cref="OptionChainUniverse"/> for the given symbol</returns>
        private static OptionChainUniverse CreateOptionChain(this IAlgorithm algorithm, Symbol symbol, out Option option, UniverseSettings universeSettings = null)
        {
            if (!symbol.SecurityType.IsOption())
            {
                throw new ArgumentException(Messages.Extensions.CreateOptionChainRequiresOptionSymbol);
            }

            // resolve defaults if not specified
            var settings = universeSettings ?? algorithm.UniverseSettings;

            option = (Option)algorithm.AddSecurity(symbol.Canonical, settings.Resolution, settings.FillForward, settings.Leverage, settings.ExtendedMarketHours);

            return (OptionChainUniverse)algorithm.UniverseManager.Values.Single(universe => universe.Configuration.Symbol == symbol.Canonical);
        }

        /// <summary>
        /// Creates a <see cref="FuturesChainUniverse"/> for a given symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <param name="symbol">Symbol of the future</param>
        /// <param name="filter">The future filter to use</param>
        /// <param name="universeSettings">The universe settings, will use algorithm settings if null</param>
        public static IEnumerable<Universe> CreateFutureChain(this IAlgorithm algorithm, Symbol symbol, PyObject filter, UniverseSettings universeSettings = null)
        {
            var result = CreateFutureChain(algorithm, symbol, out var future, universeSettings);
            future.SetFilter(filter);
            return result;
        }

        /// <summary>
        /// Creates a <see cref="FuturesChainUniverse"/> for a given symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <param name="symbol">Symbol of the future</param>
        /// <param name="filter">The future filter to use</param>
        /// <param name="universeSettings">The universe settings, will use algorithm settings if null</param>
        public static IEnumerable<Universe> CreateFutureChain(this IAlgorithm algorithm, Symbol symbol, Func<FutureFilterUniverse, FutureFilterUniverse> filter, UniverseSettings universeSettings = null)
        {
            var result = CreateFutureChain(algorithm, symbol, out var future, universeSettings);
            future.SetFilter(filter);
            return result;
        }

        /// <summary>
        /// Creates a <see cref="FuturesChainUniverse"/> for a given symbol
        /// </summary>
        private static IEnumerable<Universe> CreateFutureChain(this IAlgorithm algorithm, Symbol symbol, out Future future, UniverseSettings universeSettings = null)
        {
            if (symbol.SecurityType != SecurityType.Future)
            {
                throw new ArgumentException(Messages.Extensions.CreateFutureChainRequiresFutureSymbol);
            }

            // resolve defaults if not specified
            var settings = universeSettings ?? algorithm.UniverseSettings;

            var dataNormalizationMode = settings.GetUniverseNormalizationModeOrDefault(symbol.SecurityType);

            future = (Future)algorithm.AddSecurity(symbol.Canonical, settings.Resolution, settings.FillForward, settings.Leverage, settings.ExtendedMarketHours,
                settings.DataMappingMode, dataNormalizationMode, settings.ContractDepthOffset);

            // let's yield back both the future chain and the continuous future universe
            return algorithm.UniverseManager.Values.Where(universe => universe.Configuration.Symbol == symbol.Canonical || ContinuousContractUniverse.CreateSymbol(symbol.Canonical) == universe.Configuration.Symbol);
        }

        private static bool _notifiedUniverseSettingsUsed;
        private static readonly HashSet<SecurityType> _supportedSecurityTypes = new()
        {
            SecurityType.Equity,
            SecurityType.Forex,
            SecurityType.Cfd,
            SecurityType.Option,
            SecurityType.Future,
            SecurityType.FutureOption,
            SecurityType.IndexOption,
            SecurityType.Crypto,
            SecurityType.CryptoFuture
        };

        /// <summary>
        /// Gets the security for the specified symbol from the algorithm's securities collection.
        /// In case the security is not found, it will be created using the <see cref="IAlgorithm.UniverseSettings"/>
        /// and a best effort configuration setup.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="symbol">The symbol which security is being looked up</param>
        /// <param name="security">The found or added security instance</param>
        /// <param name="onError">Callback to invoke in case of unsupported security type</param>
        /// <returns>True if the security was found or added</returns>
        public static bool GetOrAddUnrequestedSecurity(this IAlgorithm algorithm, Symbol symbol, out Security security,
            Action<IReadOnlyCollection<SecurityType>> onError = null)
        {
            if (!algorithm.Securities.TryGetValue(symbol, out security))
            {
                if (!_supportedSecurityTypes.Contains(symbol.SecurityType))
                {
                    Log.Error("GetOrAddUnrequestedSecurity(): Unsupported security type: " + symbol.SecurityType + "-" + symbol.Value);
                    onError?.Invoke(_supportedSecurityTypes);
                    return false;
                }

                var resolution = algorithm.UniverseSettings.Resolution;
                var fillForward = algorithm.UniverseSettings.FillForward;
                var leverage = algorithm.UniverseSettings.Leverage;
                var extendedHours = algorithm.UniverseSettings.ExtendedMarketHours;

                if (!_notifiedUniverseSettingsUsed)
                {
                    // let's just send the message once
                    _notifiedUniverseSettingsUsed = true;

                    var leverageMsg = $" Leverage = {leverage};";
                    if (leverage == Security.NullLeverage)
                    {
                        leverageMsg = $" Leverage = default;";
                    }
                    algorithm.Debug($"Will use UniverseSettings for automatically added securities for open orders and holdings. UniverseSettings:" +
                        $" Resolution = {resolution};{leverageMsg} FillForward = {fillForward}; ExtendedHours = {extendedHours}");
                }

                Log.Trace("GetOrAddUnrequestedSecurity(): Adding unrequested security: " + symbol.Value);

                if (symbol.SecurityType.IsOption())
                {
                    // add current option contract to the system
                    security = algorithm.AddOptionContract(symbol, resolution, fillForward, leverage, extendedHours);
                }
                else if (symbol.SecurityType == SecurityType.Future)
                {
                    // add current future contract to the system
                    security = algorithm.AddFutureContract(symbol, resolution, fillForward, leverage, extendedHours);
                }
                else
                {
                    // for items not directly requested set leverage to 1 and at the min resolution
                    security = algorithm.AddSecurity(symbol.SecurityType, symbol.Value, resolution, symbol.ID.Market, fillForward, leverage, extendedHours);
                }
            }
            return true;
        }

        /// <summary>
        /// Inverts the specified <paramref name="right"/>
        /// </summary>
        public static OptionRight Invert(this OptionRight right)
        {
            switch (right)
            {
                case OptionRight.Call: return OptionRight.Put;
                case OptionRight.Put:  return OptionRight.Call;
                default:
                    throw new ArgumentOutOfRangeException(nameof(right), right, null);
            }
        }

        /// <summary>
        /// Compares two values using given operator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="op">Comparison operator</param>
        /// <param name="arg1">The first value</param>
        /// <param name="arg2">The second value</param>
        /// <returns>Returns true if its left-hand operand meets the operator value to its right-hand operand, false otherwise</returns>
        public static bool Compare<T>(this ComparisonOperatorTypes op, T arg1, T arg2) where T : IComparable
        {
            return ComparisonOperator.Compare(op, arg1, arg2);
        }

        /// <summary>
        /// Converts a <see cref="Data.HistoryRequest" /> instance to a <see cref="SubscriptionDataConfig"/> instance
        /// </summary>
        /// <param name="request">History request</param>
        /// <param name="isInternalFeed">
        /// Set to true if this subscription is added for the sole purpose of providing currency conversion rates,
        /// setting this flag to true will prevent the data from being sent into the algorithm's OnData methods
        /// </param>
        /// <param name="isFilteredSubscription">True if this subscription should have filters applied to it (market hours/user filters from security), false otherwise</param>
        /// <returns>Subscription data configuration</returns>
        public static SubscriptionDataConfig ToSubscriptionDataConfig(this Data.HistoryRequest request, bool isInternalFeed = false, bool isFilteredSubscription = true)
        {
            return new SubscriptionDataConfig(request.DataType,
                request.Symbol,
                request.Resolution,
                request.DataTimeZone,
                request.ExchangeHours.TimeZone,
                request.FillForwardResolution.HasValue,
                request.IncludeExtendedMarketHours,
                isInternalFeed,
                request.IsCustomData,
                request.TickType,
                isFilteredSubscription,
                request.DataNormalizationMode,
                request.DataMappingMode,
                request.ContractDepthOffset
            );
        }

        /// <summary>
        /// Centralized logic used at the top of the subscription enumerator stacks to determine if we should emit base data points
        /// based on the configuration for this subscription and the type of data we are handling.
        ///
        /// Currently we only want to emit split/dividends/delisting events for non internal <see cref="TradeBar"/> configurations
        /// this last part is because equities also have <see cref="QuoteBar"/> subscriptions which will also subscribe to the
        /// same aux events and we don't want duplicate emits of these events in the TimeSliceFactory
        /// </summary>
        /// <remarks>The "TimeSliceFactory" does not allow for multiple dividends/splits per symbol in the same time slice
        /// but we don't want to rely only on that to filter out duplicated aux data so we use this at the top of
        /// our data enumerator stacks to define what subscription should emit this data.</remarks>
        /// <remarks>We use this function to filter aux data at the top of the subscription enumerator stack instead of
        /// stopping the subscription stack from subscribing to aux data at the bottom because of a
        /// dependency with the FF enumerators requiring that they receive aux data to properly handle delistings.
        /// Otherwise we would have issues with delisted symbols continuing to fill forward after expiry/delisting.
        /// Reference PR #5485 and related issues for more.</remarks>
        public static bool ShouldEmitData(this SubscriptionDataConfig config, BaseData data, bool isUniverse = false)
        {
            // For now we are only filtering Auxiliary data; so if its another type just return true or if it's a margin interest rate which we want to emit always
            if (data.DataType != MarketDataType.Auxiliary)
            {
                return true;
            }

            // This filter does not apply to auxiliary data outside of delisting/splits/dividends so lets those emit
            var type = data.GetType();
            var expectedType = type.IsAssignableTo(config.Type);

            // Check our config type first to be lazy about using data.GetType() unless required
            var configTypeFilter = (config.Type == typeof(TradeBar) || config.Type == typeof(ZipEntryName) ||
                config.Type == typeof(Tick) && config.TickType == TickType.Trade || config.IsCustomData);

            if (!configTypeFilter)
            {
                return expectedType;
            }

            // We don't want to pump in any data to `Universe.SelectSymbols(...)` if the
            // type is not configured to be consumed by the universe. This change fixes
            // a case where a `SymbolChangedEvent` was being passed to an ETF constituent universe
            // for filtering/selection, and would result in either a runtime error
            // if casting into the expected type explicitly, or call the filter function with
            // no data being provided, resulting in all universe Symbols being de-selected.
            if (isUniverse && !expectedType)
            {
                return (data as Delisting)?.Type == DelistingType.Delisted;
            }

            // We let delistings through. We need to emit delistings for all subscriptions, even internals like
            // continuous futures mapped contracts. For instance, an algorithm might hold a position for a mapped
            // contract and then the continuous future is mapped to a different contract. If the previously mapped
            // contract is delisted, we need to let the delisting through so that positions are closed out and the
            // security is removed from the algorithm and marked as delisted and non-tradable.
            if (!(type == typeof(Split) || type == typeof(Dividend)))
            {
                return true;
            }

            // If we made it here then only filter it if its an InternalFeed
            return !config.IsInternalFeed;
        }

        /// <summary>
        /// Gets the <see cref="OrderDirection"/> that corresponds to the specified <paramref name="side"/>
        /// </summary>
        /// <param name="side">The position side to be converted</param>
        /// <returns>The order direction that maps from the provided position side</returns>
        public static OrderDirection ToOrderDirection(this PositionSide side)
        {
            switch (side)
            {
                case PositionSide.Short: return OrderDirection.Sell;
                case PositionSide.None: return OrderDirection.Hold;
                case PositionSide.Long: return OrderDirection.Buy;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        /// <summary>
        /// Determines if an order with the specified <paramref name="direction"/> would close a position with the
        /// specified <paramref name="side"/>
        /// </summary>
        /// <param name="direction">The direction of the order, buy/sell</param>
        /// <param name="side">The side of the position, long/short</param>
        /// <returns>True if the order direction would close the position, otherwise false</returns>
        public static bool Closes(this OrderDirection direction, PositionSide side)
        {
            switch (side)
            {
                case PositionSide.Short:
                    switch (direction)
                    {
                        case OrderDirection.Buy: return true;
                        case OrderDirection.Sell: return false;
                        case OrderDirection.Hold: return false;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                    }

                case PositionSide.Long:
                    switch (direction)
                    {
                        case OrderDirection.Buy: return false;
                        case OrderDirection.Sell: return true;
                        case OrderDirection.Hold: return false;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                    }

                case PositionSide.None:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        /// <summary>
        /// Determines if the two lists are equal, including all items at the same indices.
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="left">The left list</param>
        /// <param name="right">The right list</param>
        /// <returns>True if the two lists have the same counts and items at each index evaluate as equal</returns>
        public static bool ListEquals<T>(this IReadOnlyList<T> left, IReadOnlyList<T> right)
        {
            var count = left.Count;
            if (count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Computes a deterministic hash code based on the items in the list. This hash code is dependent on the
        /// ordering of items.
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="list">The list</param>
        /// <returns>A hash code dependent on the ordering of elements in the list</returns>
        public static int GetListHashCode<T>(this IReadOnlyList<T> list)
        {
            unchecked
            {
                var hashCode = 17;
                for (int i = 0; i < list.Count; i++)
                {
                    hashCode += (hashCode * 397) ^ list[i].GetHashCode();
                }

                return hashCode;
            }
        }

        /// <summary>
        /// Determine if this SecurityType requires mapping
        /// </summary>
        /// <param name="symbol">Type to check</param>
        /// <returns>True if it needs to be mapped</returns>
        public static bool RequiresMapping(this Symbol symbol)
        {
            switch (symbol.SecurityType)
            {
                case SecurityType.Base:
                    return symbol.HasUnderlying && symbol.Underlying.RequiresMapping();
                case SecurityType.Future:
                    return symbol.IsCanonical();
                case SecurityType.Equity:
                case SecurityType.Option:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks whether the fill event for closing a trade is a winning trade
        /// </summary>
        /// <param name="fill">The fill event</param>
        /// <param name="security">The security being traded</param>
        /// <param name="profitLoss">The profit-loss for the closed trade</param>
        /// <returns>
        /// Whether the trade is a win.
        /// For options assignments this depends on whether the option is ITM or OTM and the position side.
        /// See <see cref="Trade.IsWin"/> for more information.
        /// </returns>
        public static bool IsWin(this OrderEvent fill, Security security, decimal profitLoss)
        {
            // For non-options or non-exercise orders, the trade is a win if the profit-loss is positive
            if (!fill.Symbol.SecurityType.IsOption() || fill.Ticket.OrderType != OrderType.OptionExercise)
            {
                return profitLoss > 0;
            }

            var option = (Option)security;

            // If the fill is a sell, the original transaction was a buy
            if (fill.Direction == OrderDirection.Sell)
            {
                // If the option is ITM, the trade is a win only if the profit is greater than the ITM amount
                return fill.IsInTheMoney && Math.Abs(profitLoss) < option.InTheMoneyAmount(fill.FillQuantity);
            }

            // It is a win if the buyer paid more than what they saved (the ITM amount)
            return !fill.IsInTheMoney || Math.Abs(profitLoss) > option.InTheMoneyAmount(fill.FillQuantity);
        }

        /// <summary>
        /// Gets the option's ITM amount for the given quantity.
        /// </summary>
        /// <param name="option">The option security</param>
        /// <param name="quantity">The quantity</param>
        /// <returns>The ITM amount for the absolute quantity</returns>
        /// <remarks>The returned value can be negative, which would mean the option is actually OTM.</remarks>
        public static ConvertibleCashAmount InTheMoneyAmount(this Option option, decimal quantity)
        {
            return option.Holdings.GetQuantityValue(Math.Abs(quantity), option.GetPayOff(option.Underlying.Price));
        }

        /// <summary>
        /// Gets the greatest common divisor of a list of numbers
        /// </summary>
        /// <param name="values">List of numbers which greatest common divisor is requested</param>
        /// <returns>The greatest common divisor for the given list of numbers</returns>
        public static int GreatestCommonDivisor(this IEnumerable<int> values)
        {
            int? result = null;
            foreach (var value in values)
            {
                if (result.HasValue)
                {
                    result = GreatestCommonDivisor(result.Value, value);
                }
                else
                {
                    result = value;
                }
            }

            if (!result.HasValue)
            {
                throw new ArgumentException(Messages.Extensions.GreatestCommonDivisorEmptyList);
            }

            return result.Value;
        }

        /// <summary>
        /// Gets the greatest common divisor of two numbers
        /// </summary>
        private static int GreatestCommonDivisor(int a, int b)
        {
            int remainder;
            while (b != 0)
            {
                remainder = a % b;
                a = b;
                b = remainder;
            }
            return Math.Abs(a);
        }

        /// <summary>
        /// Safe method to perform divisions avoiding DivideByZeroException and Overflow/Underflow exceptions
        /// </summary>
        /// <param name="failValue">Value to be returned if the denominator is zero</param>
        /// <returns>The numerator divided by the denominator if the denominator is not
        /// zero. Otherwise, the default failValue or the provided one</returns>
        public static decimal SafeDivision(this decimal numerator, decimal denominator, decimal failValue = 0)
        {
            try
            {
                return (denominator == 0) ? failValue : (numerator / denominator);
            }
            catch
            {
                return failValue;
            }
        }

        /// <summary>
        /// Retrieve a common custom data types from the given symbols if any
        /// </summary>
        /// <param name="symbols">The target symbols to search</param>
        /// <returns>The custom data type or null</returns>
        public static Type GetCustomDataTypeFromSymbols(Symbol[] symbols)
        {
            if (symbols.Length != 0)
            {
                if (!SecurityIdentifier.TryGetCustomDataTypeInstance(symbols[0].ID.Symbol, out var dataType)
                    || symbols.Any(x => !SecurityIdentifier.TryGetCustomDataTypeInstance(x.ID.Symbol, out var customDataType) || customDataType != dataType))
                {
                    return null;
                }
                return dataType;
            }

            return null;
        }

        /// <summary>
        /// Determines if certain data type is custom
        /// </summary>
        /// <param name="symbol">Symbol associated with the data type</param>
        /// <param name="type">Data type to determine if it's custom</param>
        public static bool IsCustomDataType(Symbol symbol, Type type)
        {
            return type.Namespace != typeof(Bar).Namespace || Extensions.GetCustomDataTypeFromSymbols(new Symbol[] { symbol }) != null;
        }

        /// <summary>
        /// Returns the amount of fee's charged by executing a market order with the given arguments
        /// </summary>
        /// <param name="security">Security for which we would like to make a market order</param>
        /// <param name="quantity">Quantity of the security we are seeking to trade</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="marketOrder">This out parameter will contain the market order constructed</param>
        public static CashAmount GetMarketOrderFees(Security security, decimal quantity, DateTime time, out MarketOrder marketOrder)
        {
            marketOrder = new MarketOrder(security.Symbol, quantity, time);
            return security.FeeModel.GetOrderFee(new OrderFeeParameters(security, marketOrder)).Value;
        }

        private static Symbol ConvertToSymbol(PyObject item, bool dispose)
        {
            if (PyString.IsStringType(item))
            {
                return SymbolCache.GetSymbol(dispose ? item.GetAndDispose<string>() : item.As<string>());
            }
            else
            {
                Symbol symbol;
                try
                {
                    symbol = dispose ? item.GetAndDispose<Symbol>() : item.As<Symbol>();
                }
                catch (Exception e)
                {
                    throw new ArgumentException(Messages.Extensions.ConvertToSymbolEnumerableFailed(item), e);
                }
                return symbol;
            }
        }
    }
}
