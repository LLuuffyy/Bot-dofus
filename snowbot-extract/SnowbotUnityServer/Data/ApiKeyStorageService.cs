using System.Text.Json;

namespace SnowbotUnityServer.Data
{
    public class ApiKeyInfo
    {
        public string Key { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime ExpirationDate { get; set; }
        public int InstanceCount { get; set; }
        public string Email { get; set; } = string.Empty; // Initialiser à vide pour éviter les nulls
    }

    public class ApiKeyStorageService
    {
        private readonly string _filePath = "ApiKeys.json";
        private List<ApiKeyInfo> _apiKeys = new();

        public ApiKeyStorageService()
        {
            LoadApiKeysFromFile();
        }

        private void LoadApiKeysFromFile()
        {
            if (File.Exists(_filePath))
            {
                try // Ajouter un try-catch pour la désérialisation
                {
                    string json = File.ReadAllText(_filePath);
                    // Gérer le cas où le fichier est vide ou contient "null"
                    if (!string.IsNullOrWhiteSpace(json) && json.Trim() != "null")
                    {
                        var data = JsonSerializer.Deserialize<List<ApiKeyInfo>>(json);
                        _apiKeys = data ?? new List<ApiKeyInfo>();
                    }
                    else
                    {
                        _apiKeys = new List<ApiKeyInfo>();
                    }
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine($"Erreur lors de la désérialisation du fichier {_filePath}: {ex.Message}");
                    // Gérer l'erreur, par exemple en initialisant une liste vide ou en lançant une exception plus spécifique
                    _apiKeys = new List<ApiKeyInfo>();
                }
                catch (Exception ex) // Attraper d'autres erreurs potentielles (ex: I/O)
                {
                    Console.Error.WriteLine($"Erreur lors de la lecture du fichier {_filePath}: {ex.Message}");
                    _apiKeys = new List<ApiKeyInfo>();
                }
            }
            else
            {
                _apiKeys = new List<ApiKeyInfo>(); // Initialiser si le fichier n'existe pas
            }
        }

        private void SaveApiKeysToFile()
        {
            try
            {
                string json = JsonSerializer.Serialize(_apiKeys, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erreur lors de la sauvegarde dans le fichier {_filePath}: {ex.Message}");
                // Gérer l'erreur (ex: logger, notifier l'utilisateur)
            }
        }

        public List<ApiKeyInfo> GetApiKeys() => _apiKeys;

        public void AddApiKey(ApiKeyInfo newApiKey)
        {
            // S'assurer qu'il n'y a pas de clé dupliquée (basé sur la propriété Key)
            if (!_apiKeys.Any(k => k.Key == newApiKey.Key))
            {
                _apiKeys.Add(newApiKey);
                SaveApiKeysToFile();
            }
            else
            {
                Console.Error.WriteLine($"Tentative d'ajout d'une clé API dupliquée: {newApiKey.Key}");
                // Optionnel: lever une exception ou retourner un booléen pour indiquer l'échec
            }
        }

        public void RemoveApiKey(ApiKeyInfo keyToRemove)
        {
            // Utiliser la clé unique pour trouver et supprimer l'élément
            var item = _apiKeys.FirstOrDefault(k => k.Key == keyToRemove.Key);
            if (item != null)
            {
                _apiKeys.Remove(item);
                SaveApiKeysToFile();
            }
        }

        // --- NOUVELLE MÉTHODE ---
        public void UpdateApiKey(ApiKeyInfo updatedKeyInfo)
        {
            // Trouver l'index de la clé existante en utilisant sa propriété Key unique
            int index = _apiKeys.FindIndex(k => k.Key == updatedKeyInfo.Key);

            if (index != -1) // Si la clé a été trouvée
            {
                // Remplacer l'ancien objet par le nouveau dans la liste
                // Ceci mettra à jour toutes les propriétés si elles ont changé,
                // mais ici on se concentre sur ExpirationDate
                _apiKeys[index] = updatedKeyInfo;

                // Sauvegarder la liste mise à jour dans le fichier
                SaveApiKeysToFile();
            }
            else
            {
                Console.Error.WriteLine($"Tentative de mise à jour d'une clé API non trouvée: {updatedKeyInfo.Key}");
                // Gérer le cas où la clé à mettre à jour n'existe pas/plus
            }
        }
    }
}