using OANDARestLibrary.TradeLibrary.DataTypes;

namespace QuantConnect.Brokerages.Oanda.DataType
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
