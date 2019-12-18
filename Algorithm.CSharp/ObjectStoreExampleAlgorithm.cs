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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Storage;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm showcases some features of the <see cref="IObjectStore"/> feature.
    /// One use case is to make consecutive backtests run faster by caching the results of
    /// potentially time consuming operations. In this example, we save the results of a
    /// history call. This pattern can be equally applied to a machine learning model being
    /// trained and then saving the model weights in the object store.
    /// </summary>
    public class ObjectStoreExampleAlgorithm : QCAlgorithm
    {
        private const string SPY_Close_ObjectStore_Key = "spy_close";
        private Security SPY;
        private Identity SPY_Close;
        private ExponentialMovingAverage SPY_Close_EMA10;
        private ExponentialMovingAverage SPY_Close_EMA50;

        // track last year of close and EMA10/EMA50
        public readonly RollingWindow<IndicatorDataPoint> SPY_Close_History = new RollingWindow<IndicatorDataPoint>(252);
        public readonly RollingWindow<IndicatorDataPoint> SPY_Close_EMA10_History = new RollingWindow<IndicatorDataPoint>(252);
        public readonly RollingWindow<IndicatorDataPoint> SPY_Close_EMA50_History = new RollingWindow<IndicatorDataPoint>(252);

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            SPY = AddEquity("SPY", Resolution.Minute);

            // define indicators on SPY daily closing prices
            SPY_Close = Identity(SPY.Symbol, Resolution.Daily);
            SPY_Close_EMA10 = SPY_Close.EMA(10);
            SPY_Close_EMA50 = SPY_Close.EMA(50);

            // each time an indicator is updated, push the value into our history rolling windows
            SPY_Close.Updated += (sender, args) =>
            {
                // each time we receive new closing price data, push our window to the object store
                SPY_Close_History.Add(args);
            };

            SPY_Close_EMA10.Updated += (sender, args) => SPY_Close_EMA10_History.Add(args);
            SPY_Close_EMA50.Updated += (sender, args) => SPY_Close_EMA50_History.Add(args);

            if (ObjectStore.ContainsKey(SPY_Close_ObjectStore_Key))
            {
                // our object store has our historical data saved, read the data
                // and push it through the indicators to warm everything up
                var values = ObjectStore.ReadJson<IndicatorDataPoint[]>(SPY_Close_ObjectStore_Key);
                Debug($"{SPY_Close_ObjectStore_Key} key exists in object store. Count: {values.Length}");

                foreach (var value in values.OrderBy(x => x.EndTime))
                {
                    SPY_Close.Update(value);
                }
            }
            else
            {
                Debug($"{SPY_Close_ObjectStore_Key} key does not exist in object store. Fetching history...");

                // if our object store doesn't have our data, fetch the history to initialize
                // we're pulling the last year's worth of SPY daily trade bars to fee into our indicators
                var history = History(new[] {SPY.Symbol}, TimeSpan.FromDays(365), Resolution.Daily).Get(SPY.Symbol);

                foreach (var tradeBar in history.OrderBy(x => x.EndTime))
                {
                    SPY_Close.Update(tradeBar.EndTime, tradeBar.Close);
                }

                // save our warm up data so next time we don't need to issue the history request
                var array = SPY_Close_History.OrderBy(x => x.EndTime).ToArray();
                ObjectStore.SaveJson(SPY_Close_ObjectStore_Key, array);

                // Can also use ObjectStore.SaveBytes(key, byte[])
                // and to read  ObjectStore.ReadBytes(key) => byte[]

                // we can also get a file path for our data. some ML libraries require model
                // weights to be loaded directly from a file path. The object store can provide
                // a file path for any key by: ObjectStore.GetFilePath(key) => string (file path)
            }
        }

        public override void OnData(Slice slice)
        {
            if (SPY_Close_EMA10 > SPY_Close && SPY_Close_EMA10 > SPY_Close_EMA50)
            {
                SetHoldings(SPY.Symbol, 1m);
            }
            else if (SPY_Close_EMA10 < SPY_Close && SPY_Close_EMA10 < SPY_Close_EMA50)
            {
                SetHoldings(SPY.Symbol, -1m);
            }
            else if (Portfolio[SPY.Symbol].IsLong)
            {
                if (SPY_Close_EMA10 < SPY_Close_EMA50)
                {
                    Liquidate(SPY.Symbol);
                }
            }
            else if (Portfolio[SPY.Symbol].IsShort)
            {
                if (SPY_Close_EMA10 > SPY_Close_EMA50)
                {
                    Liquidate(SPY.Symbol);
                }
            }
        }
    }
}
