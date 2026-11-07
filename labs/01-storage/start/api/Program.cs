// Document platform API (minimal API, one file).
// - Stores uploaded files as blobs in one container, lists them, and streams them back.
// - Also serves the SPA (wwwroot/index.html), so browser and API share one address.
// - The storage backend is chosen by configuration only: Azurite locally, a real storage account in Azure.
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

// Upload limit in bytes (10 MB). Used twice below: form parsing limit and explicit size check.
const long MaxFileSize = 10 * 1024 * 1024;
// Key of the blob metadata entry that holds the original file name.
const string MetaFileName = "originalname";

// Allow-list: file extension -> content type. Only these types are accepted (F4).
// The content type stored with the blob comes from here, never from the client.
var allowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    [".pdf"] = "application/pdf",
    [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    [".jpg"] = "image/jpeg",
    [".jpeg"] = "image/jpeg",
    [".png"] = "image/png",
};

// The builder reads configuration from appsettings*.json and environment variables.
// The environment variable Storage__ConnectionString (docker-compose.yml) maps to Storage:ConnectionString.
var builder = WebApplication.CreateBuilder(args);

// Local: connection string for Azurite. In Azure: only the account URI, access via Managed Identity.
var connectionString = builder.Configuration["Storage:ConnectionString"];
var blobServiceUri = builder.Configuration["Storage:BlobServiceUri"];
var containerName = builder.Configuration["Storage:Container"] ?? "dokumente";

// One BlobContainerClient for the whole app, injected into the endpoints below.
// This is the only place that knows where the data lives (N3): the endpoints stay identical in Azure.
builder.Services.AddSingleton(_ =>
{
    var service = !string.IsNullOrEmpty(connectionString)
        // Local: the connection string contains the account key (public Azurite development key).
        ? new BlobServiceClient(connectionString)
        // Azure: no secret. DefaultAzureCredential picks up the Managed Identity of the Container App
        // (and the developer login when run on a laptop).
        : new BlobServiceClient(new Uri(blobServiceUri
              ?? throw new InvalidOperationException("Storage:BlobServiceUri is not configured")),
              new DefaultAzureCredential());
    return service.GetBlobContainerClient(containerName);
});

// Form parts larger than this limit make form parsing throw. The upload endpoint turns that into a readable 400.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = MaxFileSize);

var app = builder.Build();

// Locally Azurite starts empty. In Azure Terraform creates the container; the identity may not create containers.
if (!string.IsNullOrEmpty(connectionString))
{
    await app.Services.GetRequiredService<BlobContainerClient>().CreateIfNotExistsAsync();
}

// Serve the SPA: UseDefaultFiles maps "/" to wwwroot/index.html, UseStaticFiles delivers files from wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();

// Liveness check: answers 200 as soon as the app runs. Handy for curl and later for the Container App.
app.MapGet("/healthz", () => Results.Ok("ok"));

// F1: upload a file. Parameters are injected: the request, the container client (from the service
// registration above) and a token that is cancelled when the client disconnects.
// Antiforgery is switched off (DisableAntiforgery below) on purpose: there is no sign-in, so no user context to protect (lab "Hinweise").
app.MapPost("/api/documents", async (HttpRequest request, BlobContainerClient container, CancellationToken ct) =>
{
    // TODO F1
    // 1. Read the first file of the form (request.ReadFormAsync). Answer 400 with { error } if there is none or it is empty.
    // 2. Reject files over MaxFileSize and extensions that are not in allowedTypes (400 with { error }).
    // 3. Use a new GUID as blob name. Store the original file name only as metadata (key MetaFileName, URL-encoded).
    // 4. Upload the stream with the content type from allowedTypes.
    // 5. Answer 201 with { id, fileName, status = "hochgeladen" }.
    IFormFile? file;
    try
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "Keine Datei übermittelt." });
        file = (await request.ReadFormAsync(ct)).Files.FirstOrDefault();
    }
    catch (Exception e) when (e is InvalidDataException or BadHttpRequestException)
    {
        return Results.BadRequest(new { error = "Datei ist grösser als 10 MB." });
    }

    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "Keine Datei übermittelt." });
    if (file.Length > MaxFileSize)
        return Results.BadRequest(new { error = "Datei ist grösser als 10 MB." });

    var extension = Path.GetExtension(file.FileName);
    if (!allowedTypes.TryGetValue(extension, out var contentType))
        return Results.BadRequest(new { error = "Erlaubt sind PDF, DOCX, JPG und PNG." });

    var id = Guid.NewGuid().ToString();
    var blob = container.GetBlobClient(id);
    await using var stream = file.OpenReadStream();
    await blob.UploadAsync(stream, new BlobUploadOptions
    {
        HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        Metadata = new Dictionary<string, string>
        {
            [MetaFileName] = Uri.EscapeDataString(Path.GetFileName(file.FileName)),
        },
    }, ct);

    return Results.Created($"/api/documents/{id}/content", new { id, fileName = file.FileName, status = "hochgeladen" });
}).DisableAntiforgery();

// F2: list all documents
app.MapGet("/api/documents", async (BlobContainerClient container, CancellationToken ct) =>
{
    // TODO F2
    // List the blobs of the container including their metadata (BlobTraits.Metadata).
    // Map every blob to a DocumentInfo (original name from the metadata, newest first) and answer 200.
    var items = new List<DocumentInfo>();
    await foreach (var item in container.GetBlobsAsync(BlobTraits.Metadata, cancellationToken: ct))
    {
        item.Metadata.TryGetValue(MetaFileName, out var encoded);
        items.Add(new DocumentInfo(
            item.Name,
            encoded is null ? item.Name : Uri.UnescapeDataString(encoded),
            item.Properties.ContentType,
            item.Properties.ContentLength ?? 0,
            item.Properties.CreatedOn,
            "hochgeladen"));
    }
    return Results.Ok(items.OrderByDescending(i => i.UploadedAt));
});

// F3: download a file
app.MapGet("/api/documents/{id:guid}/content", async (Guid id, BlobContainerClient container, CancellationToken ct) =>
{
    // TODO F3
    // Answer 404 if the blob does not exist. Otherwise stream it back with its content type
    // and the original file name from the metadata (Results.File).
    var blob = container.GetBlobClient(id.ToString());
    if (!await blob.ExistsAsync(ct))
        return Results.NotFound();

    var props = (await blob.GetPropertiesAsync(cancellationToken: ct)).Value;
    props.Metadata.TryGetValue(MetaFileName, out var encoded);
    var fileName = encoded is null ? id.ToString() : Uri.UnescapeDataString(encoded);

    var stream = await blob.OpenReadAsync(cancellationToken: ct);
    return Results.File(stream, props.ContentType, fileName);
});

app.Run();

// Shape of one list entry; serialised as JSON with camelCase names (id, fileName, ...), as the SPA expects.
record DocumentInfo(string Id, string FileName, string? ContentType, long Size, DateTimeOffset? UploadedAt, string Status);
