namespace QuantConnect.OandaDownloader
{
    /// <summary>
    /// Represents a single instrument as listed in the file instruments.txt
    /// </summary>
    public class LeanInstrument
    {
        /// <summary>
        /// The symbol of the instrument
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// The name/description of the instrument
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The instrument type
        /// </summary>
        public InstrumentType Type { get; set; }
    }
}