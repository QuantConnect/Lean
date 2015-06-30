using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Oanda.DataType;

namespace QuantConnect.Brokerages.Oanda.Session
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class StreamSession<T> where T : IHeartbeat
    {
        public delegate void DataHandler(T data);

        protected readonly int _accountId;
        private WebResponse _response;
        private bool _shutdown;

        protected StreamSession(int accountId)
        {
            _accountId = accountId;
        }

        public event DataHandler DataReceived;

        public void OnDataReceived(T data)
        {
            var handler = DataReceived;
            if (handler != null) handler(data);
        }

        protected abstract Task<WebResponse> GetSession();

        public async void StartSession()
        {
            _shutdown = false;
            _response = await GetSession();


            Task.Run(() =>
            {
                var serializer = new DataContractJsonSerializer(typeof (T));
                var reader = new StreamReader(_response.GetResponseStream());
                while (!_shutdown)
                {
                    var memStream = new MemoryStream();

                    var line = reader.ReadLine();
                    memStream.Write(Encoding.UTF8.GetBytes(line), 0, Encoding.UTF8.GetByteCount(line));
                    memStream.Position = 0;

                    var data = (T) serializer.ReadObject(memStream);

                    // Don't send heartbeats
                    if (!data.IsHeartbeat())
                    {
                        OnDataReceived(data);
                    }
                }
            }
                );
        }

        public void StopSession()
        {
            _shutdown = true;
        }
    }
}