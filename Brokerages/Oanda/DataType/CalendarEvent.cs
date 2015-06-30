namespace QuantConnect.Brokerages.Oanda.DataType
{
    /// <summary>
    /// Represents the Oanda Calendar Event.
    /// </summary>
	public class CalendarEvent
	{
		public string title;
		public string timestamp;
		public string unit;
		public string currency;
		public string forecast;
		public string previous;
		public string actual;
		public string market;
	}
}
