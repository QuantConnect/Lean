using System.Collections.Generic;

namespace QuantConnect.Brokerages.Zerodha
{
    internal struct CacheObject
    {
        public string ETag;
        public string Data;
    }

    internal class Cache
    {
        Dictionary<string, CacheObject> memcache = new Dictionary<string, CacheObject>();

        public bool IsCached(string URL)
        {
            return memcache.ContainsKey(URL);
        }

        public string GetETag(string URL)
        {
            return memcache[URL].ETag;
        }

        public string GetData(string URL)
        {
            return memcache[URL].Data;
        }

        public void SetCache(string URL, CacheObject CacheItem)
        {
            memcache[URL] = CacheItem;
        }

        public void ClearCache()
        {
            memcache.Clear();
        }
    }
}
