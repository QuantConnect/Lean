using System;
using System.ComponentModel.Composition;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Fetches a remote file for a security using a Symbol, Resolution and DateTime
    /// Used to get remote files when they are not found locally
    /// </summary>
    [InheritedExport(typeof(IFileProvider))]
    public interface IFileProvider
    {
        /// <summary>
        /// Gets and downloads the remote file
        /// </summary>
        /// <param name="symbol">Symbol of the security</param>
        /// <param name="resolution">Resolution of the data requested</param>
        /// <param name="date">DateTime of the data requested</param>
        /// <returns>Bool indicating whether the remote file was fetched correctly</returns>
        bool Fetch(Symbol symbol, Resolution resolution, DateTime date);
    }
}
