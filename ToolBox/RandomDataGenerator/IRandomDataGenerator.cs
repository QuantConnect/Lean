namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public interface IRandomDataGenerator
    {
        SymbolGenerator CreateSymbolGenerator();

        ITickGenerator CreateTickGenerator();
    }
}
