using KcetPrep1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KcetPrep1.Data
{
    public class DataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataService> _logger;
        private readonly string _csvPath;

        public DataService(ApplicationDbContext context, ILogger<DataService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _csvPath = configuration.GetValue<string>("CsvDataPath") ?? @"C:\Users\hirem\source\repos\KcetPrep1\kcet_cleaned.csv";
        }

        public async Task SeedDatabaseFromCsvAsync(string csvDataPath)
        {
            try
            {
                // Check if the Colleges table already has data
                if (await _context.Colleges.AnyAsync())
                {
                    _logger.LogInformation("Colleges table already contains data. Skipping seeding.");
                    return;
                }

                // Read CSV data
                var records = LoadCsvData();
                if (!records.Any())
                {
                    _logger.LogWarning("No records found in CSV file. Seeding aborted.");
                    return;
                }

                // Transform CSV records into College entities
                var colleges = TransformCsvToColleges(records);

                // Add to database
                await _context.Colleges.AddRangeAsync(colleges);
                int changes = await _context.SaveChangesAsync();
                _logger.LogInformation($"Seeded {changes} colleges into the database.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database from CSV.");
                throw;
            }
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
                    BadDataFound = args => _logger.LogWarning($"Bad CSV data in record: {args.RawRecord}")
                });

                var records = csv.GetRecords<KcetRecord>().ToList();
                _logger.LogInformation($"Loaded {records.Count} records from {_csvPath}");
                return records;
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, $"CSV file not found at {_csvPath}");
                return new List<KcetRecord>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading CSV file at {_csvPath}");
                return new List<KcetRecord>();
            }
        }

        private List<College> TransformCsvToColleges(List<KcetRecord> records)
        {
            var colleges = new List<College>();
            var groupedByCollegeAndBranch = records
                .GroupBy(r => (r.CollegeName, r.Branch))
                .ToList();

            foreach (var group in groupedByCollegeAndBranch)
            {
                var collegeName = group.Key.CollegeName?.Trim();
                var branch = group.Key.Branch?.Trim();

                if (string.IsNullOrEmpty(collegeName) || string.IsNullOrEmpty(branch))
                {
                    _logger.LogWarning($"Skipping record with empty college name or branch: {collegeName}, {branch}");
                    continue;
                }

                // Extract CETCode (assuming collegeName starts with code like "E001 College Name")
                string cetCode = collegeName.Split(' ', 2)[0];
                string name = collegeName.Split(' ', 2).Length > 1 ? collegeName.Split(' ', 2)[1] : collegeName;

                // Location is not provided in CSV; set to "Unknown" or derive if possible
                string location = "Unknown"; // Modify if location data is available

                // Aggregate cutoff ranks by category and year
                var cutoffRanks = new Dictionary<string, int>();
                foreach (var record in group)
                {
                    if (string.IsNullOrEmpty(record.Category) || string.IsNullOrEmpty(record.ClosingRank))
                    {
                        _logger.LogWarning($"Skipping record with empty category or rank: {collegeName}, {branch}, {record.Category}, {record.ClosingRank}");
                        continue;
                    }

                    if (int.TryParse(record.ClosingRank.Replace(",", ""), out int rank))
                    {
                        string key = $"{record.Year}_{record.Category.Trim().ToUpperInvariant()}";
                        cutoffRanks[key] = rank;
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid rank format: {record.ClosingRank} for {collegeName}, {branch}, {record.Category}");
                    }
                }

                var college = new College
                {
                    CETCode = cetCode,
                    Name = name,
                    Location = location,
                    Branch = branch,
                    CutoffRanks = cutoffRanks
                };

                colleges.Add(college);
            }

            return colleges;
        }
    }
}