using System.Collections.Generic;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides a depth-only traversable tree
    /// </summary>
    public interface ITree<out T>
    {
        /// <summary>
        /// Gets the value contained at this tree node
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Gets an enumerable of child nodes
        /// </summary>
        IEnumerable<ITree<T>> Children { get; }
    }
}
