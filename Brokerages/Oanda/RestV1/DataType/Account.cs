/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2012-2013 OANDA Corporation
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
 * documentation files (the "Software"), to deal in the Software without restriction, including without 
 * limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
 * Software, and to permit persons to whom the Software is furnished  to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
 * the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
 * WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace QuantConnect.Brokerages.Oanda.RestV1.DataType
{
#pragma warning disable 1591
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
#pragma warning restore 1591
}