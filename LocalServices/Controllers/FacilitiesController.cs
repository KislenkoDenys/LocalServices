using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LocalServices.Models;
using LocalServices.Data;
using LocalServices.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;

public class FacilitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly IMemoryCache? _cache;

    public FacilitiesController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, IMemoryCache? cache = null)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
        _cache = cache;
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
            var category = _context.Categories.Find(categoryId.Value);
            if (category != null)
            {
                ViewBag.SelectedCategoryName = category.Name;
            }
        }
        return View(facility);
    }

    // GET: Facilities/Map
    public async Task<IActionResult> Map()
    {
        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        return View();
    }

    // POST: FACILITYS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,City,Street,Latitude,Longitude,Description,Phone,WorkingHours,ImagePath,ImageFile,CategoryId")] Facility facility)
    {
        if (ModelState.IsValid)
        {
            if (facility.ImageFile != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await facility.ImageFile.CopyToAsync(memoryStream);
                    var bytes = memoryStream.ToArray();
                    facility.ImagePath = $"data:{facility.ImageFile.ContentType};base64,{Convert.ToBase64String(bytes)}";
                }
            }
            _context.Add(facility);
            await _context.SaveChangesAsync();
            _cache?.Remove(CacheConstants.FacilitiesAllKey);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[CACHE INVALIDATED] Кеш закладів очищено через веб-форму створення.");
            Console.ResetColor();
            return RedirectToAction(nameof(Index));
        }
        if (facility.CategoryId > 0)
        {
            var category = await _context.Categories.FindAsync(facility.CategoryId);
            if (category != null)
            {
                ViewBag.SelectedCategoryName = category.Name;
            }
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
        if (facility.CategoryId > 0)
        {
            var category = await _context.Categories.FindAsync(facility.CategoryId);
            if (category != null)
            {
                ViewBag.SelectedCategoryName = category.Name;
            }
        }
        ViewData["CategoryList"] = new SelectList(_context.Categories, "Id", "Name", facility.CategoryId);
        return View(facility);
    }

    // POST: FACILITYS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,City,Street,Latitude,Longitude,Description,Phone,WorkingHours,ImagePath,ImageFile,CategoryId")] Facility facility)
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
                    using (var memoryStream = new MemoryStream())
                    {
                        await facility.ImageFile.CopyToAsync(memoryStream);
                        var bytes = memoryStream.ToArray();
                        facility.ImagePath = $"data:{facility.ImageFile.ContentType};base64,{Convert.ToBase64String(bytes)}";
                    }
                }

                _context.Update(facility);
                await _context.SaveChangesAsync();
                _cache?.Remove(CacheConstants.FacilitiesAllKey);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[CACHE INVALIDATED] Кеш закладів очищено через веб-форму редагування ID={facility.Id}.");
                Console.ResetColor();
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
        if (facility.CategoryId > 0)
        {
            var category = await _context.Categories.FindAsync(facility.CategoryId);
            if (category != null)
            {
                ViewBag.SelectedCategoryName = category.Name;
            }
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
        _cache?.Remove(CacheConstants.FacilitiesAllKey);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[CACHE INVALIDATED] Кеш закладів очищено через видалення у веб-формі ID={id}.");
        Console.ResetColor();
        return RedirectToAction(nameof(Index));
    }

    private bool FacilityExists(int? id)
    {
        return _context.Facilities.Any(e => e.Id == id);
    }
}

