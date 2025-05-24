using ProfileService.Models.DB;
using ProfileService.Models.Events;

namespace ProfileService.Repository.Interfaces;

public interface IProfileRepository
{
    /// <summary>
    /// Создает пользователя
    /// </summary>
    /// <param name="person"></param>
    /// <returns></returns>
    public Task CreatePersonAsync(Persons person);
    
    /// <summary>
    /// Создает пользователя
    /// </summary>
    /// <param name="person"></param>
    /// <returns></returns>
    public Task CreatePersonAsync(UserCreated person);

    /// <summary>
    /// Проверяет существует ли пользователь в БД
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns>True - если пользователь существует</returns>
    public Task<bool> UserExistsAsync(string personId);

    /// <summary>
    /// Выдает кол-во изображений профиля в БД у пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<int> GetNumImageInByIdAsync(string personId);

    /// <summary>
    /// Возвращает объект изображения
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="imageId">Идентификатор изображения</param>
    /// <returns></returns>
    public Task<Images?> GetImageByIdAsync(string personId, string imageId);

    /// <summary>
    /// Возвращает все изображения пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<List<Images>> GetAllImagesAsync(string personId);

    /// <summary>
    /// Удаляет изображение
    /// </summary>
    /// <param name="image">Объект изображения</param>
    /// <returns></returns>
    public void DeleteImage(Images image);
    
    
    /// <summary>
    /// Выдает главное изображение пользователя
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <returns></returns>
    public Task<string?> GetPrimaryImage(string personId);
    
    /// <summary>
    /// Добавляет изображение профиля в БД
    /// </summary>
    /// <param name="images">Объект изображения</param>
    /// <returns></returns>
    public Task AddImageDataAsync(Images images);
    
    /// <summary>
    /// Отмена главных изображений для пользователя
    /// </summary>
    /// <param name="personId"></param>
    /// <returns></returns>
    public Task ClearPrimaryImageAsync(string personId);
    
    /// <summary>
    /// Делает изображение основным
    /// </summary>
    /// <param name="personId">Идентификатор пользователя</param>
    /// <param name="imageId">Идентификатор изображение</param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">Изображение не найдено</exception>
    public Task SetPrimaryImageAsync(string personId, string imageId);
    
    /// <summary>
    /// Делает первое изображение основным
    /// </summary>
    /// <param name="personId"></param>
    /// <returns></returns>
    public Task SetFirstImageIsPrimaryAsync(string personId);
    
    /// <summary>
    /// Сохраняет изменения в БД
    /// </summary>
    /// <returns></returns>
    public Task SaveChangesAsync();
}