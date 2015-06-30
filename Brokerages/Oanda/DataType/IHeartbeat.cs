namespace QuantConnect.Brokerages.Oanda.DataType
{
    /// <summary>
    /// Represents the interface for the HeartBeat and RateStreamResponse class.
    /// </summary>
	public interface IHeartbeat
	{
		bool IsHeartbeat();
	}
}
