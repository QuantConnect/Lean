namespace QuantConnect.Data.Market
{
    /// <summary>
    /// The type of the RenkoBar
    /// </summary>
    public enum RenkoType
    {
        /// <summary>
        /// Indicates that the RenkoConsolidator works in a 
        /// "Classic" manner (ie. that it only returns a single 
        /// bar, at most, irrespective of tick movement).  
        /// NOTE: the Classic mode has only been retained for 
        /// backwards compatability with existing code.
        /// </summary>
        Classic,

        /// <summary>
        /// Indicates that the RenkoConsolidator works properly,
        /// and returns zero or more bars per tick, as appropriate.
        /// </summary>
        Wicked
    }
}
