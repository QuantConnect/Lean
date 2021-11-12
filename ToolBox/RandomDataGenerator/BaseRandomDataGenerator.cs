namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public abstract class BaseRandomDataGenerator : IRandomDataGenerator
    {
        protected RandomDataGeneratorSettings Settings { get; }
        protected RandomValueGenerator Random { get; }

        public BaseRandomDataGenerator(RandomDataGeneratorSettings settings, RandomValueGenerator random)
        {
            Settings = settings;
            Random = random;
        }

        public abstract SymbolGenerator CreateSymbolGenerator();

        public abstract ITickGenerator CreateTickGenerator();
    }
}
