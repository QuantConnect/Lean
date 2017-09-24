/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Threading;
using System.Globalization;

// ToDo: When a command is given, create a default event - command applied.........

namespace QuantConnect.ToolBox.IQFeed
{
 
    public class ClientStatsEventArgs : EventArgs 
    {
        internal ClientStatsEventArgs(string line)
        {
            var fields = line.Split(',');

            lock (this)
            {
                switch (fields[2])
                {
                    case "0": _type = PortType.Admin; break;
                    case "1": _type = PortType.Level1; break;
                    case "2": _type = PortType.Level2; break;
                    default: _type = PortType.Lookup; break;
                }
                if (!int.TryParse(fields[3], out _clientId)) _clientId = 0;
                _clientName = fields[4];
                if (!DateTime.TryParseExact(fields[5], "yyyyMMdd HHmmss", _enUS, DateTimeStyles.None, out _startTime)) _startTime = DateTime.MinValue;
                if (!int.TryParse(fields[6], out _symbolsWatched)) _symbolsWatched = 0;
                if (!int.TryParse(fields[7], out _regionalSymbolsWatched)) _regionalSymbolsWatched = 0;
                if (!double.TryParse(fields[8], out _kbReceived)) _kbReceived = 0;
                if (!double.TryParse(fields[9], out _kbSent)) _kbSent = 0;
                if (!double.TryParse(fields[10], out _kbQueued)) _kbQueued = 0;
            }
        }

        public PortType type { get { lock (this) return _type; } }
        public int clientId { get { lock (this) return _clientId; } }
        public string clientName { get { lock (this) return _clientName; } }
        public DateTime startTime { get { lock (this) return _startTime; } }
        public int symbolsWatched { get { lock (this) return _symbolsWatched; } }
        public int regionalSymbolsWatched { get { lock (this) return _regionalSymbolsWatched; } }
        public double kbReceived { get { lock (this) return _kbReceived; } }
        public double kbSent { get { lock (this) return _kbSent; } }
        public double kbQueued { get { lock (this) return _kbQueued; } }

        #region private
        private PortType _type;
        private int _clientId;
        private string _clientName;
        private DateTime _startTime;
        private int _symbolsWatched;
        private int _regionalSymbolsWatched;
        private double _kbReceived;
        private double _kbSent;
        private double _kbQueued;
        private CultureInfo _enUS = new CultureInfo("en-US");
        #endregion
    }
    public class ConnectedEventArgs : EventArgs
    {
    }
    public class DisconnectedEventArgs : EventArgs
    {
    }

    public class IQAdminSocketClient : SocketClient 
    {
        public event EventHandler<ClientStatsEventArgs> ClientStatsEvent;
        public event EventHandler<ConnectedEventArgs> ConnectedEvent;
        public event EventHandler<DisconnectedEventArgs> DisconnectedEvent;

        public IQAdminSocketClient(int bufferSize) : base(IQSocket.GetEndPoint(PortType.Admin), bufferSize)
        {
            _status = new Status();
        }
        public void Connect(int retries = 10, int wait = 1000, int flushSeconds = 2)
        {
            ConnectToSocketAndBeginReceive(IQSocket.GetSocket());
            Send("S,CONNECT\r\n");
            for (var i = 0; i < retries; i++)
            {
                if (_status.connected) { return; }
                Thread.Sleep(wait);
            }
            throw new Exception("Timeout: No Connect message received from IQFeed");
        }
        public void Disconnect(int retries = 5, int wait = 1000, int flushSeconds = 2)
        {
            if (_status.connected)
            {
                Send("S,DISCONNECT\r\n");
                for (var i = 0; i < retries; i++)
                {
                    if (!_status.connected)
                    {
                        break;
                    }
                    Thread.Sleep(wait);
                }
            }
            DisconnectFromSocket(flushSeconds);
        }
        public void SetClientStats(bool flag = true)
        {
            if (flag) { Send("S,CLIENTSTATS ON\r\n"); }
            else { Send("S,CLIENTSTATS OFF\r\n"); }
        }
        public void SetClientName(string name)
        {
            Send("S,SET CLIENT NAME," + name + "\r\n"); 
        }
        public void RegisterClientApp(string application, string version)
        {
            Send("S,REGISTER CLIENT APP,"+application+","+version+"\r\n");
        }
        public void RemoveClientApp(string application, string version)
        {
            Send("S,REMOVE CLIENT APP," + application + "," + version + "\r\n");
        }
        public void SetLoginId(string loginId)
        {
            Send("S,SET LOGINID,"+loginId+"\r\n");
        }
        public void SetPassword(string password)
        {
            Send("S,SET PASSWORD," + password + "\r\n");
        }
        public void SetSaveCredentials(bool save = true)
        {
            if (save)
            {
                Send("S,SET SAVE LOGIN INFO,On\r\n");
            }
            else
            {
                Send("S,SET SAVE LOGIN INFO,Off\r\n");
            }
        }
        public void SetAutoconnect(bool auto = true)
        {
            if (auto)
            {
                Send("S,SET AUTOCONNECT,On\r\n");
            }
            else
            {
                Send("S,SET AUTOCONNECT,Off\r\n");
            }
        }

        public Status status { get { return _status; } }

        protected override void OnTextLineEvent(TextLineEventArgs e)
        {
            if (e.textLine.StartsWith("S,STATS,"))
            {
                _status.Update(e.textLine);
                return;
            }
            if (e.textLine.StartsWith("S,CLIENTSTATS,"))
            {
                OnClientStatsEvent(new ClientStatsEventArgs(e.textLine));
                return;
            }
            if (e.textLine.StartsWith("S,REGISTER CLIENT APP COMPLETED,"))
            {
                // placeholder for event
                return;
            }
            if (e.textLine.StartsWith("S,REMOVE CLIENT APP COMPLETED,"))
            {
                // placeholder for event
                return;
            }
            if (e.textLine.StartsWith("S,CURRENT LOGINID,"))
            {
                // placeholder for event
                return;
            }
            if (e.textLine.StartsWith("S,CURRENT PASSWORD,"))
            {
                // placeholder for event
                return;
            }
            if (e.textLine.StartsWith("S,LOGIN INFO "))
            {
                // placeholder for event
                return;
            }
            if (e.textLine.StartsWith("S,AUTOCONNECT "))
            {
                // placeholder for event
                return;
            }

            throw new Exception("(Admin) NOT HANDLED:" + e.textLine);
         }
        protected virtual void OnClientStatsEvent(ClientStatsEventArgs e)
        {
            if (ClientStatsEvent != null) ClientStatsEvent(this, e);
        }
        protected virtual void OnConnectedEvent(ConnectedEventArgs e)
        {
            if (ConnectedEvent != null) ConnectedEvent(this, e);
        }
        protected virtual void OnDisconnectedEvent(DisconnectedEventArgs e)
        {
            if (DisconnectedEvent != null) DisconnectedEvent(this, e);
        }


        #region private
        private Status _status;
        #endregion
    }
}
