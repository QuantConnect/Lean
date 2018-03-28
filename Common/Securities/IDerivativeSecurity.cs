namespace QuantConnect.Securities
{
    /// <summary>
    /// Defines a security as a derivative of another security
    /// </summary>
    public interface IDerivativeSecurity
    {
        /// <summary>
        /// Gets or sets the underlying security for the derivative
        /// </summary>
        Security Underlying { get; set; }
    }
}