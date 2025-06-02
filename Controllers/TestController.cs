using KcetPrep1.Data;
using KcetPrep1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KcetPrep1.Controllers
{
    [Route("Test")]
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("SelectSubject")]
        public IActionResult SelectSubject()
        {
            var subjects = new[] { "Physics", "Chemistry", "Mathematics", "Biology" };
            return View(subjects);
        }

        [HttpGet("ListTests")]
        public IActionResult ListTests(string subject = null)
        {
            var tests = string.IsNullOrEmpty(subject)
                ? _context.Tests.ToList()
                : _context.Tests.Where(t => t.Subject == subject).ToList();
            ViewBag.Subject = subject ?? "All Subjects";
            return View(tests);
        }

        [HttpGet("CreateTest")]
        public IActionResult CreateTest(string subject = null)
        {
            var viewModel = new CreateTestViewModel
            {
                Subject = subject
            };
            return View(viewModel);
        }

        [HttpPost("CreateTest")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTest(CreateTestViewModel viewModel)
        {
            var validSubjects = new[] { "Physics", "Chemistry", "Mathematics", "Biology" };
            if (!validSubjects.Contains(viewModel.Subject))
            {
                ModelState.AddModelError("Subject", "Please select a valid subject.");
            }

            if (ModelState.IsValid)
            {
                if (!_context.Questions.Any(q => q.Subject == viewModel.Subject))
                {
                    TempData["Subject"] = viewModel.Subject;
                    TempData["Error"] = $"No questions available for {viewModel.Subject}. Please add questions first.";
                    return RedirectToAction("Create", "Question");
                }

                var test = new Test { Subject = viewModel.Subject, TestName = viewModel.TestName };
                _context.Tests.Add(test);
                _context.SaveChanges();

                var availableQuestions = _context.Questions.Where(q => q.Subject == viewModel.Subject).ToList();
                var selectedQuestions = availableQuestions.OrderBy(q => Guid.NewGuid()).Take(Math.Min(60, availableQuestions.Count)).ToList();
                foreach (var question in selectedQuestions)
                {
                    _context.TestQuestions.Add(new TestQuestion { TestId = test.Id, QuestionId = question.Id });
                }
                _context.SaveChanges();

                return RedirectToAction("ListTests", new { subject = viewModel.Subject });
            }

            return View(viewModel);
        }

        [HttpGet("StartTest")]
        public async Task<IActionResult> StartTest(int testId)
        {
            var test = await _context.Tests.FindAsync(testId);
            if (test == null)
            {
                TempData["Error"] = "Test not found.";
                return RedirectToAction(nameof(SelectSubject));
            }

            var questions = await _context.TestQuestions
                .Include(tq => tq.Question)
                .Where(tq => tq.TestId == testId)
                .Select(tq => tq.Question)
                .OrderBy(_ => Guid.NewGuid())
                .ToListAsync();

            if (!questions.Any())
            {
                TempData["Error"] = "No questions in this test.";
                return RedirectToAction("ListTests", new { subject = test.Subject });
            }

            ViewBag.TestId = testId;
            ViewBag.TestName = test.TestName ?? "Unnamed Test";
            ViewBag.Subject = test.Subject;
            ViewBag.QuestionCount = questions.Count;
            return View("TestPage", questions);
        }

        [HttpPost("SubmitTest")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTest(Dictionary<int, string> answers, string testId)
        {
            // Debugging: Log the submitted testId
            TempData["SubmittedTestId"] = testId ?? "null";

            if (!int.TryParse(testId, out int parsedTestId))
            {
                TempData["Error"] = $"Invalid test ID format: {testId}";
                return RedirectToAction(nameof(SelectSubject));
            }

            var test = await _context.Tests.FindAsync(parsedTestId);
            if (test == null)
            {
                TempData["Error"] = "Test not found.";
                return RedirectToAction(nameof(SelectSubject));
            }

            if (answers == null || !answers.Any())
            {
                TempData["Error"] = "No answers submitted.";
                return RedirectToAction("ListTests", new { subject = test.Subject });
            }

            int score = 0;
            foreach (var entry in answers)
            {
                var question = await _context.Questions.FindAsync(entry.Key);
                if (question != null && string.Equals(question.CorrectAnswer?.Trim(), entry.Value?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    score++;
                }
            }

            TempData["Score"] = score;
            TempData["Total"] = answers.Count;
            TempData["TestName"] = test.TestName;
            TempData["Subject"] = test.Subject;
            return RedirectToAction(nameof(Result));
        }

        [HttpGet("Result")]
        public IActionResult Result()
        {
            if (TempData["Error"] != null)
            {
                ViewBag.Error = TempData["Error"];
            }

            ViewBag.Score = TempData["Score"] != null ? Convert.ToInt32(TempData["Score"]) : 0;
            ViewBag.Total = TempData["Total"] != null ? Convert.ToInt32(TempData["Total"]) : 0;
            ViewBag.TestName = TempData["TestName"] as string ?? "Unnamed Test";
            ViewBag.Subject = TempData["Subject"] as string ?? "Unknown Subject";
            ViewBag.SubmittedTestId = TempData["SubmittedTestId"] as string ?? "Unknown";
            return View();
        }
    }
}