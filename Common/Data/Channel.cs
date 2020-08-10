using System;

namespace QuantConnect.Data
{

    /// <summary>
    /// Represents a subscription channel
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The ticker symbol of the channel
        /// </summary>
        public Symbol Symbol { get; private set; }

        /// <summary>
        /// Creates an instance of subscription channel
        /// </summary>
        /// <param name="channelName">Socket channel name</param>
        /// <param name="symbol">Associated symbol</param>
        public Channel(string channelName, Symbol symbol)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentNullException("channelName", "Channel Name can't be null or empty");
            }

            if (symbol == null)
            {
                throw new ArgumentNullException("symbol", "Symbol can't be null or empty");
            }

            Name = channelName;
            Symbol = symbol;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Channel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other?.Name) && Symbol.Equals(other.Symbol);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Channel);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash * 16777619) ^ Name.GetHashCode();
                hash = (hash * 16777619) ^ Symbol.GetHashCode();
                return hash;
            }
        }
    }
}
