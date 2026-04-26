using Microsoft.Extensions.Caching.Memory;
using Server.Game;
using System.Collections.Concurrent;

namespace SnowbotUnityServer.Data
{
    // Service de cache pour les données des personnages
    public class CharacterCacheService
    {
        private readonly IMemoryCache _cache;
        private const string CacheKey = "CharactersData";
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public CharacterCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public ConcurrentDictionary<string, List<CharacterListEvent>> GetCharactersData()
        {
            // Vérifie si les données sont en cache
            if (!_cache.TryGetValue(CacheKey, out ConcurrentDictionary<string, List<CharacterListEvent>> charactersData))
            {
                // Si non en cache, récupère depuis TcpListener
                charactersData = TcpListener.DofusListener.CharactersForEachApiKey;

                // Stocke dans le cache pour 5 minutes
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_cacheExpiration);

                _cache.Set(CacheKey, charactersData, cacheOptions);
            }

            return charactersData;
        }

        // Force le rafraîchissement du cache si nécessaire
        public void RefreshCache()
        {
            var charactersData = TcpListener.DofusListener.CharactersForEachApiKey;

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_cacheExpiration);

            _cache.Set(CacheKey, charactersData, cacheOptions);
        }
    }
}
