using System;
using System.IO;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Transport;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    public class DefaultDataFileCacheProvider : IDataFileCacheProvider
    {
        public Stream Fetch(string source, DateTime date, string entryName)
        {
           return source.GetExtension() == ".zip"
                ? Compression.UnzipBaseStream(source, entryName)
                : new FileStream(source, FileMode.Open, FileAccess.Read);
        }

        public void Store(string source, byte[] data)
        {
            //
        }

        public void Dispose()
        {
            //
        }
    }
}
