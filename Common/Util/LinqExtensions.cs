using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides more extension methods for the enumerable types
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Creates a dictionary multimap from the lookup.
        /// </summary>
        /// <typeparam name="K">The key type</typeparam>
        /// <typeparam name="V">The value type</typeparam>
        /// <param name="lookup">The ILookup instance to convert to a dictionary</param>
        /// <returns>A dictionary holding the same data as 'lookup'</returns>
        public static Dictionary<K, List<V>> ToDictionary<K, V>(this ILookup<K, V> lookup)
        {
            return lookup.ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
        }
    }
}
