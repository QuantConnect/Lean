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

using NUnit.Framework;
using System.IO;
using System.Reflection;

namespace QuantConnect.Tests.Common
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class OSTests
    {
        [Test]
        public void GetServerStatistics()
        {
            Assert.DoesNotThrow(() => OS.GetServerStatistics());
        }
        
        [Test]
        public void GetDriveCorrectly()
        {
            var expectedDrive = Path.GetPathRoot(Assembly.GetAssembly(GetType()).Location);
            var expectedDriveInfo = new DriveInfo(expectedDrive);
            var totalSizeInMegaBytes = (int)(expectedDriveInfo.TotalSize / (1024 * 1024));
            Assert.AreEqual(totalSizeInMegaBytes, OS.DriveTotalSpace);
        }
    }
}
