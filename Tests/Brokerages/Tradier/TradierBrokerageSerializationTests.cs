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

using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Brokerages.Tradier;

namespace QuantConnect.Tests.Brokerages.Tradier
{
    [TestFixture]
    public class TradierBrokerageSerializationTests
    {
        [Test]
        public void QuotesHandles_null_OHLC()
        {
            // response received from tradier pre-market
            const string rawResponse = @"{'quotes':
	{'quote':
		{'symbol':'AMZN',
		'description':'Amazon.com Inc',
		'exch':'Q',
		'type':'stock',
		'last':463.37,
		'change':0.0,
		'change_percentage':0.0,
		'volume':812,
		'average_volume':3522497,
		'last_volume':235852,
		'trade_date':1440446400000,
		'open':null,
		'high':null,
		'low':null,
		'close':null,
		'prevclose':463.37,
		'week_52_high':580.57,
		'week_52_low':284.0,
		'bid':477.2,
		'bidsize':2,
		'bidexch':'P',
		'bid_date':1440490442000,
		'ask':481.16,
		'asksize':1,
		'askexch':'P',
		'ask_date':1440490432000,
		'root_symbols':'AMZN7,AMZN'
		}
	}
}";
            dynamic dynDeserialized = JsonConvert.DeserializeObject(rawResponse);
            var deserialized = JsonConvert.DeserializeObject<TradierQuoteContainer>((string)dynDeserialized["quotes"].ToString()).Quotes[0];

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(null, deserialized.Open);
            Assert.AreEqual(null, deserialized.High);
            Assert.AreEqual(null, deserialized.Low);
            Assert.AreEqual(null, deserialized.Close);
        }
    }
}
