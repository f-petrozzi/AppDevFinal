using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EduInsight.Models;
using EduInsight.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace EduInsight.Services;

public class CollegeScorecardService : IEnrollmentBenchmarkService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly CollegeScorecardOptions _options;
    private readonly ILogger<CollegeScorecardService> _logger;

    public CollegeScorecardService(
        HttpClient httpClient,
        IOptions<CollegeScorecardOptions> options,
        ILogger<CollegeScorecardService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl)
            && Uri.TryCreate(AppendTrailingSlash(_options.BaseUrl), UriKind.Absolute, out var baseUri))
        {
            _httpClient.BaseAddress = baseUri;
        }
    }

    public async Task<BenchmarkSummary> GetBenchmarksAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = string.IsNullOrWhiteSpace(_options.ApiKey) ? "DEMO_KEY" : _options.ApiKey;
        if (apiKey.Equals("DEMO_KEY", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("College Scorecard API key is not configured. Falling back to DEMO_KEY which has strict rate limits.");
        }

        var perPage = Math.Clamp(_options.ResultPageSize, 5, 100).ToString();

        var queryParams = new Dictionary<string, string?>
        {
            ["fields"] = "school.name,latest.student.size,latest.admissions.admission_rate.overall",
            ["per_page"] = perPage,
            ["sort"] = "latest.student.size:desc",
            ["api_key"] = apiKey
        };

        if (!string.IsNullOrWhiteSpace(_options.CipCodes))
        {
            queryParams["latest.programs.cip_4_digit.code"] = _options.CipCodes;
        }

        var requestUri = QueryHelpers.AddQueryString("schools", queryParams);

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("College Scorecard API returned {Status} for {Uri}", response.StatusCode, requestUri);
                return new BenchmarkSummary();
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<CollegeScorecardResponse>(contentStream, SerializerOptions, cancellationToken);

            if (payload?.Results is null || payload.Results.Count == 0)
            {
                _logger.LogWarning("College Scorecard response did not include any results for request {Uri}", requestUri);
                return new BenchmarkSummary();
            }

        var sizes = payload.Results
            .Select(r => r.StudentSize)
            .Where(v => v.HasValue && v.Value > 0)
            .Select(v => v!.Value)
            .ToList();

        var admissionRates = payload.Results
            .Select(r => r.AdmissionRateOverall)
            .Where(v => v.HasValue && v.Value > 0)
            .Select(v => v!.Value)
            .ToList();

            if (sizes.Count == 0)
            {
                return new BenchmarkSummary();
            }

            var averageEnrollment = sizes.Average();
            var medianEnrollment = CalculateMedian(sizes);
            var averageAdmissionRate = admissionRates.Count > 0 ? admissionRates.Average() : (double?)null;

            return new BenchmarkSummary
            {
                InstitutionCount = sizes.Count,
                AverageEnrollment = Math.Round(averageEnrollment, 2),
                MedianEnrollment = Math.Round(medianEnrollment, 2),
                AverageAdmissionRate = averageAdmissionRate.HasValue ? Math.Round(averageAdmissionRate.Value, 4) : null,
                RetrievedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch benchmark data from College Scorecard API.");
            return new BenchmarkSummary();
        }
    }

    private static double CalculateMedian(IReadOnlyList<double> values)
    {
        var ordered = values.OrderBy(x => x).ToArray();
        var mid = ordered.Length / 2;
        if (ordered.Length % 2 == 0)
        {
            return (ordered[mid - 1] + ordered[mid]) / 2d;
        }

        return ordered[mid];
    }

    private static string AppendTrailingSlash(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return baseUrl;
        }

        return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
    }

    private sealed record CollegeScorecardResponse
    {
        [JsonPropertyName("results")]
        public List<CollegeScorecardResult> Results { get; init; } = new();
    }

    private sealed record CollegeScorecardResult
    {
        [JsonPropertyName("school.name")]
        public string? SchoolName { get; init; }

        [JsonPropertyName("latest.student.size")]
        public double? StudentSize { get; init; }

        [JsonPropertyName("latest.admissions.admission_rate.overall")]
        public double? AdmissionRateOverall { get; init; }
    }
}
