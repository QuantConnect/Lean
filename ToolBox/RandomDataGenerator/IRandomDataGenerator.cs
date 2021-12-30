namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public interface IRandomDataGenerator
    {
        BaseSymbolGenerator CreateSymbolGenerator();

        ITickGenerator CreateTickGenerator();
    }
}
