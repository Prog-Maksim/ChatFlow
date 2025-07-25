using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace ChatFlow.Service;

public class S3Service
{
    private readonly AmazonS3Client _s3Client;
    private const string BucketName = "chatflow-avatar";
    public const string BaseFileUrl = "https://storage.yandexcloud.net/chatflow-avatar/";
    
    private readonly IConfiguration _configuration;

    public S3Service(IConfiguration configuration)
    {
        _configuration = configuration;
        
        var accessKey = _configuration["S3:ACCESS_KEY"];
        var secretKey = _configuration["S3:SECRET_KEY"];
        
        var endpoint = new Uri("https://storage.yandexcloud.net"); 
        
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint.ToString(),
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(credentials, config);
    }
    
    /// <summary>
    /// Позволяет удалить файл из S3 хранилищя
    /// </summary>
    /// <param name="key">ключ файла</param>
    /// <exception cref="FileNotFoundException">Файл не найден</exception>
    /// <exception cref="FileLoadException">Ошибка при удалении файла</exception>
    public async Task DeleteFileFromS3Async(string key)
    {
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = BucketName,
            Key = key
        };

        var response = await _s3Client.DeleteObjectAsync(deleteRequest);
            
        if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
            throw new FileNotFoundException("Файл не был найден");

        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            throw new FileLoadException("Произошла ошибка при удаление файла");
    }
    
    /// <summary>
    /// Позволяет загрузить файл в S3 хранилище
    /// </summary>
    /// <param name="file">загружаемый файл</param>
    /// <returns></returns>
    /// <exception cref="FileLoadException">Ошибка загрузки файла</exception>
    public async Task<string> UploadFileToS3Async(IFormFile file)
    {
        string key = Guid.NewGuid().ToString();
        
        using (var stream = file.OpenReadStream())
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                InputStream = stream
            };

            var response = await _s3Client.PutObjectAsync(putRequest);
            
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                return key;
        }

        throw new FileLoadException("Произошла ошибка при загрузке изображения");
    }
}