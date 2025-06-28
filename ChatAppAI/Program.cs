using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var model = config["ModelName"];
var key = config["OpenAIKey"];

// Console.WriteLine($"Model: {model}");
// Console.WriteLine($"Key: {key}");

var chatClient = new OpenAIClient(key).GetChatClient(model).AsIChatClient();
List<ChatMessage> chatHistory =
[
    new(ChatRole.System, """
                             You are a json-based AI assistant that helps outputing JSON data by examining potentially
                             unstructured text and extracting relevant information. Everything you output should be valid JSON.
                             You are ONLY outputting JSON data, no other text. NO greetings, NO explanations, NO additional text.
                             If you cannot extract any information, output an empty JSON object {}.
                             
                             You are always outputting a JSON object with the following structure:
                             {
                                 "FirstName": "value1",
                                 "LastName": "value2",
                                 "Age": "value3"
                             }
                             
                             Consequently, you will only a) look for contextual values within a text from the user regarding someone's FirstName, LastName, and Age.
                             b) output a JSON object with the values you found, or an empty JSON object if no values were found. If some values are not found, leave them empty.
                             You operate on a best-effort basis, meaning you will try to extract the values but if they are not present, you will output an empty JSON object.
                             c) If you find multiple values, you will output the first one you find for each field by using GOOD judgment. If you still feel ambivalent about
                             which value to choose for a specific field, better leave it empty.
                             d) Be mindful that sometimes regarding dates, the user might speak in terms of relative time (e.g., "last year", "next month", etc.).
                             Adjust your extraction logic accordingly to handle such cases.
                             
                             The meaning of your work is to extract the values from the text and output them in a JSON format for other code to use.
                             
                             Example user prompts:
                             - User: Jack Brown, only 4 years old, knew it all to well
                             - System: {"FirstName": "Jack", "LastName": "Brown", "Age": "4"}
                             
                             - User: I am 30 years old, my name is John Smith
                             - System: {"FirstName": "John", "LastName": "Smith", "Age": "30"}
                             
                             - User: My brother, John Snow, was exactly one year in the past, 31 years old.
                             - System: {"FirstName": "John", "LastName": "Snow", "Age": "32"}
                             
                             - User: My brother, John Snow, is an architect                                
                             - System: {"FirstName": "John", "LastName": "Snow"}
                             
                             - User: Mr Green, 45 years old, is a great person
                             - System: {"LastName": "Green", "Age": "45"}
                         """)
];

while (true)
{
    Console.WriteLine("Your prompt:");
    var userPrompt = Console.ReadLine();
    chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

    Console.WriteLine("AI Response:");
    var response = "";
    await foreach (var item in
                   chatClient.GetStreamingResponseAsync(chatHistory))
    {
        Console.Write(item.Text);
        response += item.Text;
    }
    chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
    Console.WriteLine();
}