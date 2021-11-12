namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class SpotSymbolGenerator : SymbolGenerator
    {
        public SpotSymbolGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
            : base(settings, random)
        {
        }

        protected override Symbol GenerateSingle()
            => Random.NextSymbol(Settings.SecurityType, Settings.Market);
    }
}
