using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Mustache;

string rootDir = args.Length > 0 ? args[0] : "..\\.";

var options = new JsonSerializerOptions() { AllowTrailingCommas = true };
var petData = JsonSerializer.Deserialize<Pet[]>(File.ReadAllText(Path.Combine(rootDir, "pets.json")), options).OrderBy(p => p.name);

var template = File.ReadAllText(Path.Combine(rootDir, "template.html"));
var rendered = Template.Compile(template).Render(new { pets = await makeBase64Encoded(petData) });
File.WriteAllText(Path.Combine(rootDir, "index.html"), rendered);

async static Task<IEnumerable<Pet>> makeBase64Encoded(IEnumerable<Pet> pets)
{
    var client = new HttpClient();
    var b64Pets = new List<Pet>();
    foreach (var pet in pets)
    {
        var imgData = await getData(client, pet, retryCountOnFailure: 3);
        string b64 = Convert.ToBase64String(await imgData.Content.ReadAsByteArrayAsync());
        b64Pets.Add(pet with { img = "data:image/jpeg;base64," + b64 });
    }

    return b64Pets;
}

async static Task<HttpResponseMessage> getData(HttpClient client, Pet pet, int retryCountOnFailure)
{
    for (int i = 0; i < retryCountOnFailure; i++)
    {
        try
        {
            return await client.GetAsync(pet.img);
        }
        catch (HttpRequestException e)
        {
            // Possible temporarily network error. Keep retry 'retryCountOnFailure' times before throwing.
            if (i == retryCountOnFailure -1)
            {
                Console.WriteLine($"Couldn't add pet {pet.name}. Error was " + e.ToString());
                throw;
            }
        }
    }

    throw new InvalidOperationException("This shouldn't be reachable.");
}

record Pet(string name, string jobTitle, string img);