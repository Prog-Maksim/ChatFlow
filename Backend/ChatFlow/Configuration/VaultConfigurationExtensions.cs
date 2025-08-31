using System.Text.Json;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;

namespace ChatFlow.Configuration;

public static class VaultConfigurationExtensions
{
    public static async Task<IConfigurationBuilder> AddVaultSecrets(this IConfigurationBuilder builder)
    {
        var config = builder.Build();
        var address = config.GetSection("Vault:Address").Value!;
        var roleId = config.GetSection("Vault:RoleId").Value!;
        var secretId = config.GetSection("Vault:SecretId").Value!;

        var authMethod = new AppRoleAuthMethodInfo(roleId, secretId);
        var vaultClientSettings = new VaultClientSettings(address, authMethod);
        var client = new VaultClient(vaultClientSettings);

        // Асинхронное получение секретов
        var secret = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync("ChatFlow", mountPoint: "kv");

        var dict = new Dictionary<string, string>();
        FlattenJsonElement(secret.Data.Data, "", dict);

        builder.AddInMemoryCollection(dict);

        return builder;
    }

    private static void FlattenJsonElement(IDictionary<string, object> data, string parentKey,
        IDictionary<string, string> result)
    {
        foreach (var kv in data)
        {
            var key = string.IsNullOrEmpty(parentKey) ? kv.Key : $"{parentKey}:{kv.Key}";

            if (kv.Value is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.Object:
                        var dict = new Dictionary<string, object>();
                        foreach (var prop in jsonElement.EnumerateObject())
                            dict[prop.Name] = prop.Value;
                        FlattenJsonElement(dict, key, result);
                        break;
                    case JsonValueKind.Array:
                        result[key] = jsonElement.ToString();
                        break;
                    default:
                        result[key] = jsonElement.ToString();
                        break;
                }
            }
            else
            {
                result[key] = kv.Value?.ToString();
            }
        }
    }
}