using KcetPrep1.Data;
using KcetPrep1.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace KcetPrep1.Controllers
{
    [Route("Question")]
    public class QuestionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuestionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Index")]
        public IActionResult Index(string subject = null)
        {
            var questions = string.IsNullOrEmpty(subject)
                ? _context.Questions.ToList()
                : _context.Questions.Where(q => q.Subject == subject).ToList();
            ViewBag.SelectedSubject = subject;
            return View(questions);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Question question)
        {
            if (ModelState.IsValid)
            {
                _context.Questions.Add(question);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(question);
        }

        // EDIT - GET
        [HttpGet("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var question = _context.Questions.FirstOrDefault(q => q.Id == id);
            if (question == null)
            {
                return NotFound();
            }
            return View(question);
        }

        // EDIT - POST
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Question updatedQuestion)
        {
            if (id != updatedQuestion.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var question = _context.Questions.FirstOrDefault(q => q.Id == id);
                if (question == null)
                {
                    return NotFound();
                }

                // Update fields
                question.Subject = updatedQuestion.Subject;
                question.QuestionText = updatedQuestion.QuestionText;
                question.OptionA = updatedQuestion.OptionA;
                question.OptionB = updatedQuestion.OptionB;
                question.OptionC = updatedQuestion.OptionC;
                question.OptionD = updatedQuestion.OptionD;
                question.CorrectAnswer = updatedQuestion.CorrectAnswer;

                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(updatedQuestion);
        }

        // DELETE - GET (Confirm)
        [HttpGet("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var question = _context.Questions.FirstOrDefault(q => q.Id == id);
            if (question == null)
            {
                return NotFound();
            }
            return View(question);
        }

        // DELETE - POST (Confirmed)
        [HttpPost("Delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var question = _context.Questions.FirstOrDefault(q => q.Id == id);
            if (question == null)
            {
                return NotFound();
            }

            _context.Questions.Remove(question);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}
