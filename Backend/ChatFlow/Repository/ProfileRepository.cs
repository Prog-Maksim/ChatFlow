using ChatFlow.Models.DB;
using ChatFlow.Models.Requests;
using ChatFlow.Models.Response;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Service;
using Microsoft.EntityFrameworkCore;

namespace ChatFlow.Repository;

public class ProfileRepository: IProfileRepository
{
    private readonly ApplicationContext _context;
    private readonly ILogger<ProfileRepository> _logger;

    public ProfileRepository(ApplicationContext context, ILogger<ProfileRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task CreatePersonAsync(DataPersons person)
    {
        await _context.DataPersons.AddAsync(person);
        _logger.LogDebug("Пользователь успешно создан");
    }
    
    public async Task<SummaryDataPerson?> GetSummaryPersonDataAsync(string personId)
    {
        var person = await _context.DataPersons
            .Include(i => i.Images)
            .FirstOrDefaultAsync(p => p.PersonId == personId);
        
        if (person is not null)
        {
            var image = person.Images.FirstOrDefault(i => i.IsPrimary);
            string? imageUrl = image != null ? S3Service.BaseFileUrl + image.ImageId : null;
            
            DataImage? dataImage = null;
            
            if (imageUrl is not null)
                dataImage = new DataImage
                {
                    ImageId = image!.ImageId,
                    Url = imageUrl
                };
            
            return new SummaryDataPerson
            {
                Name = person.Name,
                Surname = person.Surname,
                Image = dataImage
            };
        }
        
        return null;
    }

    public async Task<Models.Other.DataPerson?> GetPersonDataAsync(string personId)
    {
        var person = await _context.DataPersons
            .Include(i => i.Images)
            .FirstOrDefaultAsync(p => p.PersonId == personId);

        if (person is not null)
        {
            var images = person.Images.ToList();
            
            return new Models.Other.DataPerson
            {
                Name = person.Name,
                Surname = person.Surname,
                Tag = person.Tag,
                Description = person.Description,
                Images = images.Select(i => new DataImage
                {
                    ImageId = i.ImageId,
                    Url = S3Service.BaseFileUrl + i.ImageId
                }).ToList(),
            };
        }
        
        return null;
    }

    public async Task<List<DataImage>?> GetImagesPersonData(string personId)
    {
        var person = await _context.DataPersons
            .Include(i => i.Images)
            .FirstOrDefaultAsync(p => p.PersonId == personId);

        if (person is not null)
        {
            var images = person.Images.ToList();
            
            return images.Select(i => new DataImage
            {
                ImageId = i.ImageId,
                Url = S3Service.BaseFileUrl + i.ImageId
            }).ToList();
        }
        
        return null;
    }

    public async Task<bool> UserExistsAsync(string personId)
    {
        return await _context.Persons.AnyAsync(u => u.PersonId == personId);
    }

    public async Task<int> GetNumImageInByIdAsync(string personId)
    {
        var num = await _context.Images.CountAsync(p => p.PersonId == personId);
        _logger.LogDebug("Кол-во изображений у пользователя: {p} || {c}", personId, num);
        return num;
    }

    public async Task<Images?> GetImageByIdAsync(string personId, string imageId)
    {
        return await _context.Images.FirstOrDefaultAsync(p => p.PersonId == personId && p.ImageId == imageId);
    }

    public async Task<List<Images>> GetAllImagesAsync(string personId)
    {
        return await _context.Images.Where(p => p.PersonId == personId).ToListAsync();
    }

    public void DeleteImage(Images image)
    {
        _context.Images.Remove(image);
    }

    public async Task<string?> GetPrimaryImage(string personId)
    {
        var image = await _context.Images.FirstOrDefaultAsync(p => p.PersonId == personId && p.IsPrimary == true);

        if (image is null)
            return null;

        return image.ImageId;
    }

    public async Task AddImageDataAsync(Images images)
    {
        await _context.Images.AddAsync(images);
    }

    public async Task ClearPrimaryImageAsync(string personId)
    {
        await _context.Images.Where(p => p.PersonId == personId)
            .ExecuteUpdateAsync(s => s.SetProperty(
                p => p.IsPrimary, false));
    }

    public async Task SetPrimaryImageAsync(string personId, string imageId)
    {
        var image = await _context.Images.FirstOrDefaultAsync(p => p.PersonId == personId && p.ImageId == imageId);

        if (image is null)
            throw new FileNotFoundException("Изображение не найдено");
        
        image.IsPrimary = true;
    }

    public async Task SetFirstImageIsPrimaryAsync(string personId)
    {
        var primaryImageId = await _context.Images
            .Where(p => p.PersonId == personId)
            .OrderByDescending(p => p.Created)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();

        if (primaryImageId != 0)
            await _context.Images
                .Where(p => p.PersonId == personId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.IsPrimary, p => p.Id == primaryImageId));
    }
    

    public async Task<DataPersons?> CheckTagAsync(string tag)
    {
        return await _context.DataPersons.FirstOrDefaultAsync(t => t.Tag == tag);
    }

    public async Task UpdateProfileDataAsync(string personId, Profile profile)
    {
        await _context.DataPersons
            .Where(p => p.PersonId == personId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Name, profile.Name)
                .SetProperty(p => p.Surname, profile.Surname)
                .SetProperty(p => p.Tag, '@' + profile.Tag)
                .SetProperty(p => p.Description, profile.Description)
            );
    }


    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
        _logger.LogDebug("Данные сохранены в БД");
    }
}