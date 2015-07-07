using System.Collections.Generic;
using OANDARestLibrary.TradeLibrary.DataTypes.Communications;

namespace QuantConnect.Brokerages.Oanda.DataType.Communications
{
	public class DeletePositionResponse : Response
	{
		public List<int> ids { get; set; }
		public string instrument { get; set; }
		public int totalUnits { get; set; }
		public double price { get; set; }
	}
}
