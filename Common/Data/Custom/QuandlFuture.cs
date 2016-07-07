namespace QuantConnect.Data.Custom
{
    /// <summary>
    /// Custom quandl data type for setting customized value column name. Value column is used for the primary trading calculations and charting.
    /// </summary>
    public class QuandlFuture : Quandl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuandlFuture"/> class.
        /// </summary>
        public QuandlFuture() : base("Settle") { }
    }
}