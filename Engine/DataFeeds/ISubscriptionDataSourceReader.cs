using System;
using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Represents a type responsible for accepting an input <see cref="SubscriptionDataSource"/>
    /// and returning an enumerable of the source's <see cref="BaseData"/>
    /// </summary>
    public interface ISubscriptionDataSourceReader
    {
        /// <summary>
        /// Event fired when the specified source is considered invalid, this may
        /// be from a missing file or failure to download a remote source
        /// </summary>
        event EventHandler<InvalidSourceEventArgs> InvalidSource;

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        IEnumerable<BaseData> Read(SubscriptionDataSource source);
    }
}