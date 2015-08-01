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
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Oanda.DataType.Communications
{
    /// <summary>
    /// Represents the post order response from Oanda.
    /// </summary>
	public class PostOrderResponse : Response
	{
		public string instrument { get; set; }
		public string time { get; set; }
		public double? price { get; set; }

		public Order orderOpened { get; set; }
		public TradeData tradeOpened { get; set; }
		public List<Transaction> tradesClosed { get; set; } // TODO: verify this
		public Transaction tradeReduced { get; set; } // TODO: verify this
	}
}
