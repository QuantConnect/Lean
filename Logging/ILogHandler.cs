namespace QuantConnect.Logging
{
    /// <summary>
    /// Interface for redirecting log output
    /// </summary>
    public interface ILogHandler
    {
        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text"></param>
        void Error(string text);
       
        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text"></param>
        void Debug(string text);
       
        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text"></param>
        void Trace(string text);
    }
}