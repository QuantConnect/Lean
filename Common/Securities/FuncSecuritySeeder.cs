using System;
using QuantConnect.Data;
using QuantConnect.Logging;

namespace QuantConnect.Securities
{
    public class FuncSecuritySeeder : ISecuritySeeder
    {
        private Func<Security, BaseData> _historySeeder;

        public FuncSecuritySeeder(Func<Security, BaseData> historySeeder)
        {
            _historySeeder = historySeeder;
        }

        public BaseData GetLastData(Security security)
        {
            try
            {
                return _historySeeder(security);
            }
            catch (Exception ex)
            {
                Log.Error("FuncSecuritySeeder.GetSeedPrice(): " + ex.GetBaseException());
            }

            Log.Trace("FuncSecuritySeeder.GetSeedPrice(): Could not seed price for security {0} from history.", security.Symbol);

            return null;
        }
    }
}
