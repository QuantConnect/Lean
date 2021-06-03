
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests
{
    public static class TestGlobals
    {
        public static IDataProvider DataProvider = new DefaultDataProvider();
        public static IMapFileProvider MapFileProvider = new LocalDiskMapFileProvider();
        public static IFactorFileProvider FactorFileProvider = new LocalDiskFactorFileProvider();

        /// <summary>
        /// Initialize our providers, called by AssemblyInitialize.cs so all tests
        /// can access initialized providers
        /// </summary>
        public static void Initialize()
        {
            MapFileProvider.Initialize(DataProvider);
            FactorFileProvider.Initialize(MapFileProvider, DataProvider);
        }
    }
}
