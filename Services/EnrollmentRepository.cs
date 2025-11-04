using System.Globalization;
using System.Text.Json;
using EduInsight.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace EduInsight.Services;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ILogger<EnrollmentRepository> _logger;
    private readonly string _dataFilePath;
    private readonly string _seedCsvPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly object _syncRoot = new();
    private List<Enrollment> _enrollments = new();

    public EnrollmentRepository(IWebHostEnvironment environment, ILogger<EnrollmentRepository> logger)
    {
        _logger = logger;

        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(home))
        {
            var dataDir = Path.Combine(home, "site", "data");
            _dataFilePath = Path.Combine(dataDir, "enrollments.json");
        }
        else
        {
            _dataFilePath = Path.Combine(environment.ContentRootPath, "Data", "enrollments.json");
        }

        _seedCsvPath = Path.Combine(environment.WebRootPath ?? environment.ContentRootPath, "data", "enrollments.csv");
        EnsureStorageInitialized();
    }

    public Task<IReadOnlyList<Enrollment>> GetAllAsync()
    {
        lock (_syncRoot)
        {
            var ordered = _enrollments
                .OrderBy(e => e.StudentName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.StudentId, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
            return Task.FromResult<IReadOnlyList<Enrollment>>(ordered);
        }
    }

    public Task<Enrollment?> GetByStudentIdAsync(string studentId)
    {
        lock (_syncRoot)
        {
            var enrollment = _enrollments.FirstOrDefault(e =>
                string.Equals(e.StudentId, studentId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(enrollment is null ? null : Clone(enrollment));
        }
    }

    public Task AddAsync(Enrollment enrollment)
    {
        ArgumentNullException.ThrowIfNull(enrollment);

        lock (_syncRoot)
        {
            if (_enrollments.Any(e =>
                string.Equals(e.StudentId, enrollment.StudentId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"An enrollment already exists for Student ID '{enrollment.StudentId}'.");
            }

            _enrollments.Add(Clone(enrollment));
            Persist();
        }

        _logger.LogInformation("Added enrollment for student {StudentId}", enrollment.StudentId);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string originalStudentId, Enrollment updated)
    {
        ArgumentException.ThrowIfNullOrEmpty(originalStudentId);
        ArgumentNullException.ThrowIfNull(updated);

        lock (_syncRoot)
        {
            var existing = _enrollments.FirstOrDefault(e =>
                string.Equals(e.StudentId, originalStudentId, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                throw new KeyNotFoundException($"Enrollment with Student ID '{originalStudentId}' was not found.");
            }

            var changingId = !string.Equals(originalStudentId, updated.StudentId, StringComparison.OrdinalIgnoreCase);
            if (changingId && _enrollments.Any(e =>
                    string.Equals(e.StudentId, updated.StudentId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Cannot change to Student ID '{updated.StudentId}' because it already exists.");
            }

            existing.StudentId = updated.StudentId;
            existing.StudentName = updated.StudentName;
            existing.Program = updated.Program;
            existing.Term = updated.Term;
            existing.Gpa = Math.Round(updated.Gpa, 2);
            existing.EnrollmentDate = updated.EnrollmentDate;

            Persist();
        }

        _logger.LogInformation("Updated enrollment for student {StudentId} (original {OriginalId})", updated.StudentId, originalStudentId);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string studentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(studentId);

        bool removed;
        lock (_syncRoot)
        {
            var enrollment = _enrollments.FirstOrDefault(e =>
                string.Equals(e.StudentId, studentId, StringComparison.OrdinalIgnoreCase));

            if (enrollment is null)
            {
                removed = false;
            }
            else
            {
                _enrollments.Remove(enrollment);
                Persist();
                removed = true;
            }
        }

        if (removed)
        {
            _logger.LogInformation("Deleted enrollment for student {StudentId}", studentId);
        }
        return Task.FromResult(removed);
    }

    private void EnsureStorageInitialized()
    {
        var directory = Path.GetDirectoryName(_dataFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_dataFilePath))
        {
            var seeded = TrySeedFromCsv();
            Persist(seeded);
            _enrollments = seeded;
        }
        else
        {
            _enrollments = LoadFromDisk();
        }
    }

    private List<Enrollment> LoadFromDisk()
    {
        try
        {
            using var stream = File.OpenRead(_dataFilePath);
            var data = JsonSerializer.Deserialize<List<Enrollment>>(stream, _jsonOptions);
            return data?.Select(Clone).ToList() ?? new List<Enrollment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load enrollments from {File}. Using an empty dataset.", _dataFilePath);
            return new List<Enrollment>();
        }
    }

    private List<Enrollment> TrySeedFromCsv()
    {
        if (!File.Exists(_seedCsvPath))
        {
            _logger.LogWarning("Seed CSV {CsvPath} not found. Starting with an empty enrollment dataset.", _seedCsvPath);
            return new List<Enrollment>();
        }

        try
        {
            var lines = File.ReadAllLines(_seedCsvPath)
                .Skip(1)
                .Where(line => !string.IsNullOrWhiteSpace(line));

            var enrollments = new List<Enrollment>();
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length < 6)
                {
                    continue;
                }

                var enrollment = new Enrollment
                {
                    StudentId = parts[0].Trim(),
                    StudentName = parts[1].Trim(),
                    Program = parts[2].Trim(),
                    Term = parts[3].Trim(),
                    Gpa = ParseGpa(parts[4]),
                    EnrollmentDate = ParseDate(parts[5])
                };

                if (!string.IsNullOrEmpty(enrollment.StudentId))
                {
                    enrollments.Add(enrollment);
                }
            }

            return enrollments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed enrollments from CSV file {CsvPath}", _seedCsvPath);
            return new List<Enrollment>();
        }
    }

    private void Persist()
    {
        Persist(_enrollments);
    }

    private void Persist(List<Enrollment> source)
    {
        try
        {
            using var stream = File.Open(_dataFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            JsonSerializer.Serialize(stream, source, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write enrollments to {File}", _dataFilePath);
            throw;
        }
    }

    private static Enrollment Clone(Enrollment enrollment) => new()
    {
        StudentId = enrollment.StudentId,
        StudentName = enrollment.StudentName,
        Program = enrollment.Program,
        Term = enrollment.Term,
        Gpa = enrollment.Gpa,
        EnrollmentDate = enrollment.EnrollmentDate
    };

    private static double ParseGpa(string value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return Math.Clamp(Math.Round(parsed, 2), 0, 4);
        }
        return 0;
    }

    private static DateTime ParseDate(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed.Date;
        }

        return DateTime.Today;
    }
}
