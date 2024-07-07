namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Exception thrown when there are no tickers left to generate for a certain combination of security type and market.
    /// </summary>
    public class NoTickersAvailableException : RandomValueGeneratorException
    {
        public NoTickersAvailableException(SecurityType securityType, string market)
            : base(
                $"Failed to generate {securityType} symbol for {market}, there are no tickers left"
            ) { }
    }
}
