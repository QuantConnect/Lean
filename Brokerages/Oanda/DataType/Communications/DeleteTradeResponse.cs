using OANDARestLibrary.TradeLibrary.DataTypes.Communications;

namespace QuantConnect.Brokerages.Oanda.DataType.Communications
{
	public class DeleteTradeResponse : Response
	{
		public int id { get; set; }
		public double price { get; set; }
		public string instrument { get; set; }
		public double profit { get; set; }
		public string side { get; set; }
		public string time { get; set; }
	}
}
