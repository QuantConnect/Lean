using OANDARestLibrary.TradeLibrary.DataTypes;

namespace QuantConnect.Brokerages.Oanda.DataType
{
    /// <summary>
    /// Represents a single event when subscribed to the streaming events.
    /// </summary>
	public class Event : IHeartbeat
	{
		public Heartbeat heartbeat { get; set; }
		public Transaction transaction { get; set; }
		public bool IsHeartbeat()
		{
			return (heartbeat != null);
		}
	}
}
