using System;
using System.Collections.Generic;
using System.Text;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Newtonsoft.Json;

namespace Levrum.Utils.Messaging
{
    public class RabbitMQProducer<T> : IDisposable
    {
        public bool Connected { get; set; }

        public string HostName { get; set; }
        public int Port { get; set; }

        public string Queue { get; set; }
        public bool Durable { get; set; }
        public bool Exclusive { get; set; }
        public bool AutoDelete { get; set; }
        public IDictionary<string, object> Arguments { get; set; }

        public IConnection Connection { get; protected set; }
        public IModel Channel { get; protected set; }
        public EventingBasicConsumer Consumer { get; protected set; }

        public RabbitMQProducer(string _hostName, int _port, string _queue)
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
            try
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
            catch (Exception ex)
            {

            }
        }

        public void SendObject(T obj, string exchange = "", IBasicProperties basicProperties = null)
        {
            if (!Connected || !TestConnection())
            {
                Connect();
            }

            string message = JsonConvert.SerializeObject(obj);
            var body = Encoding.UTF8.GetBytes(message);

            Channel.BasicPublish(exchange, Queue, basicProperties, body);
        }

        public void Dispose()
        {
            try
            {
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
