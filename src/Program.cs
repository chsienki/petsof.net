using Mustache;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

string rootDir = args.Length > 0 ? args[0] : "..\\.";

var template = File.ReadAllText(Path.Combine(rootDir, "template.html"));
var petData = JsonConvert.DeserializeObject<Pet[]>(File.ReadAllText(Path.Combine(rootDir, "pets.json"))).OrderBy(p => p.name);

var rendered = Template.Compile(template).Render(new { pets = await makeBase64Encoded(petData) });
File.WriteAllText(Path.Combine(rootDir, "index.html"), rendered);

async static Task<IEnumerable<Pet>> makeBase64Encoded(IEnumerable<Pet> pets)
{
    var client = new HttpClient();
    var b64Pets = new List<Pet>();
    foreach (var pet in pets)
    {
        string b64 = string.Empty;
        try
        {
            var imgData = await client.GetAsync(pet.img);
            b64 = Convert.ToBase64String(await imgData.Content.ReadAsByteArrayAsync());
        }
        catch(Exception e)
        {
            Console.WriteLine("Couldn't add pet "+pet.name+". Error was "+e.ToString());
        }
        finally
        {
            b64Pets.Add(pet with { img = "data:image/jpeg;base64," + b64 });
        }
    }
    return b64Pets;
}

record Pet(string name, string jobTitle, string img);