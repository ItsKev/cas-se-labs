using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

const long MaxFileSize = 10 * 1024 * 1024;
const string MetaFileName = "originalname";

// Allowed types: extension -> content type
var allowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    [".pdf"] = "application/pdf",
    [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    [".jpg"] = "image/jpeg",
    [".jpeg"] = "image/jpeg",
    [".png"] = "image/png",
};

var builder = WebApplication.CreateBuilder(args);

// Local: connection string for Azurite. In Azure: only the account URI, access via Managed Identity.
var connectionString = builder.Configuration["Storage:ConnectionString"];
var blobServiceUri = builder.Configuration["Storage:BlobServiceUri"];
var containerName = builder.Configuration["Storage:Container"] ?? "dokumente";

builder.Services.AddSingleton(_ =>
{
    var service = !string.IsNullOrEmpty(connectionString)
        ? new BlobServiceClient(connectionString)
        : new BlobServiceClient(new Uri(blobServiceUri
              ?? throw new InvalidOperationException("Storage:BlobServiceUri is not configured")),
              new DefaultAzureCredential());
    return service.GetBlobContainerClient(containerName);
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = MaxFileSize);

var app = builder.Build();

// Locally Azurite starts empty. In Azure Terraform creates the container; the identity may not create containers.
if (!string.IsNullOrEmpty(connectionString))
{
    await app.Services.GetRequiredService<BlobContainerClient>().CreateIfNotExistsAsync();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/healthz", () => Results.Ok("ok"));

// F1: upload a file
app.MapPost("/api/documents", async (HttpRequest request, BlobContainerClient container, CancellationToken ct) =>
{
    // TODO F1
    // 1. Read the first file of the form (request.ReadFormAsync). Answer 400 with { error } if there is none or it is empty.
    // 2. Reject files over MaxFileSize and extensions that are not in allowedTypes (400 with { error }).
    // 3. Use a new GUID as blob name. Store the original file name only as metadata (key MetaFileName, URL-encoded).
    // 4. Upload the stream with the content type from allowedTypes.
    // 5. Answer 201 with { id, fileName, status = "hochgeladen" }.
    await Task.CompletedTask;
    return Results.StatusCode(StatusCodes.Status501NotImplemented);
}).DisableAntiforgery();

// F2: list all documents
app.MapGet("/api/documents", async (BlobContainerClient container, CancellationToken ct) =>
{
    // TODO F2
    // List the blobs of the container including their metadata (BlobTraits.Metadata).
    // Map every blob to a DocumentInfo (original name from the metadata, newest first) and answer 200.
    await Task.CompletedTask;
    return Results.StatusCode(StatusCodes.Status501NotImplemented);
});

// F3: download a file
app.MapGet("/api/documents/{id:guid}/content", async (Guid id, BlobContainerClient container, CancellationToken ct) =>
{
    // TODO F3
    // Answer 404 if the blob does not exist. Otherwise stream it back with its content type
    // and the original file name from the metadata (Results.File).
    await Task.CompletedTask;
    return Results.StatusCode(StatusCodes.Status501NotImplemented);
});

app.Run();

record DocumentInfo(string Id, string FileName, string? ContentType, long Size, DateTimeOffset? UploadedAt, string Status);
