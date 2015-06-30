namespace QuantConnect.Brokerages.Oanda.DataType
{
    /// <summary>
    /// Reprensents the Rate received event in a Rate Stream.
    /// </summary>
	public class RateStreamResponse : IHeartbeat
	{
		public Heartbeat heartbeat;
		public Price tick;
		public bool IsHeartbeat()
		{
			return (heartbeat != null);
		}
	}
}
