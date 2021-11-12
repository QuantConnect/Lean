namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class OptionRandomDataGenerator : BaseRandomDataGenerator
    {
        public OptionRandomDataGenerator(RandomDataGeneratorSettings settings, RandomValueGenerator random)
            : base(settings, random)
        {
        }

        public override SymbolGenerator CreateSymbolGenerator()
            => new OptionSymbolGenerator(Settings, Random);

        public override ITickGenerator CreateTickGenerator()
            => new TickGenerator(Settings, Random);
    }
}
