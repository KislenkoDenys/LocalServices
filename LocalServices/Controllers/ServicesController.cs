using LocalServices.Data;
using LocalServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class ServicesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ServicesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: SERVICES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Services
            .Include(s => s.Facility)
            .ToListAsync());
    }

    // GET: SERVICES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var service = await _context.Services
            .Include(s => s.Facility)
                .ThenInclude(f => f!.Category)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (service == null)
        {
            return NotFound();
        }

        return View(service);
    }

    // GET: SERVICES/Create
    public IActionResult Create()
    {
        ViewData["FacilityList"] = new SelectList(_context.Facilities, "Id", "Name");
        return View();
    }

    // POST: SERVICES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Price,FacilityId")] Service service)
    {
        if (ModelState.IsValid)
        {
            _context.Add(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["FacilityList"] = new SelectList(_context.Facilities, "Id", "Name", service.FacilityId);
        return View(service);
    }

    // GET: SERVICES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }
        ViewData["FacilityList"] = new SelectList(_context.Facilities, "Id", "Name", service.FacilityId);
        return View(service);
    }

    // POST: SERVICES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,Price,FacilityId")] Service service)
    {
        if (id != service.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(service);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceExists(service.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["FacilityList"] = new SelectList(_context.Facilities, "Id", "Name", service.FacilityId);
        return View(service);
    }

    // GET: SERVICES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var service = await _context.Services
            .Include(s => s.Facility)
                .ThenInclude(f => f!.Category)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (service == null)
        {
            return NotFound();
        }

        return View(service);
    }

    // POST: SERVICES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service != null)
        {
            _context.Services.Remove(service);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ServiceExists(int? id)
    {
        return _context.Services.Any(e => e.Id == id);
    }
}
