using System.ComponentModel;
using QuantConnect.Brokerages.Oanda;
using QuantConnect.Brokerages.Oanda.DataType;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications.Requests
{
	public class CandlesRequest : Request
	{
		public SmartProperty<string> instrument;

		[IsOptional]
		[DefaultValue(EGranularity.S5)]
		public SmartProperty<EGranularity> granularity;

		[IsOptional]
		[DefaultValue(500)]
		public SmartProperty<int> count;

		[IsOptional]
		public SmartProperty<string> start;

		[IsOptional]
		public SmartProperty<string> end;

		[IsOptional]
		[DefaultValue(ECandleFormat.bidask)]
		public SmartProperty<ECandleFormat> candleFormat;

		[IsOptional]
		//[DefaultValue(true)]
		public SmartProperty<bool> includeFirst;

		[IsOptional]
		public SmartProperty<string> dailyAlignment;

		[IsOptional]
		public SmartProperty<string> weeklyAlignment;

		public override string EndPoint
		{
			get { return "candles"; }
		}

		public override Server GetServer()
		{
			return Server.Rates;
		}
	}

	

	public enum ECandleFormat
	{
		bidask,
		midpoint
	}

	public enum EGranularity
	{
		S5,
		S10,
		S15,
		S30,
		M1,
		M2,
		M3,
		M5,
		M10,
		M15,
		M30,
		H1,
		H2,
		H3,
		H4,
		H6,
		H8,
		H12,
		D,
		W,
		M
	}
}
