using KcetPrep1.Data;
using KcetPrep1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class CutoffController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public CutoffController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file, int year)
    {
        if (file != null && file.Length > 0 && Path.GetExtension(file.FileName) == ".pdf")
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var cutoff = new CutoffFile
            {
                FileName = file.FileName,
                FilePath = "/uploads/" + fileName,
                Year = year
            };

            _context.CutoffFiles.Add(cutoff);
            await _context.SaveChangesAsync();

            TempData["Success"] = "PDF uploaded successfully!";
            return RedirectToAction("List");
        }

        TempData["Error"] = "Please upload a valid PDF file.";
        return View();
    }

    public IActionResult List(int? year)
    {
        var files = _context.CutoffFiles.AsQueryable();

        if (year.HasValue)
            files = files.Where(f => f.Year == year.Value);

        return View(files.OrderByDescending(f => f.UploadedAt).ToList());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var file = await _context.CutoffFiles.FindAsync(id);
        if (file == null)
        {
            TempData["Error"] = "File not found.";
            return RedirectToAction("List");
        }

        var physicalPath = Path.Combine(_env.WebRootPath, "uploads", Path.GetFileName(file.FilePath));
        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }

        _context.CutoffFiles.Remove(file);
        await _context.SaveChangesAsync();

        TempData["Success"] = "PDF deleted successfully.";
        return RedirectToAction("List");
    }
}
