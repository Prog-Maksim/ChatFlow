using ChatFlow.Models.DB;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using MongoDB.Driver;

namespace ChatFlow.Repository;

public class OtherPersonDataRepository: IOtherPersonDataRepository
{
    private readonly ILogger<OtherPersonDataRepository> _logger;
    private readonly IMongoCollection<OtherPersonData> _userCollections;

    public OtherPersonDataRepository(IMongoClient client, ILogger<OtherPersonDataRepository> logger)
    {
        _logger = logger;
        
        var emojiDatabase = client.GetDatabase("PersonData");
        _userCollections = emojiDatabase.GetCollection<OtherPersonData>("Data");
    }

    public async Task InitializePersonData(string personId)
    {
        try
        {
            var existingPerson = await _userCollections.Find(x => x.PersonId == personId).FirstOrDefaultAsync();

            if (existingPerson != null)
            {
                _logger.LogInformation($"Person data for ID '{personId}' already exists. Skipping initialization.");
            }
            
            OtherPersonData personData = new OtherPersonData { PersonId = personId, PublicKeys = new()};

            await _userCollections.InsertOneAsync(personData);
            _logger.LogInformation($"New person data for ID '{personId}' initialized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while initializing person data for ID '{personId}'.");
            throw;
        }
    }

    public async Task AddPublicKey(string personId, string publicKey, string deviceId)
    {
        var filter = Builders<OtherPersonData>.Filter.Eq(e => e.PersonId, personId);

        var update = Builders<OtherPersonData>.Update
            .Set($"PublicKeys.{deviceId}", new DeviceKeyData
            {
                PublicKey = publicKey,
                Status = true
            })
            .SetOnInsert(e => e.PersonId, personId);

        await _userCollections.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = true }
        );
    }
    
    public async Task UpdatePublicKeyStatus(string personId, string deviceId, bool newStatus)
    {
        var filter = Builders<OtherPersonData>.Filter.Eq(e => e.PersonId, personId);

        var update = Builders<OtherPersonData>.Update
            .Set($"PublicKeys.{deviceId}.Status", newStatus);

        var result = await _userCollections.UpdateOneAsync(filter, update);

        if (result.ModifiedCount == 0)
        {
            _logger.LogWarning($"No public key found for person '{personId}' and device '{deviceId}'.");
        }
    }
    
    public async Task<List<PublicKeyResponse>> GetActivePublicKeys(string personId)
    {
        var filter = Builders<OtherPersonData>.Filter.Eq(e => e.PersonId, personId);
        var personData = await _userCollections.Find(filter).FirstOrDefaultAsync();

        if (personData == null || personData.PublicKeys == null)
        {
            _logger.LogWarning($"No public keys found for person ID '{personId}'.");
            return new List<PublicKeyResponse>();
        }

        var activeKeys = personData.PublicKeys
            .Where(kvp => kvp.Value.Status)
            .Select(kvp => new PublicKeyResponse
            {
                DeviceId = kvp.Key,
                PublicKey = kvp.Value.PublicKey
            })
            .ToList();

        return activeKeys;
    }
}