using Microsoft.EntityFrameworkCore;
using ProfileService.Models.DB;
using ProfileService.Models.Events;
using ProfileService.Repository.Interfaces;

namespace ProfileService.Repository;

public class ProfileRepository: IProfileRepository
{
    private readonly ApplicationContext _context;
    private readonly ILogger<ProfileRepository> _logger;

    public ProfileRepository(ApplicationContext context, ILogger<ProfileRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task CreatePersonAsync(Persons person)
    {
        await _context.Persons.AddAsync(person);
        _logger.LogDebug("Пользователь успешно создан");
    }

    public async Task CreatePersonAsync(UserCreated person)
    {
        Persons personCreated = new Persons
        {
            PersonId = person.PersonId,
            Name = person.Name,
            Surname = person.Surname
        };
        
        await _context.Persons.AddAsync(personCreated);
        _logger.LogDebug("Пользователь успешно создан");
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

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
        _logger.LogDebug("Данные сохранены в БД");
    }
}