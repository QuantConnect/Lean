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


using System.IO;
using NUnit.Framework;
using System;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Tests.Common.Data.Auxiliary
{
    [TestFixture]
    public class LocalZipFactorFileProviderTests : LocalDiskFactorFileProviderTests
    {
        private string _zipFilePath;

        [OneTimeSetUp]
        public new void Setup()
        {
            // Take our repo included factor files and zip them up for these tests
            var date = DateTime.UtcNow.Date.AddDays(-3);
            var path = Path.Combine(Globals.DataFolder, $"equity/usa/factor_files/");
            var tmp = "./tmp.zip";

            _zipFilePath = Path.Combine(Globals.DataFolder, $"equity/usa/factor_files/factor_files_{date:yyyyMMdd}.zip");

            // Have to compress to tmp file or else it doesn't finish reading all the files in dir
            QuantConnect.Compression.ZipDirectory(path, tmp);
            File.Move(tmp, _zipFilePath);

            FactorFileProvider = new LocalZipFactorFileProvider();
            FactorFileProvider.Initialize(TestGlobals.MapFileProvider, TestGlobals.DataProvider);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (File.Exists(_zipFilePath))
            {
                File.Delete(_zipFilePath);
            }
        }
    }
}
