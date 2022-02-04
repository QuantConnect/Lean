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

using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Brokerages.Binance.Messages;

namespace QuantConnect.Tests.Brokerages.Binance
{
    [TestFixture]
    public class SpotAccountConverterTests
    {
        [Test]
        public void DeserializeJson()
        {
            var json = @"{
                ""makerCommission"": 15,
                ""takerCommission"": 15,
                ""buyerCommission"": 0,
                ""sellerCommission"": 0,
                ""canTrade"": true,
                ""canWithdraw"": true,
                ""canDeposit"": true,
                ""updateTime"": 123456789,
                ""accountType"": ""SPOT"",
                ""balances"": [
                {
                    ""asset"": ""BTC"",
                    ""free"": ""4723846.89208129"",
                    ""locked"": ""0.00000000""
                },
                {
                    ""asset"": ""LTC"",
                    ""free"": ""4763368.68006011"",
                    ""locked"": ""1.00000000""
                }
                ],
                ""permissions"": [
                    ""SPOT""
                ]
            }";

            var balances = JsonConvert.DeserializeObject<AccountInformation>(json, new SpotAccountConverter()).Balances
                .Cast<SpotBalance>()
                .ToArray();

            Assert.AreEqual(2, balances.Length);
            var bnb = balances.FirstOrDefault(a => a.Asset == "LTC");
            Assert.NotNull(bnb);
            Assert.AreEqual(4763368.68006011m, bnb.Free);
            Assert.AreEqual(1m, bnb.Locked);
            Assert.AreEqual(bnb.Free + bnb.Locked, bnb.Amount);
        }
    }
}
