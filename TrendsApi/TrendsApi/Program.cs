using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GoogleTrendsApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapPost("/trends", async (HttpContext context, TrendsApiRequest apiRequest) =>
{
    var json = await RetryWithFibonacciAsync(() => Api.GetInterestOverTime(
        keywords: [apiRequest.SearchTerm1, apiRequest.SearchTerm2],
        geo: apiRequest.Country,
        time: apiRequest.DateOptions ?? DateOptions.FromStart
    ));

    var data = json?.AsArray();

    if (data == null || data.Count == 0)
        return Results.BadRequest("No data returned from API.");

    var team1Avg = data.Average(item => (int)item!["value"]![0]!);
    var team2Avg = data.Average(item => (int)item!["value"]![1]!);

    return Results.Ok(new
    {
        team_1_points = Math.Round(team1Avg, 2),
        team_2_points = Math.Round(team2Avg, 2)
    });
})
.WithName("GetGoogleTrends");

app.Run();


static async Task<JsonNode?> RetryWithFibonacciAsync(
    Func<Task<JsonNode?>> operation,
    int maxRetries = 5,
    int baseDelayMs = 300)
{
    int a = 0, b = 1;

    for (int attempt = 0; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (TaskCanceledException)
        {
            if (attempt == maxRetries)
                throw;

            int delay = baseDelayMs * b;
            await Task.Delay(delay);

            int temp = a + b;
            a = b;
            b = temp;
        }
        catch (Exception)
        {
            throw;
        }
    }

    return null;
}

internal class TrendsApiRequest
{
    [JsonPropertyName("country")]
    public string Country { get; init; }

    [JsonPropertyName("time_horizon")]
    public DateOptions? DateOptions { get; init; }

    [JsonPropertyName("team_1_answer")]
    public string SearchTerm1 { get; init; }

    [JsonPropertyName("team_2_answer")]
    public string SearchTerm2 { get; init; }
}
