namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class OptionSymbolGenerator : SymbolGenerator
    {
        public OptionSymbolGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
            : base(settings, random)
        {
        }

        protected override Symbol GenerateSingle()
            => Random.NextOption(Settings.Market, Settings.Start, Settings.End, 100m, 75m);
    }
}
