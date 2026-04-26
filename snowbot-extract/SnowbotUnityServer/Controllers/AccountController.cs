using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using SnowbotUnityServer.Data;
using System.Reflection;

namespace SnowbotUnityServer.Controllers
{
    [Route("admin/user/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        public AccountController(ApiKeyStorageService apiKey, CharacterCacheService cacheService)
        {
            ApiKey=apiKey;
            CacheService=cacheService;
        }

        public ApiKeyStorageService ApiKey { get; }
        public CharacterCacheService CacheService { get; }

        [HttpGet]
        public IActionResult VerifyByUsername(string password, string username, string? token = null)
        {
            if (string.IsNullOrEmpty(password))
            {
                return BadRequest($"Invalid Password");
            }
            if (password != "789456123@!")
            {
                return BadRequest($"Invalid Password");
            }
            try
            {
                var charactersData = CacheService.GetCharactersData();

                // Vérification par token si fourni
                if (!string.IsNullOrEmpty(token))
                {
                    if (charactersData.TryGetValue(token, out var charactersList))
                    {
                        var character = charactersList.FirstOrDefault(c => c.Characters.Any(ch => ch.CharacterBasicInformation.Name.Equals(username, StringComparison.OrdinalIgnoreCase)));
                        if (character != null)
                        {
                            return Ok($"1|{token}|{character.Characters.First(ch => ch.CharacterBasicInformation.Name.Equals(username, StringComparison.OrdinalIgnoreCase)).Id}");
                        }
                    }
                }
                else
                {
                    // Vérification dans tous les tokens
                    foreach (var entry in charactersData)
                    {
                        var character = entry.Value.FirstOrDefault(c => c.Characters.Any(ch => ch.CharacterBasicInformation.Name.Equals(username, StringComparison.OrdinalIgnoreCase)));
                        if (character != null)
                        {
                            return Ok($"1|{entry.Key}|{character.Characters.First(ch => ch.CharacterBasicInformation.Name.Equals(username, StringComparison.OrdinalIgnoreCase)).Id}");
                        }
                    }
                }

                return Ok("0|NotFound|0"); // Utilisateur non trouvé
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult VerifyByCharacterId(string password,long characterId, string? token = null)
        {
            if (string.IsNullOrEmpty(password))
            {
                return BadRequest($"Invalid Password");
            }
            if (password != "789456123@!")
            {
                return BadRequest($"Invalid Password");
            }
            try
            {
                var charactersData = CacheService.GetCharactersData();

                // Vérification par token si fourni
                if (!string.IsNullOrEmpty(token))
                {
                    if (charactersData.TryGetValue(token, out var charactersList))
                    {
                        var character = charactersList.FirstOrDefault(c => c.Characters.Any(ch => ch.Id == characterId));
                        if (character != null)
                        {
                            return Ok($"1|{token}|{character.Characters.First(ch => ch.Id == characterId).CharacterBasicInformation.Name}");
                        }
                    }
                }
                else
                {
                    // Vérification dans tous les tokens
                    foreach (var entry in charactersData)
                    {
                        var character = entry.Value.FirstOrDefault(c => c.Characters.Any(ch => ch.Id == characterId));
                        if (character != null)
                        {
                            return Ok($"1|{entry.Key}|{character.Characters.First(ch => ch.Id == characterId).CharacterBasicInformation.Name}");
                        }
                    }
                }

                return Ok("0|NotFound|0"); // Personnage non trouvé
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult RefreshCache()
        {
            try
            {
                CacheService.RefreshCache();
                return Ok("Cache rafraîchi avec succès");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur lors du rafraîchissement du cache: {ex.Message}");
            }
        }
        [HttpGet]
        public async Task<IActionResult> check_file(string token, string id, string hint)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Ok("NOT_AUTHORIZED");
            }
            if (ApiKey.GetApiKeys().Where(x => x.Key == token).Count() == 0)
            {
                return Ok("NOT_AUTHORIZED");
            }
            try
            {
                string result = "NOK";
                if (Program.MapIdsHints.ContainsKey(id) && Program.MapIdsHints[id].Contains(hint))
                {
                    result = "OK";
                }
                return Ok(result);



            }
            catch (Exception ex)
            {
                return Ok("NOT_AUTHORIZED");
            }
        }
        [HttpGet]
        public async Task<IActionResult> SubscriptionExpiration(string token)
        {
            try
            {


                var apiKey = ApiKey.GetApiKeys().FirstOrDefault(x => x.Key == token && x.ExpirationDate > DateTime.UtcNow);
                if (apiKey == null)
                {
                    return Ok(""); //not found
                }
                if (apiKey.ExpirationDate < DateTime.UtcNow)
                {
                    return Ok($"{apiKey.ExpirationDate}"); 
                }

                return Ok($"{apiKey.ExpirationDate}"); 

            }
            catch (Exception ex)
            {
                return BadRequest("");
            }
        }
        [HttpGet]
        public async Task<IActionResult> SubscriptionType(string token)
        {

            try
            {


                var apiKey = ApiKey.GetApiKeys().FirstOrDefault(x => x.Key == token && x.ExpirationDate > DateTime.UtcNow);
                if (apiKey == null)
                {
                    return Ok("0||0|0"); //not found
                }
                if (apiKey.ExpirationDate < DateTime.UtcNow)
                {

                    return Ok($"{0}|{apiKey.Email}|0|0"); // abonnement 25e expiré


                }
               
                return Ok($"{apiKey.InstanceCount}|{apiKey.Email}|{1000}|{1000}"); // abonnement 50 valide + bots à afficher


            }
            catch (Exception ex)
            {
                return BadRequest("Not Found");
            }

        }

    }
}
