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
using Newtonsoft.Json;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// This data provider will wrap and use multiple data providers internally in the provided order
    /// </summary>
    public class CompositeDataProvider : IDataProvider
    {
        /// <summary>
        /// Event raised each time data fetch is finished (successfully or not)
        /// </summary>
        public event EventHandler<DataProviderNewDataRequestEventArgs> NewDataRequest;

        private readonly List<IDataProvider> _dataProviders;

        /// <summary>
        /// Creates a new instance and initialize data providers used
        /// </summary>
        public CompositeDataProvider()
        {
            _dataProviders = new List<IDataProvider>();

            var dataProvidersConfig = Config.Get("composite-data-providers");
            if (!string.IsNullOrEmpty(dataProvidersConfig))
            {
                var dataProviders = JsonConvert.DeserializeObject<List<string>>(dataProvidersConfig);
                foreach (var dataProvider in dataProviders)
                {
                    _dataProviders.Add(Composer.Instance.GetExportedValueByTypeName<IDataProvider>(dataProvider));
                }

                if (_dataProviders.Count == 0)
                {
                    throw new ArgumentException("CompositeDataProvider(): requires at least 1 valid data provider in 'composite-data-providers'");
                }
            }
            else
            {
                throw new ArgumentException("CompositeDataProvider(): requires 'composite-data-providers' to be set with a valid type name");
            }

            _dataProviders.ForEach(x => x.NewDataRequest += OnNewDataRequest);
        }

        /// <summary>
        /// Retrieves data to be used in an algorithm
        /// </summary>
        /// <param name="key">A string representing where the data is stored</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public Stream Fetch(string key)
        {
            for (var i = 0; i < _dataProviders.Count; i++)
            {
                var result = _dataProviders[i].Fetch(key);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Event invocator for the <see cref="NewDataRequest"/> event
        /// </summary>
        private void OnNewDataRequest(object sender, DataProviderNewDataRequestEventArgs e)
        {
            NewDataRequest?.Invoke(this, e);
        }
    }
}
