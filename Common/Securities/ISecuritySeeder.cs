using QuantConnect.Data;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Used to seed the security with the correct price
    /// </summary>
    public interface ISecuritySeeder
    {
        BaseData GetSeedData(Security security);
    }
}
