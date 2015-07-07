using System.Collections.Generic;
using OANDARestLibrary.TradeLibrary.DataTypes;
using OANDARestLibrary.TradeLibrary.DataTypes.Communications;

namespace QuantConnect.Brokerages.Oanda.DataType.Communications
{
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
