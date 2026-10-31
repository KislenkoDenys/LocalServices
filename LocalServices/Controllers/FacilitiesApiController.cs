using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocalServices.Models;
using LocalServices.Data;

[Route("api/[controller]")]
[Route("api/facilities")]
[ApiController]
public class FacilitiesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FacilitiesApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/facilities?skip=0&limit=10 (B1: Pagination API)
    [HttpGet]
    public async Task<ActionResult<PagedResult<Facility>>> GetFacilities([FromQuery] int skip = 0, [FromQuery] int limit = 10)
    {
        if (skip < 0) skip = 0;
        if (limit < 1) limit = 10;
        if (limit > 100) limit = 100;

        var query = _context.Facilities
            .Include(f => f.Category)
            .Include(f => f.Services)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(f => f.Id)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

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
            .FirstOrDefaultAsync(f => f.Id == id);

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

        // Пошук виконується за умови введення щонайменше 3 символів
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

        return CreatedAtAction(nameof(GetFacility), new { id = facility.Id }, facility);
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

        return NoContent();
    }

    private bool FacilityExists(int id)
    {
        return _context.Facilities.Any(e => e.Id == id);
    }
}