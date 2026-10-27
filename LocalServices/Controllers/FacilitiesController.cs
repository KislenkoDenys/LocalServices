using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocalServices.Models;
using LocalServices.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

public class FacilitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;

    public FacilitiesController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    // GET: FACILITYS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Facilities
            .Include(f => f.Category)
            .Include(f => f.Services)
            .ToListAsync());
    }

    // GET: FACILITYS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var facility = await _context.Facilities
            .Include(f => f.Category)
            .Include(f => f.Services)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (facility == null)
        {
            return NotFound();
        }

        return View(facility);
    }

    // GET: FACILITYS/Create
    public IActionResult Create(int? categoryId)
    {
        ViewData["CategoryList"] = new SelectList(_context.Categories, "Id", "Name", categoryId);
        var facility = new Facility();
        if (categoryId.HasValue)
        {
            facility.CategoryId = categoryId.Value;
        }
        return View(facility);
    }

    // POST: FACILITYS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,City,Street,ImagePath,ImageFile,CategoryId")] Facility facility)
    {
        if (ModelState.IsValid)
        {
            if (facility.ImageFile != null)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(facility.ImageFile.FileName);
                string path = Path.Combine(wwwRootPath + "/images/", fileName);
                Directory.CreateDirectory(Path.Combine(wwwRootPath, "images"));
                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    await facility.ImageFile.CopyToAsync(fileStream);
                }
                facility.ImagePath = "/images/" + fileName;
            }
            _context.Add(facility);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["CategoryList"] = new SelectList(_context.Categories, "Id", "Name", facility.CategoryId);
        return View(facility);
    }

    // GET: FACILITYS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var facility = await _context.Facilities
            .Include(f => f.Category)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (facility == null)
        {
            return NotFound();
        }
        ViewData["CategoryList"] = new SelectList(_context.Categories, "Id", "Name", facility.CategoryId);
        return View(facility);
    }

    // POST: FACILITYS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,City,Street,ImagePath,ImageFile,CategoryId")] Facility facility)
    {
        if (id != facility.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (facility.ImageFile != null)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(facility.ImageFile.FileName);
                    string path = Path.Combine(wwwRootPath + "/images/", fileName);
                    Directory.CreateDirectory(Path.Combine(wwwRootPath, "images"));
                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await facility.ImageFile.CopyToAsync(fileStream);
                    }
                    facility.ImagePath = "/images/" + fileName;
                }

                _context.Update(facility);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FacilityExists(facility.Id))
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
        ViewData["CategoryList"] = new SelectList(_context.Categories, "Id", "Name", facility.CategoryId);
        return View(facility);
    }

    // GET: FACILITYS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var facility = await _context.Facilities
            .Include(f => f.Category)
            .Include(f => f.Services)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (facility == null)
        {
            return NotFound();
        }

        return View(facility);
    }

    // POST: FACILITYS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var facility = await _context.Facilities.FindAsync(id);
        if (facility != null)
        {
            _context.Facilities.Remove(facility);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool FacilityExists(int? id)
    {
        return _context.Facilities.Any(e => e.Id == id);
    }
}

