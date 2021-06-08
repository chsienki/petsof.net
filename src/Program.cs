using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Mustache;

string rootDir = args.Length > 0 ? args[0] : "..\\.";
var client = new HttpClient();

var options = new JsonSerializerOptions() { AllowTrailingCommas = true };
var petData = JsonSerializer.Deserialize<Pet[]>(File.ReadAllText(Path.Combine(rootDir, "pets.json")), options).OrderBy(p => p.name);

var updatedPets = new List<Pet>();
foreach (var pet in petData)
{
    updatedPets.Add(pet with { img = await UploadIfNotExists(pet.img) });
}

var rendered = Template.Compile(template).Render(new { pets = updatedPets });
File.WriteAllText(Path.Combine(rootDir, "index.html"), rendered);

async Task<string> UploadIfNotExists(string url)
{
    string b64FileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));
    // if (!blobExists){

    // do conversion and upload
    // }
    try
    {
        var imgData = await client.GetAsync(url);
        var resizedImage = await imgData.Content.ReadAsByteArrayAsync();
        //await putBlobClientAsync(resizedImage);
    }
    catch (Exception)
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