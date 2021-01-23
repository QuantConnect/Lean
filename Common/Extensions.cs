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
using static QuantConnect.StringExtensions;
using Microsoft.IO;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Securities.FutureOption;
using QuantConnect.Securities.Option;

namespace QuantConnect
{
    /// <summary>
    /// Extensions function collections - group all static extensions functions here.
    /// </summary>
    public static class Extensions
    {
        private static RecyclableMemoryStreamManager MemoryManager = new RecyclableMemoryStreamManager();
        private static ConcurrentBag<Guid> Guids = new ConcurrentBag<Guid>();

        private static readonly Dictionary<IntPtr, PythonActivator> PythonActivators
            = new Dictionary<IntPtr, PythonActivator>();

        /// <summary>
        /// The offset span from the market close to liquidate or exercise a security on the delisting date
        /// </summary>
        /// <remarks>Will no be used in live trading</remarks>
        /// <remarks>By default span is negative 15 minutes. We want to liquidate before market closes if not, in some cases
        /// like future options the market close would match the delisted event time and would cancel all orders and mark the security
        /// as non tradable and delisted.</remarks>
        public static TimeSpan DelistingMarketCloseOffsetSpan { get; set; } = TimeSpan.FromMinutes(-15);

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
        /// <remarks>For performance will reuse a memory stream guid per thread. So</remarks>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemoryStream GetMemoryStream(Guid guid)
        {
            return MemoryManager.GetStream(guid);
        }

        /// <summary>
        /// Gets a unique id. Should be returned using <see cref="ReturnId"/>
        /// </summary>
        /// <remarks>Creating a new <see cref="Guid"/> is expensive</remarks>
        /// <remarks>Used for <see cref="GetMemoryStream"/></remarks>
        /// <returns>A unused <see cref="Guid"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid RentId()
        {
            Guid guid;
            if (!Guids.TryTake(out guid))
            {
                guid = new Guid();
            }
            return guid;
        }

        /// <summary>
        /// Returns a rented unique id <see cref="RentId"/>
        /// </summary>
        /// <remarks>Creating a new <see cref="Guid"/> is expensive</remarks>
        /// <remarks>Used for <see cref="GetMemoryStream"/></remarks>
        /// <param name="guid">The guid to return</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReturnId(Guid guid)
        {
            Guids.Add(guid);
        }

        /// <summary>
        /// Serialize a list of ticks using protobuf
        /// </summary>
        /// <param name="ticks">The list of ticks to serialize</param>
        /// <returns>The resulting byte array</returns>
        public static byte[] ProtobufSerialize(this List<Tick> ticks)
        {
            var guid = RentId();
            byte[] result;
            using (var stream = GetMemoryStream(guid))
            {
                Serializer.Serialize(stream, ticks);
                result = stream.ToArray();
            }

            ReturnId(guid);
            return result;
        }

        /// <summary>
        /// Serialize a base data instance using protobuf
        /// </summary>
        /// <param name="baseData">The data point to serialize</param>
        /// <returns>The resulting byte array</returns>
        public static byte[] ProtobufSerialize(this IBaseData baseData)
        {
            var guid = RentId();

            byte[] result;
            using (var stream = GetMemoryStream(guid))
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
                result = stream.ToArray();
            }
            ReturnId(guid);

            return result;
        }

        /// <summary>
        /// Extension method to get security price is 0 messages for users
        /// </summary>
        /// <remarks>The value of this method is normalization</remarks>
        public static string GetZeroPriceMessage(this Symbol symbol)
        {
            return $"{symbol}: The security does not have an accurate price as it has not yet received a bar of data. " +
                   "Before placing a trade (or using SetHoldings) warm up your algorithm with SetWarmup, or use slice.Contains(symbol)" +
                   " to confirm the Slice object has price before using the data. Data does not necessarily all arrive at the same" +
                   " time so your algorithm should confirm the data is ready before using it. In live trading this can mean you do" +
                   " not have an active subscription to the asset class you're trying to trade. If using custom data make sure you've" +
                   " set the 'Value' property.";
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
                    Log.Trace($"StopSafely(): waiting for '{thread.Name}' thread to stop...");
                    // just in case we add a time out
                    if (!thread.Join(timeout))
                    {
                        Log.Error($"StopSafely(): Timeout waiting for '{thread.Name}' thread to stop");
                        thread.Abort();
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
        public static int GetHash(this IDictionary<int, Order> orders)
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
                            var stopMarket = order as StopMarketOrder;
                            if (stopMarket != null)
                            {
                                stopMarket.StopPrice = stopMarket.StopPrice.SmartRounding();
                            }
                            return JsonConvert.SerializeObject(pair.Value, Formatting.None);
                        }
                    )
            );
            return joinedOrders.GetHashCode();
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
        /// Returns true if the specified <see cref="Series"/> instance holds no <see cref="ChartPoint"/>
        /// </summary>
        public static bool IsEmpty(this Series series)
        {
            return series.Values.Count == 0;
        }

        /// <summary>
        /// Returns if the specified <see cref="Chart"/> instance  holds no <see cref="Series"/>
        /// or they are all empty <see cref="IsEmpty(Series)"/>
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
                var method = instance.GetAttr(name);
                var pythonType = method.GetPythonType();
                var isPythonDefined = pythonType.Repr().Equals("<class \'method\'>");

                return isPythonDefined ? method : null;
            }
        }

        /// <summary>
        /// Returns an ordered enumerable where position reducing orders are executed first
        /// and the remaining orders are executed in decreasing order value.
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
            return targets.Select(x => new {
                    PortfolioTarget = x,
                    TargetQuantity = x.Quantity,
                    ExistingQuantity = algorithm.Portfolio[x.Symbol].Quantity
                                       + algorithm.Transactions.GetOpenOrderTickets(x.Symbol)
                                           .Aggregate(0m, (d, t) => d + t.Quantity - t.QuantityFilled),
                    Security = algorithm.Securities[x.Symbol]
                })
                .Where(x => x.Security.HasData
                            && (targetIsDelta ? Math.Abs(x.TargetQuantity) : Math.Abs(x.TargetQuantity - x.ExistingQuantity))
                            >= x.Security.SymbolProperties.LotSize
                )
                .Select(x => new {
                    PortfolioTarget = x.PortfolioTarget,
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
                throw new ArgumentException($"Data type \'{type.Name}\' missing parameterless constructor " +
                    $"E.g. public {type.Name}() {{ }}");
            }

            var instance = objectActivator.Invoke(new object[] { type });
            if(instance == null)
            {
                // shouldn't happen but just in case...
                throw new ArgumentException($"Failed to create instance of type \'{type.Name}\'");
            }

            // we expect 'instance' to inherit BaseData in most cases so we use 'as' versus 'IsAssignableFrom'
            // since it is slightly cheaper
            var result = instance as BaseData;
            if (result == null)
            {
                throw new ArgumentException($"Data type \'{type.Name}\' does not inherit required {nameof(BaseData)}");
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
            var builder = new StringBuilder();
            using (var md5Hash = MD5.Create())
            {
                var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(str));
                foreach (var t in data) builder.Append(t.ToStringInvariant("x2"));
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
            var crypt = new SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(data), 0, Encoding.UTF8.GetByteCount(data));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToStringInvariant("x2"));
            }
            return hash.ToString();
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
        /// Removes the specified element to the collection with the specified key. If the entry's count drops to
        /// zero, then the entry will be removed.
        /// </summary>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TElement">The collection element type</typeparam>
        /// <param name="dictionary">The source dictionary to be added to</param>
        /// <param name="key">The key</param>
        /// <param name="element">The element to be added</param>
        public static ImmutableDictionary<TKey, ImmutableHashSet<TElement>> Remove<TKey, TElement>(
            this ImmutableDictionary<TKey, ImmutableHashSet<TElement>> dictionary,
            TKey key,
            TElement element
            )
        {
            ImmutableHashSet<TElement> set;
            if (!dictionary.TryGetValue(key, out set))
            {
                return dictionary;
            }

            set = set.Remove(element);
            if (set.Count == 0)
            {
                return dictionary.Remove(key);
            }

            return dictionary.SetItem(key, set);
        }

        /// <summary>
        /// Removes the specified element to the collection with the specified key. If the entry's count drops to
        /// zero, then the entry will be removed.
        /// </summary>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <typeparam name="TElement">The collection element type</typeparam>
        /// <param name="dictionary">The source dictionary to be added to</param>
        /// <param name="key">The key</param>
        /// <param name="element">The element to be added</param>
        public static ImmutableSortedDictionary<TKey, ImmutableHashSet<TElement>> Remove<TKey, TElement>(
            this ImmutableSortedDictionary<TKey, ImmutableHashSet<TElement>> dictionary,
            TKey key,
            TElement element
            )
        {
            ImmutableHashSet<TElement> set;
            if (!dictionary.TryGetValue(key, out set))
            {
                return dictionary;
            }

            set = set.Remove(element);
            if (set.Count == 0)
            {
                return dictionary.Remove(key);
            }

            return dictionary.SetItem(key, set);
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
                list = new List<Tick>(1);
                dictionary.Add(key, list);
            }
            list.Add(tick);
        }

        /// <summary>
        /// Extension method to round a double value to a fixed number of significant figures instead of a fixed decimal places.
        /// </summary>
        /// <param name="d">Double we're rounding</param>
        /// <param name="digits">Number of significant figures</param>
        /// <returns>New double rounded to digits-significant figures</returns>
        public static double RoundToSignificantDigits(this double d, int digits)
        {
            if (d == 0) return 0;
            var scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
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
                    $"It is not possible to cast a non-finite floating-point value ({input}) as decimal. Please review math operations and verify the result is valid.",
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
        /// Uses <see cref="Normalize"/>.
        /// </summary>
        /// <param name="input">The <see cref="decimal"/> to convert to <see cref="string"/></param>
        /// <returns>Input converted to <see cref="string"/> with no trailing zeros</returns>
        public static string NormalizeToStr(this decimal input)
        {
            return Normalize(input).ToString(CultureInfo.InvariantCulture);
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
        /// <param name="extendedMarket">True for extended market hours, otherwise false</param>
        /// <returns>Rounded datetime</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ExchangeRoundDown(this DateTime dateTime, TimeSpan interval, SecurityExchangeHours exchangeHours, bool extendedMarket)
        {
            // can't round against a zero interval
            if (interval == TimeSpan.Zero) return dateTime;

            var rounded = dateTime.RoundDown(interval);
            while (!exchangeHours.IsOpen(rounded, rounded + interval, extendedMarket))
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
        /// <param name="extendedMarket">True for extended market hours, otherwise false</param>
        /// <returns>Rounded datetime</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ExchangeRoundDownInTimeZone(this DateTime dateTime, TimeSpan interval, SecurityExchangeHours exchangeHours, DateTimeZone roundingTimeZone, bool extendedMarket)
        {
            // can't round against a zero interval
            if (interval == TimeSpan.Zero) return dateTime;

            var dateTimeInRoundingTimeZone = dateTime.ConvertTo(exchangeHours.TimeZone, roundingTimeZone);
            var roundedDateTimeInRoundingTimeZone = dateTimeInRoundingTimeZone.RoundDown(interval);
            var rounded = roundedDateTimeInRoundingTimeZone.ConvertTo(roundingTimeZone, exchangeHours.TimeZone);

            while (!exchangeHours.IsOpen(rounded, rounded + interval, extendedMarket))
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

            return from.AtLeniently(LocalDateTime.FromDateTime(time)).WithZone(to).ToDateTimeUnspecified();
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

            return from.AtLeniently(LocalDateTime.FromDateTime(time)).ToDateTimeUtc();
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
        public static TimeSpan ToTimeSpan(this Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                    // ticks can be instantaneous
                    return TimeSpan.FromTicks(0);
                case Resolution.Second:
                    return TimeSpan.FromSeconds(1);
                case Resolution.Minute:
                    return TimeSpan.FromMinutes(1);
                case Resolution.Hour:
                    return TimeSpan.FromHours(1);
                case Resolution.Daily:
                    return TimeSpan.FromDays(1);
                default:
                    throw new ArgumentOutOfRangeException("resolution");
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
                throw new InvalidOperationException(Invariant($"Unable to exactly convert time span ('{timeSpan}') to resolution."));
            }

            // for non-perfect matches
            if (Time.OneSecond > timeSpan) return Resolution.Tick;
            if (Time.OneMinute > timeSpan) return Resolution.Second;
            if (Time.OneHour   > timeSpan) return Resolution.Minute;
            if (Time.OneDay    > timeSpan) return Resolution.Hour;

            return Resolution.Daily;
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
                return Enum.Parse(type, value);
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
        /// Return the first in the series of names, or find the one that matches the configured algirithmTypeName
        /// </summary>
        /// <param name="names">The list of class names</param>
        /// <param name="algorithmTypeName">The configured algorithm type name from the config</param>
        /// <returns>The name of the class being run</returns>
        public static string SingleOrAlgorithmTypeName(this List<string> names, string algorithmTypeName)
        {
            // if there's only one use that guy
            // if there's more than one then find which one we should use using the algorithmTypeName specified
            return names.Count == 1 ? names.Single() : names.SingleOrDefault(x => x.EndsWith("." + algorithmTypeName));
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
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Converts the specified <paramref name="optionRight"/> value to its corresponding string representation
        /// </summary>
        /// <remarks>This method provides faster performance than enum <see cref="ToString"/></remarks>
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
                case SecurityType.Commodity:
                    return "commodity";
                case SecurityType.Forex:
                    return "forex";
                case SecurityType.Future:
                    return "future";
                case SecurityType.Cfd:
                    return "cfd";
                case SecurityType.Crypto:
                    return "crypto";
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
                case OrderType.OptionExercise:
                case OrderType.Market:
                case OrderType.MarketOnOpen:
                case OrderType.MarketOnClose:
                    limitPrice = order.Price;
                    stopPrice = order.Price;
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
                order.Time,
                order.Tag,
                order.Properties);

            submitOrderRequest.SetOrderId(order.Id);
            var orderTicket = new OrderTicket(transactionManager, submitOrderRequest);
            orderTicket.SetOrder(order);
            return orderTicket;
        }

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
                    // Special case: Type
                    if (typeof(Type).IsAssignableFrom(type))
                    {
                        result = (T)pyObject.AsManagedObject(type);
                        return true;
                    }

                    // Special case: IEnumerable
                    if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        result = (T)pyObject.AsManagedObject(type);
                        return true;
                    }

                    var pythonType = pyObject.GetPythonType();
                    var csharpType = pythonType.As<Type>();

                    if (!type.IsAssignableFrom(csharpType))
                    {
                        pythonType.Dispose();
                        return false;
                    }

                    result = (T)pyObject.AsManagedObject(type);

                    // If the PyObject type and the managed object names are the same,
                    // pyObject is a C# object wrapped in PyObject, in this case return true
                    // Otherwise, pyObject is a python object that subclass a C# class, only return true if 'allowPythonDerivative'
                    var name = (((dynamic) pythonType).__name__ as PyObject).GetAndDispose<string>();
                    pythonType.Dispose();
                    return allowPythonDerivative || name == result.GetType().Name;
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

            if (!typeof(MulticastDelegate).IsAssignableFrom(type))
            {
                throw new ArgumentException($"TryConvertToDelegate cannot be used to convert a PyObject into {type}.");
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

                    PythonEngine.Exec(code, null, locals.Handle);
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
        /// Wraps the provided universe selection selector checking if it returned <see cref="Universe.Unchanged"/>
        /// and returns it instead, else enumerates result as <see cref="IEnumerable{Symbol}"/>
        /// </summary>
        /// <remarks>This method is a work around for the fact that currently we can not create a delegate which returns
        /// an <see cref="IEnumerable{Symbol}"/> from a python method returning an array, plus the fact that
        /// <see cref="Universe.Unchanged"/> can not be cast to an array</remarks>
        public static Func<T, IEnumerable<Symbol>> ConvertToUniverseSelectionSymbolDelegate<T>(this Func<T, object> selector)
        {
            return data =>
            {
                var result = selector(data);
                return ReferenceEquals(result, Universe.Unchanged)
                    ? Universe.Unchanged : ((object[])result).Select(x => (Symbol)x);
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
                throw new ArgumentException($"ConvertToDelegate cannot be used to convert a PyObject into {typeof(T)}.");
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
                    throw new ArgumentException(
                        $"ConvertToDictionary cannot be used to convert a {inputType} into {targetType}. Reason: {e.Message}",
                        e
                    );
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
                if (!PyList.IsListType(pyObject))
                {
                    pyObject = new PyList(new[] {pyObject});
                }

                foreach (PyObject item in pyObject)
                {
                    if (PyString.IsStringType(item))
                    {
                        yield return SymbolCache.GetSymbol(item.GetAndDispose<string>());
                    }
                    else
                    {
                        Symbol symbol;
                        try
                        {
                            symbol = item.GetAndDispose<Symbol>();
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentException(
                                "Argument type should be Symbol or a list of Symbol. " +
                                $"Object: {item}. Type: {item.GetPythonType()}",
                                e
                            );
                        }

                        yield return symbol;
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
                    throw new ArgumentException($"GetEnumString(): {pyObject.Repr()} is not a C# Type.");
                }
            }
        }

        /// <summary>
        /// Creates a type with a given name, if PyObject is not a CLR type. Otherwise, convert it.
        /// </summary>
        /// <param name="pyObject">Python object representing a type.</param>
        /// <returns>Type object</returns>
        public static Type CreateType(this PyObject pyObject)
        {
            Type type;
            if (pyObject.TryConvert(out type) &&
                type != typeof(PythonQuandl) &&
                type != typeof(PythonData))
            {
                return type;
            }

            PythonActivator pythonType;
            if (!PythonActivators.TryGetValue(pyObject.Handle, out pythonType))
            {
                AssemblyName an;
                using (Py.GIL())
                {
                    an = new AssemblyName(pyObject.Repr().Split('\'')[1]);
                }
                var typeBuilder = AppDomain.CurrentDomain
                    .DefineDynamicAssembly(an, AssemblyBuilderAccess.Run)
                    .DefineDynamicModule("MainModule")
                    .DefineType(an.Name, TypeAttributes.Class, type);

                pythonType = new PythonActivator(typeBuilder.CreateType(), pyObject);

                ObjectActivator.AddActivator(pythonType.Type, pythonType.Factory);

                // Save to prevent future additions
                PythonActivators.Add(pyObject.Handle, pythonType);
            }
            return pythonType.Type;
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
            if (s.EndsWith(ending))
            {
                return s.Substring(0, s.Length - ending.Length);
            }
            else
            {
                return s;
            }
        }

        /// <summary>
        /// Normalizes the specified price based on the DataNormalizationMode
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetNormalizedPrice(this SubscriptionDataConfig config, decimal price)
        {
            switch (config.DataNormalizationMode)
            {
                case DataNormalizationMode.Raw:
                    return price;

                // the price scale factor will be set accordingly based on the mode in update scale factors
                case DataNormalizationMode.Adjusted:
                case DataNormalizationMode.SplitAdjusted:
                    return price * config.PriceScaleFactor;

                case DataNormalizationMode.TotalReturn:
                    return (price * config.PriceScaleFactor) + config.SumOfDividends;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Gets the delisting date for the provided Symbol
        /// </summary>
        /// <param name="symbol">The symbol to lookup the last trading date</param>
        /// <param name="mapFile">Map file to use for delisting date. Defaults to SID.DefaultDate if no value is passed and is equity.</param>
        /// <returns></returns>
        public static DateTime GetDelistingDate(this Symbol symbol, MapFile mapFile = null)
        {
            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Future:
                    return symbol.ID.Date;
                case SecurityType.Option:
                    return OptionSymbol.GetLastDayOfTrading(symbol);
                case SecurityType.FutureOption:
                    return FutureOptionSymbol.GetLastDayOfTrading(symbol);
                default:
                    return mapFile?.DelistingDate ?? SecurityIdentifier.DefaultDate;
            }
        }

        /// <summary>
        /// Returns the delisted liquidation time for a given delisting warning and exchange hours
        /// </summary>
        /// <param name="delisting">The delisting warning event</param>
        /// <param name="exchangeHours">The securities exchange hours to use</param>
        /// <returns>The securities liquidation time</returns>
        public static DateTime GetLiquidationTime(this Delisting delisting, SecurityExchangeHours exchangeHours)
        {
            if (delisting.Type != DelistingType.Warning)
            {
                throw new ArgumentException("GetLiquidationTime can only be called with the liquidate warning event", nameof(delisting));
            }

            var delistingWarning = delisting.Time.Date;

            // by default liquidation/exercise will happen a few min before the end of the last trading day
            var liquidationTime = delistingWarning.AddDays(1).Add(DelistingMarketCloseOffsetSpan);

            // if the market is open today (most probably should), we will determine the market close and liquidate a few min before instead
            if (exchangeHours.IsDateOpen(delistingWarning))
            {
                var marketOpen = delistingWarning;
                if (!exchangeHours.IsOpen(marketOpen, false))
                {
                    // if the market isn't open at 0:00 we get next market open
                    marketOpen = exchangeHours.GetNextMarketOpen(delistingWarning, false);
                }

                // using current market open we will get next market close which should be today and we will liquidate a few min before
                liquidationTime = exchangeHours.GetNextMarketClose(marketOpen, false)
                    .Add(DelistingMarketCloseOffsetSpan);
            }

            return liquidationTime;
        }

        /// <summary>
        /// Scale data based on factor function
        /// </summary>
        public static BaseData Scale(this BaseData data, Func<decimal, decimal> factor)
        {
            switch (data.DataType)
            {
                case MarketDataType.TradeBar:
                    var tradeBar = data as TradeBar;
                    if (tradeBar != null)
                    {
                        tradeBar.Open = factor(tradeBar.Open);
                        tradeBar.High = factor(tradeBar.High);
                        tradeBar.Low = factor(tradeBar.Low);
                        tradeBar.Close = factor(tradeBar.Close);
                    }
                    break;
                case MarketDataType.Tick:
                    var securityType = data.Symbol.SecurityType;
                    if (securityType != SecurityType.Equity &&
                        securityType != SecurityType.Option &&
                        securityType != SecurityType.FutureOption &&
                        securityType != SecurityType.Future)
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
                        tick.Value = factor(tick.Value);
                        break;
                    }

                    tick.BidPrice = tick.BidPrice != 0 ? factor(tick.BidPrice) : 0;
                    tick.AskPrice = tick.AskPrice != 0 ? factor(tick.AskPrice) : 0;

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
                            quoteBar.Ask.Open = factor(quoteBar.Ask.Open);
                            quoteBar.Ask.High = factor(quoteBar.Ask.High);
                            quoteBar.Ask.Low = factor(quoteBar.Ask.Low);
                            quoteBar.Ask.Close = factor(quoteBar.Ask.Close);
                        }
                        if (quoteBar.Bid != null)
                        {
                            quoteBar.Bid.Open = factor(quoteBar.Bid.Open);
                            quoteBar.Bid.High = factor(quoteBar.Bid.High);
                            quoteBar.Bid.Low = factor(quoteBar.Bid.Low);
                            quoteBar.Bid.Close = factor(quoteBar.Bid.Close);
                        }
                        quoteBar.Value = quoteBar.Close;
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
        /// <param name="config">Price scale</param>
        /// <returns></returns>
        public static BaseData Normalize(this BaseData data, SubscriptionDataConfig config)
        {
            return data?.Scale(p => config.GetNormalizedPrice(p));
        }

        /// <summary>
        /// Adjust prices based on price scale
        /// </summary>
        /// <param name="data">Data to be adjusted</param>
        /// <param name="scale">Price scale</param>
        /// <returns></returns>
        public static BaseData Adjust(this BaseData data, decimal scale)
        {
            return data?.Scale(p => p * scale);
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
                throw new ArgumentException($"Source cannot be null or empty.");
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
        /// Creates a <see cref="OptionChainUniverse"/> for a given symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <param name="symbol">Symbol of the option</param>
        /// <param name="filter">The option filter to use</param>
        /// <param name="universeSettings">The universe settings, will use algorithm settings if null</param>
        /// <returns><see cref="OptionChainUniverse"/> for the given symbol</returns>
        public static OptionChainUniverse CreateOptionChain(this IAlgorithm algorithm, Symbol symbol, Func<OptionFilterUniverse, OptionFilterUniverse> filter, UniverseSettings universeSettings = null)
        {
            if (symbol.SecurityType != SecurityType.Option && symbol.SecurityType != SecurityType.FutureOption)
            {
                throw new ArgumentException("CreateOptionChain requires an option symbol.");
            }

            // rewrite non-canonical symbols to be canonical
            var market = symbol.ID.Market;
            var underlying = symbol.Underlying;
            if (!symbol.IsCanonical())
            {
                // The underlying can be a non-equity Symbol, so we must explicitly
                // initialize the Symbol using the CreateOption(Symbol, ...) overload
                // to ensure that the underlying SecurityType is preserved and not
                // written as SecurityType.Equity.
                var alias = $"?{underlying.Value}";

                symbol = Symbol.CreateOption(
                    underlying,
                    market,
                    default(OptionStyle),
                    default(OptionRight),
                    0m,
                    SecurityIdentifier.DefaultDate,
                    alias);
            }

            // resolve defaults if not specified
            var settings = universeSettings ?? algorithm.UniverseSettings;

            // create canonical security object, but don't duplicate if it already exists
            Security security;
            Option optionChain;
            if (!algorithm.Securities.TryGetValue(symbol, out security))
            {
                var config = algorithm.SubscriptionManager.SubscriptionDataConfigService.Add(
                    typeof(ZipEntryName),
                    symbol,
                    settings.Resolution,
                    settings.FillForward,
                    settings.ExtendedMarketHours,
                    false);
                optionChain = (Option)algorithm.Securities.CreateSecurity(symbol, config, settings.Leverage, false);
            }
            else
            {
                optionChain = (Option)security;
            }

            // set the option chain contract filter function
            optionChain.SetFilter(filter);

            // force option chain security to not be directly tradable AFTER it's configured to ensure it's not overwritten
            optionChain.IsTradable = false;

            return new OptionChainUniverse(optionChain, settings, algorithm.LiveMode);
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
    }
}
