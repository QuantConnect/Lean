namespace QuantConnect.Brokerages.Oanda.Framework
{
    /// <summary>
    /// Custom Event Arguments for Oanda Event Handlers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CustomEventArgs<T>
    {
        public CustomEventArgs(T content)
        {
            Item = content;
        }

        public T Item { get; private set; }
    }
}
