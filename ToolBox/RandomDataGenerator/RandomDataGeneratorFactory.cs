namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class RandomDataGeneratorFactory
    {
        public static IRandomDataGenerator CreateGenerator(RandomDataGeneratorSettings settings, RandomValueGenerator random)
        {
            switch (settings.SecurityType)
            {
                case SecurityType.Option:
                    return new OptionRandomDataGenerator(settings, random);

                case SecurityType.Future:
                    return new FutureRandomDataGenerator(settings, random);

                default:
                    return new SpotRandomDataGenerator(settings, random);
            }
        }
    }
}
