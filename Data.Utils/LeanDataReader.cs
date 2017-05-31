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
using QuantConnect.Util;
using Ionic.Zip;


namespace QuantConnect.Data.Utils
{
    public class LeanDataReader
    {
        private readonly DateTime _date;
        private readonly string _zipPath;
        private readonly SubscriptionDataConfig _config;
        
        public LeanDataReader(SubscriptionDataConfig config, Symbol symbol, Resolution resolution, DateTime date, string dataFolder)
        {
            _date = date;
            _zipPath = LeanData.GenerateZipFilePath(dataFolder, symbol, date,  resolution, LeanData.GetCommonTickType(symbol.SecurityType));
            _config = config;
        }

        /// <summary>
        /// Enumerate over the tick zip file and return a list of BaseData.
        /// </summary>
        /// <returns>IEnumerable of ticks</returns>
        public IEnumerable<BaseData> Parse()
        {
            var factory = (BaseData) ObjectActivator.GetActivator(_config.Type).Invoke(new object[0]);
            ZipFile zipFile;
            using (var unzipped = Compression.Unzip(_zipPath, out zipFile))
            {
                string line;
                while ((line = unzipped.ReadLine()) != null)
                {
                    yield return factory.Reader(_config, line, _date, false);
                }
            }
            zipFile.Dispose();
        }
    }
}
