using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocalServices.Models;
using LocalServices.Data;

[Route("api/[controller]")]
[ApiController]
public class FacilitiesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public FacilitiesApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Facility
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Facility>>> GetFacility()
    {
        return await _context.Facilities.ToListAsync();
    }

    // GET: api/Facility/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Facility>> GetFacility(int id)
    {
        var facility = await _context.Facilities.FindAsync(id);

        if (facility == null)
        {
            return NotFound();
        }

        return facility;
    }

    // PUT: api/Facility/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutFacility(int? id, Facility facility)
    {
        if (id != facility.Id)
        {
            return BadRequest();
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

    // POST: api/Facility
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Facility>> PostFacility(Facility facility)
    {
        _context.Facilities.Add(facility);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetFacility", new { id = facility.Id }, facility);
    }

    // DELETE: api/Facility/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFacility(int? id)
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

    private bool FacilityExists(int? id)
    {
        return _context.Facilities.Any(e => e.Id == id);
    }
}
