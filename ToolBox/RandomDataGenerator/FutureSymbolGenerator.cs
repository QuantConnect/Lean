namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class FutureSymbolGenerator : SymbolGenerator
    {
        public FutureSymbolGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
            : base(settings, random)
        {
        }

        protected override Symbol GenerateSingle()
            => Random.NextFuture(Settings.Market, Settings.Start, Settings.End);

    }
}
