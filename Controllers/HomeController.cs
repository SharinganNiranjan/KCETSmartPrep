using KcetPrep1.Data;
using KcetPrep1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using DocumentFormat.OpenXml.Bibliography;

namespace KcetPrep1.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataService _dataService;
        private readonly ILogger<HomeController> _logger;
        private readonly string _csvPath;

        public HomeController(DataService ds, ILogger<HomeController> logger, IConfiguration configuration)
        {
            _dataService = ds;
            _logger = logger;
            _csvPath = configuration.GetValue<string>("CsvDataPath")
                ?? @"C:\Users\hirem\source\repos\KcetPrep1\kcet_cleaned.csv";
        }
        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult PredictColleges() => View();

        [HttpPost]
        public IActionResult PredictColleges(int userRank, string userCategory, string userBranch)
        {
            // 1) validate as before...
            if (userRank <= 0 || string.IsNullOrWhiteSpace(userCategory))
            {
                ViewBag.Error = "Rank and category are required.";
                return View();
            }

            // 2) store all three inputs in ViewBag for the view to read back
            ViewBag.UserRank = userRank;
            ViewBag.UserCategory = userCategory;
            ViewBag.UserBranch = userBranch;

            // 3) your existing prediction logic
            var predictions = PredictCollegesInternal(userRank, userCategory, userBranch);
            if (!predictions.Any())
            {
                ViewBag.Error = $"No colleges found for rank {userRank}, category {userCategory}, branch {userBranch ?? "Any"} in the defined window.";
            }
            else
            {
                ViewBag.Predictions = predictions;
            }

            return View();
        }

       private List<CollegePredictionResult> PredictCollegesInternal(int userRank, string userCategory, string userBranch)
{
    var catFilter = userCategory.Trim().ToUpperInvariant();
    var branchFilter = userBranch?.Trim().ToUpperInvariant();

    // 1) Query colleges from the database via DataService
    //    (assumes DataService exposes a method or property to get College entities)
    var colleges = _dataService.GetColleges()   // or DbContext.Colleges if available
                       .ToList();             // force query execution:contentReference[oaicite:3]{index=3}

    // 2) Apply branch filter to colleges
    if (!string.IsNullOrEmpty(branchFilter))
    {
        colleges = colleges
            .Where(c => !string.IsNullOrEmpty(c.Branch) &&
                        c.Branch.Trim().ToUpperInvariant() == branchFilter)
            .ToList();
    }

    // 3) Define dynamic window around userRank
    int lower = System.Math.Max(userRank - 500, 1);
    int upper = (int)(userRank * 3.5);

    var results = new List<CollegePredictionResult>();

    // 4) For each college, examine its CutoffRanks dictionary entries
    foreach (var college in colleges)
    {
        if (college.CutoffRanks == null) 
            continue;

        foreach (var kvp in college.CutoffRanks)
        {
            // Split the dictionary key (format assumed "Year_Category", e.g. "2022_1G")
            string[] parts = kvp.Key.Split('_');  // .Split() usage:contentReference[oaicite:4]{index=4}
            if (parts.Length != 2) 
                continue;

            string yearStr = parts[0];
            string categoryKey = parts[1].ToUpperInvariant();
            if (categoryKey != catFilter)
                continue;  // Skip categories that don’t match

            // Parse the year and closing rank value
            if (!int.TryParse(yearStr, out int year))
                continue;  // Malformed year
            int closingRank = kvp.Value;

            // Filter by the dynamic rank window [lower, upper]
            if (closingRank >= lower && closingRank <= upper)
            {
                // Compute admission chance based on rank comparison
                string chance = userRank < closingRank
                                 ? "High"
                                 : userRank > closingRank
                                   ? "Low"
                                   : "Moderate";

                // Add to result list
                results.Add(new CollegePredictionResult {
                    CollegeName = college.Name.Trim(),
                    Branch      = college.Branch.Trim(),
                    Category    = categoryKey,
                    ClosingRank = closingRank,
                    Chance      = chance,
                    Years = year.ToString()

                });
            }
        }
    }

    // 5) Sort by ClosingRank ascending
    results = results.OrderBy(r => r.ClosingRank).ToList();
    return results;
}

        private List<KcetRecord> LoadCsvData()
        {
            try
            {
                using var fs = new FileStream(_csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    BadDataFound = args => _logger.LogWarning($"Bad CSV data: {args.RawRecord}")
                });

                var records = csv.GetRecords<KcetRecord>().ToList();
                _logger.LogInformation($"Successfully loaded {records.Count} records from {_csvPath}");
                return records;
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, $"CSV not found: {_csvPath}");
                return new List<KcetRecord>();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error reading CSV: {_csvPath}");
                return new List<KcetRecord>();
            }
        }

        [HttpGet]
        public IActionResult ExtractPDF() => View();

        [HttpPost]
        public async Task<IActionResult> ExtractPDFSubmit()
        {
            using var client = new HttpClient { Timeout = System.TimeSpan.FromMinutes(10) };
            using var form = new MultipartFormDataContent();
            var entries = new[]
            {
                new { Year = 2020, Path = @"C:\Users\hirem\source\repos\KcetPrep1\Kcet-2020.pdf" },
                new { Year = 2021, Path = @"C:\Users\hirem\source\repos\KcetPrep1\Kcet-2021.pdf" },
                new { Year = 2022, Path = @"C:\Users\hirem\source\repos\KcetPrep1\Kcet-2022.pdf" },
                new { Year = 2023, Path = @"C:\Users\hirem\source\repos\KcetPrep1\Kcet-2023.pdf" },
            };

            foreach (var e in entries)
            {
                try
                {
                    var bytes = await System.IO.File.ReadAllBytesAsync(e.Path);
                    var content = new ByteArrayContent(bytes);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                    form.Add(content, "files", Path.GetFileName(e.Path));
                    form.Add(new StringContent(e.Year.ToString()), "years");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, $"Failed to read PDF {e.Path}");
                    ViewBag.Response = $"Failed to read PDF for {e.Year}: {ex.Message}";
                    return View("ExtractPDF");
                }
            }

            try
            {
                var response = await client.PostAsync("http://localhost:5000/extract", form);
                var json = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(json);

                if (result["success"]?.Value<bool>() == true)
                {
                    ViewBag.Response = $"{result["rows"]} rows processed";
                }
                else
                {
                    ViewBag.Response = "Extraction failed: " + (result["error"]?.ToString() ?? "Unknown");
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("timed out"))
            {
                _logger.LogError(ex, "Extraction timed out");
                ViewBag.Response = "Extraction timed out. Please retry.";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Extraction error");
                ViewBag.Response = "Extraction failed: " + ex.Message;
            }

            return View("ExtractPDF");
        }
    }
}
