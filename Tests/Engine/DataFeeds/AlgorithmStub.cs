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

using QuantConnect.Python;
using QuantConnect.Algorithm;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    /// <summary>
    /// This type allows tests to easily create an algorithm that is mostly initialized in one line
    /// </summary>
    public class AlgorithmStub : QCAlgorithm
    {
        public List<SecurityChanges> SecurityChangesRecord = new List<SecurityChanges>();
        public DataManager DataManager;
        public IDataFeed DataFeed;

        /// <summary>
        /// Lanzy PandasConverter only if used
        /// </summary>
        public override PandasConverter PandasConverter
        {
            get
            {
                if(base.PandasConverter == null)
                {
                    SetPandasConverter();
                }
                return base.PandasConverter;
            }
        }

        public AlgorithmStub(bool createDataManager = true)
        {
            if (createDataManager)
            {
                var dataManagerStub = new DataManagerStub(this);
                DataManager = dataManagerStub;
                DataFeed = dataManagerStub.DataFeed;
                SubscriptionManager.SetDataManager(DataManager);
            }
            var orderProcessor = new FakeOrderProcessor();
            orderProcessor.TransactionManager = Transactions;
            Transactions.SetOrderProcessor(orderProcessor);
        }

        public AlgorithmStub(IDataFeed dataFeed)
        {
            DataFeed = dataFeed;
            DataManager = new DataManagerStub(dataFeed, this);
            SubscriptionManager.SetDataManager(DataManager);
            Transactions.SetOrderProcessor(new FakeOrderProcessor());
        }

        public void AddSecurities(Resolution resolution = Resolution.Second, List<string> equities = null, List<string> forex = null, List<string> crypto = null)
        {
            foreach (var ticker in equities ?? new List<string>())
            {
                AddSecurity(SecurityType.Equity, ticker, resolution);
                var symbol = SymbolCache.GetSymbol(ticker);
                Securities[symbol].Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            }
            foreach (var ticker in forex ?? new List<string>())
            {
                AddSecurity(SecurityType.Forex, ticker, resolution);
            }
            foreach (var ticker in crypto ?? new List<string>())
            {
                AddSecurity(SecurityType.Crypto, ticker, resolution);
                var symbol = SymbolCache.GetSymbol(ticker);
                Securities[symbol].Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.Utc));
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            SecurityChangesRecord.Add(changes);
        }
    }
}
