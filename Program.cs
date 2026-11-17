using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var regionName = builder.Configuration["AWS:Region"] ?? "us-east-1";
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(RegionEndpoint.GetBySystemName(regionName)));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/uploads", async (IFormFile file, IAmazonS3 s3, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    const long maxFileSize = 10 * 1024 * 1024;
    var bucketName = configuration["AWS:S3Bucket"];

    if (string.IsNullOrWhiteSpace(bucketName))
    {
        return Results.Problem("Set AWS__S3Bucket to the name of your S3 bucket.", statusCode: (int)HttpStatusCode.ServiceUnavailable);
    }

    if (file.Length == 0)
    {
        return Results.BadRequest(new { error = "Choose a non-empty file." });
    }

    if (file.Length > maxFileSize)
    {
        return Results.BadRequest(new { error = "Files must be 10 MB or smaller." });
    }

    var safeFileName = Path.GetFileName(file.FileName);
    var objectKey = $"uploads/{Guid.NewGuid():N}/{safeFileName}";

    await using var stream = file.OpenReadStream();
    var request = new PutObjectRequest
    {
        BucketName = bucketName,
        Key = objectKey,
        InputStream = stream,
        ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
    };

    await s3.PutObjectAsync(request, cancellationToken);
    return Results.Ok(new { bucket = bucketName, key = objectKey, size = file.Length });
})
.DisableAntiforgery();

app.MapGet("/", () => Results.Content("""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>S3 Upload Starter</title>
  <style>
    body { font: 16px system-ui, sans-serif; max-width: 42rem; margin: 4rem auto; padding: 0 1rem; color: #182230; }
    main { border: 1px solid #d0d5dd; border-radius: 12px; padding: 2rem; }
    button { background: #175cd3; color: white; border: 0; border-radius: 6px; padding: .7rem 1rem; cursor: pointer; }
    pre { white-space: pre-wrap; overflow-wrap: anywhere; background: #f2f4f7; padding: 1rem; border-radius: 6px; }
  </style>
</head>
<body>
  <main>
    <h1>S3 Upload Starter</h1>
    <p>Choose a file up to 10 MB to upload it to your configured Amazon S3 bucket.</p>
    <form id="upload-form">
      <p><input type="file" name="file" required></p>
      <button type="submit">Upload to S3</button>
    </form>
    <pre id="result" aria-live="polite">Ready.</pre>
  </main>
  <script>
    const form = document.querySelector('#upload-form');
    const result = document.querySelector('#result');
    form.addEventListener('submit', async (event) => {
      event.preventDefault();
      result.textContent = 'Uploading…';
      try {
        const response = await fetch('/api/uploads', { method: 'POST', body: new FormData(form) });
        const body = await response.json();
        result.textContent = response.ok ? JSON.stringify(body, null, 2) : (body.detail || body.error || 'Upload failed.');
      } catch (error) {
        result.textContent = `Upload failed: ${error.message}`;
      }
    });
  </script>
</body>
</html>
""", "text/html"));

app.Run();
