namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class SpotRandomDataGenerator : BaseRandomDataGenerator
    {
        public SpotRandomDataGenerator(RandomDataGeneratorSettings settings, RandomValueGenerator random)
            : base(settings, random)
        {
        }

        public override SymbolGenerator CreateSymbolGenerator()
            => new SpotSymbolGenerator(Settings, Random);

        public override ITickGenerator CreateTickGenerator()
            => new TickGenerator(Settings, Random);
    }
}
