using System.IO;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Represents a stream reader capable of reading lines from disk
    /// </summary>
    public class FileSubscriptionStreamReader : IStreamReader
    {
        private readonly StreamReader _streamReader;

        public FileSubscriptionStreamReader(string source)
        {
            // unzip if necessary
            _streamReader = source.GetExtension() == ".zip" 
                ? Compression.Unzip(source) 
                : new StreamReader(source);
        }

        public SubscriptionTransportMedium TransportMedium
        {
            get { return SubscriptionTransportMedium.File; }
        }

        public bool EndOfStream
        {
            get
            {
                if (_streamReader == null)
                {
                    return true;
                }

                return _streamReader.EndOfStream;
            }
        }

        public string ReadLine()
        {
            return _streamReader.ReadLine();
        }

        public void Close()
        {
            if (_streamReader != null)
            {
                _streamReader.Close();
            }
        }

        public void Dispose()
        {
            if (_streamReader != null)
            {
                _streamReader.Dispose();
            }
        }
    }
}