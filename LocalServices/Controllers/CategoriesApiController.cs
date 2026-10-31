using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocalServices.Models;
using LocalServices.Data;

[Route("api/[controller]")]
[Route("api/categories")]
[ApiController]
public class CategoriesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/categories?skip=0&limit=10 (B1: Pagination API)
    [HttpGet]
    public async Task<ActionResult<PagedResult<Category>>> GetCategories([FromQuery] int skip = 0, [FromQuery] int limit = 10)
    {
        if (skip < 0) skip = 0;
        if (limit < 1) limit = 10;
        if (limit > 100) limit = 100;

        var query = _context.Categories
            .Include(c => c.Facilities)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Id)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        var path = Request?.Path.Value ?? "/api/categories";
        string? nextLink = (skip + limit < totalCount) ? $"{path}?skip={skip + limit}&limit={limit}" : null;
        string? prevLink = (skip > 0) ? $"{path}?skip={Math.Max(0, skip - limit)}&limit={limit}" : null;

        var result = new PagedResult<Category>
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

    // GET: api/Category/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Facilities)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        return category;
    }

    // PUT: api/Category/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCategory(int id, [FromBody] Category category)
    {
        if (id != category.Id)
        {
            return BadRequest("ID у URL не відповідає ID об'єкта.");
        }

        _context.Entry(category).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CategoryExists(id))
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

    // GET: api/categories/search?term=кав&page=1&pageSize=10
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
        var query = _context.Categories
            .Where(c => c.Name.ToLower().Contains(trimmed));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                id = c.Id,
                text = c.Name
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

    // POST: api/Category
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Category>> PostCategory([FromBody] Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    // DELETE: api/Category/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool CategoryExists(int id)
    {
        return _context.Categories.Any(e => e.Id == id);
    }
}