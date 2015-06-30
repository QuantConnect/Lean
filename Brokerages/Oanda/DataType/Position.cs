namespace QuantConnect.Brokerages.Oanda.DataType
{
    /// <summary>
    /// Represent a Position in Oanda.
    /// </summary>
    public class Position
    {
        public string side { get; set; }
        public string instrument { get; set; }
        public int units { get; set; }
        public double avgPrice { get; set; }
    }
}
