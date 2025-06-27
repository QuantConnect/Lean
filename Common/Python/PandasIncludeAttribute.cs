

using System;

namespace QuantConnect.Python
{
    /// <summary>
    /// Attribute to mark a property or field as explicitly included 
    /// when converting an instance to a pandas DataFrame row.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PandasIncludeAttribute : Attribute
    {
    }
}