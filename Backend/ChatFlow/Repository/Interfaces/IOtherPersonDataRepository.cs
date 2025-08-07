namespace ChatFlow.Repository.Interfaces;

public interface IOtherPersonDataRepository
{
    public Task InitializePersonData(string personId);

    public Task AddPublicKey(string personId, string publicKey, string deviceId);

    public Task UpdatePublicKeyStatus(string personId, string deviceId, bool newStatus);
}