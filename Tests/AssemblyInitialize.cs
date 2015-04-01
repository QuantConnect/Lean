using NUnit.Framework;
using QuantConnect.Logging;

[SetUpFixture]
public class AssemblyInitialize
{
    [SetUp]
    public void SetLogHandler()
    {
        Log.LogHandler = new ConsoleLogHandler();
    }
}
