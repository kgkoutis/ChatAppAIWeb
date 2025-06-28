using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.WriteIndented = true;
});

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var model = config["ModelName"];
var key = config["OpenAIKey"];

// Console.WriteLine($"Model: {model}");
// Console.WriteLine($"Key: {key}");

var chatClient = new OpenAIClient(key)
    .GetChatClient(model)
    .AsIChatClient();

var context = File.ReadAllText("./person.prompt.md");

var systemPrompt = new ChatMessage(ChatRole.System, context);
var app = builder.Build();
app.UseHttpsRedirection();

app.MapPost("/extract-person", async (PromptRequest request) =>
{
    var chatHistory = new List<ChatMessage>
    {
        systemPrompt,
        new(ChatRole.User, request.Prompt)
    };

    var rawResponse = "";
    await foreach (var chunk in chatClient.GetStreamingResponseAsync(chatHistory))
    {
        rawResponse += chunk.Text;
    }

    PersonExtractionResult? result;
    try
    {
        result = JsonSerializer.Deserialize<PersonExtractionResult>(rawResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { error = "AI returned invalid JSON." });
    }

    result ??= new PersonExtractionResult();

    // Normalize the result as you see fit
    var age = int.TryParse(result.Age, out var ageValue) ? ageValue : 0;
    if (age is < 0 or > 130)
    {
        return Results.Json(new { FirstName = result.FirstName ?? string.Empty, LastName = result.LastName ?? string.Empty });
    }
    return Results.Json(new
    {
        FirstName = result.FirstName ?? string.Empty,
        LastName = result.LastName ?? string.Empty,
        Age = age
    });
});
app.Run();

public record PromptRequest(string Prompt);

public class PersonExtractionResult
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Age { get; set; }
}
