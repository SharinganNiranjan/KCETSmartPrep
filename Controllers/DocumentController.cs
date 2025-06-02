using KcetPrep1.Data;
using KcetPrep1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KcetPrep1.Controllers
{
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, string documentName)
        {
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "documents");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var document = new DocumentTemplate
                {
                    DocumentName = documentName,
                    FileName = file.FileName,
                    FilePath = "/documents/" + fileName
                };

                _context.DocumentTemplates.Add(document);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Document uploaded successfully!";
                return RedirectToAction("List");
            }

            TempData["Error"] = "Please select a file.";
            return View();
        }

        public IActionResult List()
        {
            var docs = _context.DocumentTemplates.OrderByDescending(d => d.UploadedAt).ToList();
            return View(docs);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.DocumentTemplates.FindAsync(id);
            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction("List");
            }

            // Delete physical file
            var physicalPath = Path.Combine(_env.WebRootPath, "documents", Path.GetFileName(document.FilePath));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            _context.DocumentTemplates.Remove(document);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Document deleted successfully.";
            return RedirectToAction("List");
        }
    }

}
