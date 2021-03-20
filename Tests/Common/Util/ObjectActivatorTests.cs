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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ObjectActivatorTests
    {
        [Test]
        public void IsFasterThanRawReflection()
        {
            int count = 100000;
            var data = new TradeBar(DateTime.Now, Symbols.SPY, 1m, 2m, 3m, 4m, 5);
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                var clone = Clone(data);
            }
            stopwatch.Stop();
            var elapsed1 = stopwatch.Elapsed;
            Log.Trace(elapsed1.TotalMilliseconds.ToStringInvariant());

            stopwatch.Reset();
            stopwatch.Start();
            for (int i = 0; i < count; i++)
            {
                var clone = ObjectActivator.Clone(data);
            }
            stopwatch.Stop();
            var elapsed2 = stopwatch.Elapsed;
            Log.Trace(elapsed2.TotalMilliseconds.ToStringInvariant());
            Assert.Less(elapsed2, elapsed1);
        }

        [Test]
        public void ClonesBaseDataDerivedTypes()
        {
            BaseData data = new IndicatorDataPoint(Symbols.SPY, DateTime.Now, 1m);
            BaseData clone = ObjectActivator.Clone(data) as BaseData;
            Assert.IsNotNull(clone);
            Assert.IsInstanceOf(data.GetType(), clone);
            Assert.AreEqual(data.Symbol, clone.Symbol);
            Assert.AreEqual(data.Time, clone.Time);
            Assert.AreEqual(data.Value, clone.Value);

            data = new TradeBar(DateTime.Now, Symbols.SPY, 1m, 2m, 3m, 4m, 5);
            var bar = ObjectActivator.Clone(data) as TradeBar;
            Assert.IsNotNull(clone);
            Assert.IsInstanceOf(data.GetType(), bar);
            Assert.AreEqual(data.Symbol, bar.Symbol);
            Assert.AreEqual(data.Time, bar.Time);
            Assert.AreEqual(data.Value, bar.Value);
            Assert.AreEqual(((TradeBar) data).Open, bar.Open);
            Assert.AreEqual(((TradeBar) data).High, bar.High);
            Assert.AreEqual(((TradeBar) data).Low, bar.Low);
            Assert.AreEqual(((TradeBar) data).Close, bar.Close);
            Assert.AreEqual(data.Price, bar.Price);
        }

        #region this was an original implementation which we'll compare against

        public static object Clone(object instanceToClone)
        {
            if (instanceToClone == null)
            {
                return null;
            }

            var type = instanceToClone.GetType();
            var factory = ObjectActivator.GetActivator(type);
            var fields = GetFieldInfosIncludingBaseClasses(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var instance = factory.Invoke(new object[0]);
            foreach (var field in fields)
            {
                field.SetValue(instance, field.GetValue(instanceToClone));
            }

            return instance;
        }

        /// <summary>
        /// Private fields in base classes aren't found in normal reflection, so we need to recurse on base types
        /// </summary>
        private static IEnumerable<FieldInfo> GetFieldInfosIncludingBaseClasses(Type type, BindingFlags bindingFlags)
        {
            FieldInfo[] fieldInfos = type.GetFields(bindingFlags);

            // If this class doesn't have a base, don't waste any time
            if (type.BaseType == typeof (object))
            {
                return fieldInfos;
            }

            // Otherwise, collect all types up to the furthest base class
            var currentType = type;
            var fieldComparer = new FieldInfoComparer();
            var fieldInfoList = new HashSet<FieldInfo>(fieldInfos, fieldComparer);
            while (currentType != typeof (object) && currentType != null)
            {
                fieldInfos = currentType.GetFields(bindingFlags);
                fieldInfoList.UnionWith(fieldInfos);
                currentType = currentType.BaseType;
            }
            return fieldInfoList;
        }

        private class FieldInfoComparer : IEqualityComparer<FieldInfo>
        {
            public bool Equals(FieldInfo x, FieldInfo y)
            {
                return x.DeclaringType == y.DeclaringType && x.Name == y.Name;
            }

            public int GetHashCode(FieldInfo obj)
            {
                return obj.Name.GetHashCode() ^ obj.DeclaringType.GetHashCode();
            }
        }

        #endregion

    }
}
