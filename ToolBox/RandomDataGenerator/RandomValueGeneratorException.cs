using System;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Provides a base class for exceptions thrown by implementations of <see cref="IRandomValueGenerator"/>
    /// </summary>
    public class RandomValueGeneratorException : ApplicationException
    {
        public RandomValueGeneratorException(string message)
            : base(message)
        {
        }
    }
}