using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Mustache;

string rootDir = args[0];
string azureConnection = Encoding.UTF8.GetString(Convert.FromBase64String(args[1]));
bool dryRun = args.Length > 2 ? true : false;


System.Diagnostics.Debugger.Launch();

var client = new HttpClient();
var blobContainer = new BlobContainerClient(azureConnection, "img");
blobContainer.CreateIfNotExists();

var template = File.ReadAllText(Path.Combine(rootDir, "template.html"));
var options = new JsonSerializerOptions() { AllowTrailingCommas = true };
var petData = JsonSerializer.Deserialize<Pet[]>(File.ReadAllText(Path.Combine(rootDir, "pets.json")), options).OrderBy(p => p.name);

var updatedPets = new List<Pet>();
foreach (var pet in petData)
{
    updatedPets.Add(pet with { img = await UploadIfNotExists(pet) });
}

var rendered = Template.Compile(template).Render(new { pets = updatedPets });
File.WriteAllText(Path.Combine(rootDir, "index.html"), rendered);

async Task<string> UploadIfNotExists(Pet pet)
{
    if (string.IsNullOrWhiteSpace(pet.img))
        return "";

    string b64FileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(pet.img));
    var blobClient = blobContainer.GetBlobClient(b64FileName);

    if (!blobClient.Exists())
    {
        try
        {
            var imgStream = await getData(client, pet, retryCountOnFailure: 3);
            if (!dryRun)
            {
                blobClient.Upload(imgStream);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Couldn't add pet {pet.name}. Error was " + e.ToString());
        }
    }

    return blobClient.Uri.AbsoluteUri.ToString();
}

async static Task<Stream> getData(HttpClient client, Pet pet, int retryCountOnFailure)
{
    for (int i = 0; i < retryCountOnFailure; i++)
    {
        try
        {
            var result = await client.GetAsync(pet.img);
            result.EnsureSuccessStatusCode();
            return await result.Content.ReadAsStreamAsync();
        }
        catch (HttpRequestException)
        {
            // Possible temporarily network error. Keep retry 'retryCountOnFailure' times before throwing.
            if (i == retryCountOnFailure - 1)
            {
                throw;
            }
        }
    }

    throw new InvalidOperationException("This shouldn't be reachable.");
}

record Pet(string name, string jobTitle, string img);