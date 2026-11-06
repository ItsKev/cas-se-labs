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
    // Read the form ourselves: binding errors (e.g. file over the limit) become a readable message
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

    // Validate before touching storage. User-facing messages are German on purpose.
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "Keine Datei übermittelt." });
    if (file.Length > MaxFileSize)
        return Results.BadRequest(new { error = "Datei ist grösser als 10 MB." });

    // The extension check is no security feature: a client can name any content like this (see lab "Hinweise").
    var extension = Path.GetExtension(file.FileName);
    if (!allowedTypes.TryGetValue(extension, out var contentType))
        return Results.BadRequest(new { error = "Erlaubt sind PDF, DOCX, JPG und PNG." });

    // GUID as blob name; the original name is only stored as metadata (URL-encoded: metadata must be ASCII)
    // A GUID cannot be guessed and cannot collide (N1). Path.GetFileName strips any folder part of the name.
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

    // 201 Created, with the address of the new document in the Location header.
    return Results.Created($"/api/documents/{id}/content", new { id, fileName = file.FileName, status = "hochgeladen" });
}).DisableAntiforgery();

// F2: list all documents, newest first.
app.MapGet("/api/documents", async (BlobContainerClient container, CancellationToken ct) =>
{
    var items = new List<DocumentInfo>();
    // Listing returns blob metadata only when asked for (BlobTraits.Metadata).
    // The listing pages through the container; await foreach hides the paging.
    await foreach (var item in container.GetBlobsAsync(BlobTraits.Metadata, cancellationToken: ct))
    {
        // Blobs not uploaded by this API (e.g. via Storage Explorer) have no metadata: fall back to the blob name.
        item.Metadata.TryGetValue(MetaFileName, out var encoded);
        items.Add(new DocumentInfo(
            item.Name,
            encoded is null ? item.Name : Uri.UnescapeDataString(encoded),
            item.Properties.ContentType,
            item.Properties.ContentLength ?? 0,
            item.Properties.CreatedOn,
            "hochgeladen"));
    }
    // Blob Storage returns no defined order, so sort here.
    return Results.Ok(items.OrderByDescending(i => i.UploadedAt));
});

// F3: download a file. The route constraint {id:guid} only lets valid GUIDs through,
// so nobody can pass a path or another blob name.
app.MapGet("/api/documents/{id:guid}/content", async (Guid id, BlobContainerClient container, CancellationToken ct) =>
{
    var blob = container.GetBlobClient(id.ToString());
    if (!await blob.ExistsAsync(ct))
        return Results.NotFound();

    // Properties carry the content type and the metadata with the original file name.
    var props = (await blob.GetPropertiesAsync(cancellationToken: ct)).Value;
    props.Metadata.TryGetValue(MetaFileName, out var encoded);
    var fileName = encoded is null ? id.ToString() : Uri.UnescapeDataString(encoded);

    // Stream the blob to the client instead of loading it into memory.
    // Results.File sets Content-Disposition with the original name (umlauts and spaces are encoded correctly).
    var stream = await blob.OpenReadAsync(cancellationToken: ct);
    return Results.File(stream, props.ContentType, fileName);
});

app.Run();

// Shape of one list entry; serialised as JSON with camelCase names (id, fileName, ...), as the SPA expects.
record DocumentInfo(string Id, string FileName, string? ContentType, long Size, DateTimeOffset? UploadedAt, string Status);
