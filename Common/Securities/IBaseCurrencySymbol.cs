namespace QuantConnect.Securities
{
    public interface IBaseCurrencySymbol
    {
        /// <summary>
        /// Gets the currency acquired by going long this currency pair
        /// </summary>
        /// <remarks>
        /// For example, the EUR/USD has a base currency of the euro, and as a result
        /// of going long the EUR/USD a trader is acquiring euros in exchange for US dollars
        /// </remarks>
        string BaseCurrencySymbol { get; }
    }
}