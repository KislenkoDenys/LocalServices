using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LocalServices.Models;
using LocalServices.Data;
using LocalServices.Infrastructure;
using LocalServices.Services;

[Route("api/[controller]")]
[Route("api/facilities")]
[ApiController]
public class FacilitiesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FacilitiesApiController>? _logger;
    private readonly ITelegramNotificationService? _telegramService;
    private readonly IBlobStorageService? _blobStorageService;

    public FacilitiesApiController(
        ApplicationDbContext context,
        IMemoryCache? cache = null,
        ILogger<FacilitiesApiController>? logger = null,
        ITelegramNotificationService? telegramService = null,
        IBlobStorageService? blobStorageService = null)
    {
        _context = context;
        _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        _logger = logger;
        _telegramService = telegramService;
        _blobStorageService = blobStorageService;
    }

    // GET: api/facilities?skip=0&limit=10
    [HttpGet]
    public async Task<ActionResult<PagedResult<Facility>>> GetFacilities([FromQuery] int skip = 0, [FromQuery] int limit = 10)
    {
        if (skip < 0) skip = 0;
        if (limit < 1) limit = 10;
        if (limit > 100) limit = 100;

        if (!_cache.TryGetValue(CacheConstants.FacilitiesAllKey, out List<Facility>? allFacilities) || allFacilities == null)
        {
            allFacilities = await _context.Facilities
                .Include(f => f.Category)
                .Include(f => f.Services)
                .OrderBy(f => f.Id)
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheConstants.DefaultCacheDuration)
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));

            _cache.Set(CacheConstants.FacilitiesAllKey, allFacilities, cacheOptions);

            Response?.Headers.TryAdd("X-Cache", "MISS");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[CACHE MISS] Заклади ({allFacilities.Count} шт.) завантажено з бази даних та збережено в IMemoryCache.");
            Console.ResetColor();
            _logger?.LogInformation("[CACHE MISS] Заклади завантажено з бази даних.");
        }
        else
        {
            Response?.Headers.TryAdd("X-Cache", "HIT");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[CACHE HIT] Заклади ({allFacilities.Count} шт.) повернуто з IMemoryCache (запитів до БД: 0).");
            Console.ResetColor();
            _logger?.LogInformation("[CACHE HIT] Заклади отримано з кешу.");
        }

        var totalCount = allFacilities.Count;
        var items = allFacilities
            .Skip(skip)
            .Take(limit)
            .ToList();

        var path = Request?.Path.Value ?? "/api/facilities";
        string? nextLink = (skip + limit < totalCount) ? $"{path}?skip={skip + limit}&limit={limit}" : null;
        string? prevLink = (skip > 0) ? $"{path}?skip={Math.Max(0, skip - limit)}&limit={limit}" : null;

        var result = new PagedResult<Facility>
        {
            TotalCount = totalCount,
            Skip = skip,
            Limit = limit,
            TotalPages = (int)Math.Ceiling((double)totalCount / limit),
            HasNextPage = nextLink != null,
            HasPreviousPage = prevLink != null,
            NextLink = nextLink,
            PreviousLink = prevLink,
            Items = items
        };

        return Ok(result);
    }

    // GET: api/Facility/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Facility>> GetFacility(int id)
    {
        var facility = await _context.Facilities
            .Include(f => f.Category)
            .Include(f => f.Services)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (facility == null)
        {
            return NotFound();
        }

        return facility;
    }

    // PUT: api/Facility/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutFacility(int id, [FromBody] Facility facility)
    {
        if (id != facility.Id)
        {
            return BadRequest("ID у URL не відповідає ID об'єкта.");
        }

        _context.Entry(facility).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();

            InvalidateCache($"оновлення закладу ID={id}");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!FacilityExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // GET: api/facilities/search?term=мак&page=1&pageSize=10
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? term, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 3)
        {
            return Ok(new
            {
                results = Array.Empty<object>(),
                pagination = new { more = false }
            });
        }

        var trimmed = term.Trim().ToLower();
        var query = _context.Facilities
            .Where(f => f.Name.ToLower().Contains(trimmed) || (f.City != null && f.City.ToLower().Contains(trimmed)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                id = f.Id,
                text = string.IsNullOrWhiteSpace(f.City) ? f.Name : $"{f.Name} ({f.City})"
            })
            .ToListAsync();

        return Ok(new
        {
            results = items,
            pagination = new
            {
                more = (page * pageSize) < totalCount
            }
        });
    }

    // POST: api/Facility
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Facility>> PostFacility([FromBody] Facility facility)
    {
        _context.Facilities.Add(facility);
        await _context.SaveChangesAsync();

        InvalidateCache($"створення закладу «{facility.Name}»");

        if (_telegramService != null)
        {
            var cat = await _context.Categories.FindAsync(facility.CategoryId);
            await _telegramService.SendFacilityCreatedAsync(facility, cat?.Name);
        }

        return CreatedAtAction(nameof(GetFacility), new { id = facility.Id }, facility);
    }

    // POST: api/facilities/upload-image
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Файл не надано.");
        }

        if (_blobStorageService == null)
        {
            return StatusCode(500, "Сервіс збереження файлів не налаштовано.");
        }

        var url = await _blobStorageService.UploadImageAsync(file);
        if (string.IsNullOrWhiteSpace(url))
        {
            return StatusCode(500, "Не вдалося завантажити зображення в Blob Storage.");
        }

        return Ok(new { url });
    }

    // DELETE: api/Facility/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFacility(int id)
    {
        var facility = await _context.Facilities.FindAsync(id);
        if (facility == null)
        {
            return NotFound();
        }

        _context.Facilities.Remove(facility);
        await _context.SaveChangesAsync();

        InvalidateCache($"видалення закладу ID={id}");

        return NoContent();
    }

    private void InvalidateCache(string reason)
    {
        _cache.Remove(CacheConstants.FacilitiesAllKey);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[CACHE INVALIDATED] Кеш закладів очищено через {reason}.");
        Console.ResetColor();
        _logger?.LogInformation("[CACHE INVALIDATED] Кеш закладів очищено через {Reason}.", reason);
    }

    private bool FacilityExists(int id)
    {
        return _context.Facilities.Any(e => e.Id == id);
    }
}
