namespace ChatFlow.Service.Interfaces;

public interface IS3Service
{
    /// <summary>
    /// Позволяет удалить файл из S3 хранилищя
    /// </summary>
    /// <param name="key">Ключ файла</param>
    /// <exception cref="FileNotFoundException">Файл не найден</exception>
    /// <exception cref="FileLoadException">Ошибка при удалении файла</exception>
    public Task DeleteFileAsync(string key);

    /// <summary>
    /// Позволяет загрузить файл в S3 хранилище
    /// </summary>
    /// <param name="file">Загружаемый файл</param>
    /// <returns></returns>
    /// <exception cref="FileLoadException">Ошибка загрузки файла</exception>
    public Task<string> UploadFileAsync(IFormFile file);
}