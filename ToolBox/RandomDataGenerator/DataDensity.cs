namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Specifies how dense data should be generated
    /// </summary>
    public enum DataDensity
    {
        /// <summary>
        /// At least once per resolution step
        /// </summary>
        Dense,

        /// <summary>
        /// At least once per 5 resolution steps
        /// </summary>
        Sparse,

        /// <summary>
        /// At least once per 50 resolution steps
        /// </summary>
        VerySparse
    }
}