using ChatFlow.Models.Response;

namespace ChatFlow.Repository.Interfaces;

public interface IOtherPersonDataRepository
{
    /// <summary>
    /// Создает структуру
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task InitializePersonData(string personId);

    /// <summary>
    /// Добавляет публичный ключ
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="publicKey">Публичный ключ</param>
    /// <param name="deviceId">Идентификатор устройства</param>
    /// <returns></returns>
    public Task AddPublicKey(string personId, string publicKey, string deviceId);

    /// <summary>
    /// Обновляет статус публичных ключей пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="deviceId">Идентификатор устройства</param>
    /// <param name="newStatus">Новый статус</param>
    /// <returns></returns>
    public Task UpdatePublicKeyStatus(string personId, string deviceId, bool newStatus);

    /// <summary>
    /// Возвращает публичные ключи пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<List<PublicKeyResponse>> GetActivePublicKeys(string personId);
}