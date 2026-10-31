using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocalServices.Models;
using LocalServices.Data;
using LocalServices.Services;

[Route("api/[controller]")]
[Route("api/services")]
[ApiController]
public class ServicesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITelegramNotificationService? _telegramService;

    public ServicesApiController(ApplicationDbContext context, ITelegramNotificationService? telegramService = null)
    {
        _context = context;
        _telegramService = telegramService;
    }

    // GET: api/services?skip=0&limit=10
    [HttpGet]
    public async Task<ActionResult<PagedResult<Service>>> GetServices([FromQuery] int skip = 0, [FromQuery] int limit = 10)
    {
        if (skip < 0) skip = 0;
        if (limit < 1) limit = 10;
        if (limit > 100) limit = 100;

        var query = _context.Services
            .Include(s => s.Facility)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.Id)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        var path = Request?.Path.Value ?? "/api/services";
        string? nextLink = (skip + limit < totalCount) ? $"{path}?skip={skip + limit}&limit={limit}" : null;
        string? prevLink = (skip > 0) ? $"{path}?skip={Math.Max(0, skip - limit)}&limit={limit}" : null;

        var result = new PagedResult<Service>
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

    // GET: api/Service/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Service>> GetService(int id)
    {
        var service = await _context.Services
            .Include(s => s.Facility)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
        {
            return NotFound();
        }

        return service;
    }

    // PUT: api/Service/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutService(int id, [FromBody] Service service)
    {
        if (id != service.Id)
        {
            return BadRequest("ID у URL не відповідає ID об'єкта.");
        }

        _context.Entry(service).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceExists(id))
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

    // POST: api/Service
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Service>> PostService([FromBody] Service service)
    {
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        if (_telegramService != null)
        {
            var fac = await _context.Facilities.FindAsync(service.FacilityId);
            await _telegramService.SendServiceCreatedAsync(service, fac?.Name);
        }

        return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
    }

    // DELETE: api/Service/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ServiceExists(int id)
    {
        return _context.Services.Any(e => e.Id == id);
    }
}