# Local Azurite only. Does not read Azure credentials or change public media.
$ErrorActionPreference = 'Stop'
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('nexo-chat-storage-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><PackageReference Include="Azure.Storage.Blobs" Version="12.30.1" /></ItemGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $scratch 'Storage.csproj') -Encoding UTF8
@'
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Security.Cryptography;
var client = new BlobServiceClient("UseDevelopmentStorage=true");
var container = client.GetBlobContainerClient("chat-private");
await container.CreateIfNotExistsAsync(PublicAccessType.None);
if ((await container.GetPropertiesAsync()).Value.PublicAccess != PublicAccessType.None)
    throw new Exception("chat-private must deny public access.");
var probe = container.GetBlobClient("smoke/" + Guid.NewGuid().ToString("N"));
try {
    var ciphertext = RandomNumberGenerator.GetBytes(64);
    await probe.UploadAsync(BinaryData.FromBytes(ciphertext), overwrite: false);
    var read = (await probe.DownloadContentAsync()).Value.Content.ToArray();
    if (!ciphertext.SequenceEqual(read)) throw new Exception("Blob roundtrip failed.");
    using var http = new HttpClient();
    var anonymous = await http.GetAsync(probe.Uri);
    if (anonymous.IsSuccessStatusCode) throw new Exception("Anonymous access unexpectedly allowed.");
    Console.WriteLine($"CHAT-STORAGE-OK: private container, ciphertext roundtrip, anonymous HTTP {(int)anonymous.StatusCode}.");
} finally { await probe.DeleteIfExistsAsync(); }
'@ | Set-Content -LiteralPath (Join-Path $scratch 'Program.cs') -Encoding UTF8
dotnet run --project (Join-Path $scratch 'Storage.csproj') --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Local chat storage preparation failed.' }
