namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Exception thrown when multiple attempts to generate a valid random value end in failure
    /// </summary>
    public class TooManyFailedAttemptsException : RandomValueGeneratorException
    {
        public TooManyFailedAttemptsException(string method, int attempts)
            : base($"Failed to generate a valid value for '{method}' after {attempts} attempts.")
        {
        }
    }
}