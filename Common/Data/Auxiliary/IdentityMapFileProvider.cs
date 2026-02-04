/*
 * Cascade Labs - Identity Map File Provider
 * Falls back to identity mapping when map files aren't available
 */

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Logging;
using QuantConnect.Interfaces;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Map file provider that falls back to identity mapping when files aren't found.
    /// This allows using symbols without requiring QuantConnect's map file data.
    /// </summary>
    public class IdentityMapFileProvider : IMapFileProvider
    {
        private static int _wroteTraceStatement;
        private readonly ConcurrentDictionary<AuxiliaryDataKey, IdentityMapFileResolver> _cache;
        private IDataProvider _dataProvider;

        /// <summary>
        /// Default first trading date for identity mappings
        /// </summary>
        public static readonly DateTime DefaultFirstDate = new DateTime(1998, 1, 2);

        /// <summary>
        /// Creates a new instance of the <see cref="IdentityMapFileProvider"/>
        /// </summary>
        public IdentityMapFileProvider()
        {
            _cache = new ConcurrentDictionary<AuxiliaryDataKey, IdentityMapFileResolver>();
        }

        /// <summary>
        /// Initializes the provider with a data provider
        /// </summary>
        public void Initialize(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Gets a <see cref="MapFileResolver"/> for the specified market/security type.
        /// Returns identity mappings for symbols without map files.
        /// </summary>
        public MapFileResolver Get(AuxiliaryDataKey auxiliaryDataKey)
        {
            return _cache.GetOrAdd(auxiliaryDataKey, GetMapFileResolver);
        }

        private IdentityMapFileResolver GetMapFileResolver(AuxiliaryDataKey key)
        {
            var securityType = key.SecurityType;
            var market = key.Market;

            var mapFileDirectory = Globals.GetDataFolderPath(MapFile.GetRelativeMapFilePath(market, securityType));

            IEnumerable<MapFile> mapFiles;
            if (!Directory.Exists(mapFileDirectory))
            {
                if (Interlocked.CompareExchange(ref _wroteTraceStatement, 1, 0) == 0)
                {
                    Log.Trace($"IdentityMapFileProvider: Map file directory not found, using identity mappings: {mapFileDirectory}");
                }
                mapFiles = Enumerable.Empty<MapFile>();
            }
            else
            {
                mapFiles = MapFile.GetMapFiles(mapFileDirectory, market, securityType, _dataProvider);
            }

            return new IdentityMapFileResolver(mapFiles);
        }

        /// <summary>
        /// Creates an identity map file for a symbol (symbol maps to itself with a default start date)
        /// </summary>
        public static MapFile CreateIdentityMapFile(string symbol)
        {
            var rows = new List<MapFileRow>
            {
                new MapFileRow(DefaultFirstDate, symbol, Exchange.UNKNOWN),
                new MapFileRow(Time.EndOfTime, symbol, Exchange.UNKNOWN)
            };
            return new MapFile(symbol, rows);
        }
    }

    /// <summary>
    /// MapFileResolver that returns identity mappings for unknown symbols
    /// </summary>
    public class IdentityMapFileResolver : MapFileResolver
    {
        private readonly ConcurrentDictionary<string, MapFile> _identityCache;

        public IdentityMapFileResolver(IEnumerable<MapFile> mapFiles)
            : base(mapFiles)
        {
            _identityCache = new ConcurrentDictionary<string, MapFile>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Resolves the map file for a symbol. Falls back to identity mapping if not found.
        /// </summary>
        public override MapFile ResolveMapFile(string symbol, DateTime date)
        {
            var mapFile = base.ResolveMapFile(symbol, date);

            // If the map file is empty (no rows), return an identity mapping
            if (mapFile == null || !mapFile.Any())
            {
                return _identityCache.GetOrAdd(symbol.ToUpperInvariant(),
                    s => IdentityMapFileProvider.CreateIdentityMapFile(s));
            }

            return mapFile;
        }
    }
}
