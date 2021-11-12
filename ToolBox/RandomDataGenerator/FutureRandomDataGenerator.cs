namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class FutureRandomDataGenerator : BaseRandomDataGenerator
    {
        public FutureRandomDataGenerator(RandomDataGeneratorSettings settings, RandomValueGenerator random)
            : base(settings, random)
        {
        }

        public override SymbolGenerator CreateSymbolGenerator()
            => new FutureSymbolGenerator(Settings, Random);

        public override ITickGenerator CreateTickGenerator()
            => new TickGenerator(Settings, Random);
    }
}
