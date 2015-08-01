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
﻿namespace QuantConnect.Brokerages.Oanda.DataType
{
    /// <summary>
    /// Represents the Oanda Account.
    /// </summary>
    public class Account
    {
		public bool HasAccountId;
		private int _accountId;
		public int accountId
		{
			get { return _accountId; }
			set
			{
				_accountId = value;
				HasAccountId = true;
			}
		}

		public bool HasAccountName;
		private string _accountName;
		public string accountName
		{
			get { return _accountName; }
			set
			{
				_accountName = value;
				HasAccountName = true;
			}
		}

		public bool HasAccountCurrency;
		private string _accountCurrency;
		public string accountCurrency
		{
			get { return _accountCurrency; }
			set
			{
				_accountCurrency = value;
				HasAccountCurrency = true;
			}
		}

		public bool HasMarginRate;
		private string _marginRate;
		public string marginRate
		{
			get { return _marginRate; }
			set
			{
				_marginRate = value;
				HasMarginRate = true;
			}
		}

		[IsOptional]
		public bool HasBalance;
		private string _balance;
		public string balance
		{
			get { return _balance; }
			set
			{
				_balance = value;
				HasBalance = true;
			}
		}

		[IsOptional]
		public bool HasUnrealizedPl;
		private string _unrealizedPl;
		public string unrealizedPl
		{
			get { return _unrealizedPl; }
			set
			{
				_unrealizedPl = value;
				HasUnrealizedPl = true;
			}
		}

		[IsOptional]
		public bool HasRealizedPl;
		private string _realizedPl;
		public string realizedPl
		{
			get { return _realizedPl; }
			set
			{
				_realizedPl = value;
				HasRealizedPl = true;
			}
		}

		[IsOptional]
		public bool HasMarginUsed;
		private string _marginUsed;
		public string marginUsed
		{
			get { return _marginUsed; }
			set
			{
				_marginUsed = value;
				HasMarginUsed = true;
			}
		}

		[IsOptional]
		public bool HasMarginAvail;
		private string _marginAvail;
		public string marginAvail
		{
			get { return _marginAvail; }
			set
			{
				_marginAvail = value;
				HasMarginAvail = true;
			}
		}
		
		[IsOptional]
		public bool HasOpenTrades;
		private string _openTrades;
		public string openTrades
		{
			get { return _openTrades; }
			set
			{
				_openTrades = value;
				HasOpenTrades = true;
			}
		}
		
		[IsOptional]
		public bool HasOpenOrders;
		private string _openOrders;
		public string openOrders
		{
			get { return _openOrders; }
			set
			{
				_openOrders = value;
				HasOpenOrders = true;
			}
		}
    }
}