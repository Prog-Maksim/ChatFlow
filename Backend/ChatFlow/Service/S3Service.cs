using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ChatFlow.Service.Interfaces;

namespace ChatFlow.Service;

public class S3Service: IS3Service
{
    private readonly AmazonS3Client _s3Client;
    private const string BucketName = "chatflow-avatar";
    public const string BaseFileUrl = "https://storage.yandexcloud.net/chatflow-avatar/";

    public S3Service(IConfiguration configuration)
    {
        var accessKey = configuration["S3:ACCESS_KEY"];
        var secretKey = configuration["S3:SECRET_KEY"];
        
        var endpoint = new Uri("https://storage.yandexcloud.net"); 
        
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint.ToString(),
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(credentials, config);
    }
    
    public async Task DeleteFileAsync(string key)
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
    
    public async Task<string> UploadFileAsync(IFormFile file)
    {
        string key = Guid.NewGuid().ToString();

        await using var stream = file.OpenReadStream();
        var putRequest = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = stream
        };

        var response = await _s3Client.PutObjectAsync(putRequest);
            
        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            return key;

        throw new FileLoadException("Произошла ошибка при загрузке изображения");
    }
}