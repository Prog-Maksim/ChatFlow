using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatFlow.Models.DB;

public class OtherPersonData
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    public required string PersonId { get; set; }
    public required Dictionary<string, DeviceKeyData> PublicKeys { get; set; }
}

public class DeviceKeyData
{
    /// <summary>
    /// Публичный ключ
    /// </summary>
    public required string PublicKey { get; set; }

    /// <summary>
    /// Статус ключа (false в случае блокировки сессии)
    /// </summary>
    public bool Status { get; set; } = true;
}