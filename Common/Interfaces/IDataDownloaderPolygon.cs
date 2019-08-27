using System;
namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Wrapper on the API for getting data from polygon.io
    /// </summary>
    public interface IDataDownloaderPolygon
    {
        /// <summary>
        /// Authenticate the api
        /// </summary>
        void Authenticate(String ApiKey);
    }
}
