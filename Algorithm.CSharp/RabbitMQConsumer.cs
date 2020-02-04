using System;
using System.Collections.Generic;
using System.Text;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Newtonsoft.Json;

namespace Levrum.Utils.Messaging
{
    public delegate void RabbitMQConsumerDelegate<T>(object sender, string message, T obj);

    public class RabbitMQConsumer<T> : IDisposable
    {
        public bool Connected { get; set; }

        public string HostName { get; set; }
        public int Port { get; set; }

        public string Queue { get; set; }
        public bool Durable { get; set; }
        public bool Exclusive { get; set; }
        public bool AutoDelete { get; set; }
        public IDictionary<string, object> Arguments { get; set; }

        public bool AutoAck { get; set; }

        public IConnection Connection { get; protected set; }
        public IModel Channel { get; protected set; }
        public EventingBasicConsumer Consumer { get; protected set; }

        public event RabbitMQConsumerDelegate<T> MessageReceived = null;

        public RabbitMQConsumer(string _hostName, int _port, string _queue)
        {
            HostName = _hostName;
            Port = _port;
            Queue = _queue;
        }

        public bool Connect()
        {
            try
            {
                if (!Connected || !TestConnection())
                {
                    var factory = new ConnectionFactory() { HostName = HostName, Port = Port, UseBackgroundThreadsForIO = true };
                    Connection = factory.CreateConnection();
                    Channel = Connection.CreateModel();
                    Channel.QueueDeclare(Queue, Durable, Exclusive, AutoDelete, Arguments);

                    Consumer = new EventingBasicConsumer(Channel);
                    Consumer.Received += onReceive;
                    Channel.BasicConsume(Queue, AutoAck, Consumer);
                    Connected = true;
                }
            }
            catch (Exception ex)
            {
                Connected = false;
            }
            return Connected;
        }

        public bool TestConnection()
        {
            return Connected;
        }

        public void Disconnect()
        {
            if (Channel != null)
            {
                Channel.Close();
            }

            if (Connection != null)
            {
                Connection.Close();
            }
        }

        private void onReceive(object sender, BasicDeliverEventArgs e)
        {
            if (MessageReceived != null)
            {
                try
                {
                    var body = e.Body;
                    var message = Encoding.UTF8.GetString(body);
                    T obj = JsonConvert.DeserializeObject<T>(message, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All, TypeNameHandling = TypeNameHandling.All });
                    MessageReceived.Invoke(this, message, obj);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public void Dispose()
        {
            try
            {
                MessageReceived = null;
                Consumer.Received -= onReceive;
                Consumer.HandleModelShutdown(Channel, new ShutdownEventArgs(ShutdownInitiator.Application, 0, "Disposing"));
            }
            catch { }
            try
            {
                Channel.Close();
                Channel.Dispose();
            }
            catch { }
            try
            {
                Connection.Close();
                Connection.Dispose();
            }
            catch { }
        }
    }
}
