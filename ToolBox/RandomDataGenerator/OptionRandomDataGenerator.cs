namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class OptionRandomDataGenerator : BaseRandomDataGenerator
    {
        public OptionRandomDataGenerator(RandomDataGeneratorSettings settings, RandomValueGenerator random)
            : base(settings, random)
        {
        }

        public override SymbolGenerator CreateSymbolGenerator()
            => new OptionSymbolGenerator(Settings, Random, 100m, 75m);

        public override ITickGenerator CreateTickGenerator()
            => new BlackScholesTickGenerator(Settings, Random);
    }
}
