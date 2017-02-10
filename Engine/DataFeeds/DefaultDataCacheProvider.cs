using System.IO;
using Ionic.Zip;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    public class DefaultDataCacheProvider : IDataCacheProvider
    {
        private ZipFile _zipFile;

        public Stream Fetch(string source, string entryName)
        {
           return source.GetExtension() == ".zip"
                ? Compression.UnzipBaseStream(source, entryName, out _zipFile)
                : new FileStream(source, FileMode.Open, FileAccess.Read);
        }

        public void Store(string source, byte[] data)
        {
            // NOP
        }

        public void Dispose()
        {
            if (_zipFile != null)
            {
                _zipFile.Dispose();
            }
        }
    }
}
