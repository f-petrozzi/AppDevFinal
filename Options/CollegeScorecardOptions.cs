namespace EduInsight.Options;

public class CollegeScorecardOptions
{
    public const string SectionName = "CollegeScorecard";

    public string BaseUrl { get; set; } = "https://api.data.gov/ed/collegescorecard/v1/";

    public string ApiKey { get; set; } = string.Empty;

    public string? CipCodes { get; set; }

    public int ResultPageSize { get; set; } = 25;
}
