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

    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "Keine Datei übermittelt." });
    if (file.Length > MaxFileSize)
        return Results.BadRequest(new { error = "Datei ist grösser als 10 MB." });

    var extension = Path.GetExtension(file.FileName);
    if (!allowedTypes.TryGetValue(extension, out var contentType))
        return Results.BadRequest(new { error = "Erlaubt sind PDF, DOCX, JPG und PNG." });

    // GUID as blob name; the original name is only stored as metadata (URL-encoded: metadata must be ASCII)
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

app.MapGet("/api/documents", async (BlobContainerClient container, CancellationToken ct) =>
{
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

app.MapGet("/api/documents/{id:guid}/content", async (Guid id, BlobContainerClient container, CancellationToken ct) =>
{
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

record DocumentInfo(string Id, string FileName, string? ContentType, long Size, DateTimeOffset? UploadedAt, string Status);
