using System.IO;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Represents a stream reader capable of reading lines from disk
    /// </summary>
    public class LocalFileSubscriptionStreamReader : IStreamReader
    {
        private readonly StreamReader _streamReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="source">The local file to be read</param>
        public LocalFileSubscriptionStreamReader(string source)
        {
            // unzip if necessary
            _streamReader = source.GetExtension() == ".zip" 
                ? Compression.Unzip(source) 
                : new StreamReader(source);
        }

        /// <summary>
        /// Gets the transport medium of this stream reader
        /// </summary>
        public SubscriptionTransportMedium TransportMedium
        {
            get { return SubscriptionTransportMedium.LocalFile; }
        }

        /// <summary>
        /// Gets whether or not there's more data to be read in the stream
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                return _streamReader == null || _streamReader.EndOfStream;
            }
        }

        /// <summary>
        /// Gets the next line/batch of content from the stream 
        /// </summary>
        public string ReadLine()
        {
            return _streamReader.ReadLine();
        }

        /// <summary>
        /// Closes the stream
        /// </summary>
        public void Close()
        {
            if (_streamReader != null)
            {
                _streamReader.Close();
            }
        }

        /// <summary>
        /// Disposes of the stream
        /// </summary>
        public void Dispose()
        {
            if (_streamReader != null)
            {
                _streamReader.Dispose();
            }
        }
    }
}