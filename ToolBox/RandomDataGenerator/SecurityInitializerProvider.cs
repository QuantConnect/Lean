using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class SecurityInitializerProvider : ISecurityInitializerProvider
    {
        public ISecurityInitializer SecurityInitializer { get; }

        public SecurityInitializerProvider(ISecurityInitializer securityInitializer)
        {
            SecurityInitializer = securityInitializer;
        }
    }
}
