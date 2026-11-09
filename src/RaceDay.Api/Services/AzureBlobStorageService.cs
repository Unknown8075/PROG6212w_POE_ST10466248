
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public class AzureBlobStorageService
{
	private readonly BlobServiceClient _client;

	public AzureBlobStorageService(IConfiguration config)
	{
		var connectionString =
			config["AzureStorage:ConnectionString"];

		if (string.IsNullOrWhiteSpace(connectionString))
			throw new InvalidOperationException(
				"Azure Storage connection string is not configured.");

		_client = new BlobServiceClient(connectionString);
	}

	public async Task<string> UploadAsync(
		IFormFile file,
		string containerName)
	{
		if (file.Length == 0 || file.Length > 5 * 1024 * 1024)
			throw new ArgumentException(
				"Image must be between 1 byte and 5 MB.");

		var extension = Path.GetExtension(file.FileName)
			.ToLowerInvariant();

		var types = new Dictionary<string, string>
		{
			[".jpg"] = "image/jpeg",
			[".jpeg"] = "image/jpeg",
			[".png"] = "image/png",
			[".webp"] = "image/webp"
		};

		if (!types.TryGetValue(extension, out var contentType))
			throw new ArgumentException(
				"Only JPG, PNG and WEBP images are supported.");

		await using var stream = file.OpenReadStream();

		byte[] header = new byte[12];
		int bytesRead = await stream.ReadAsync(header);

		bool valid = contentType switch
		{
			"image/jpeg" => bytesRead >= 3 &&
				header[0] == 0xFF &&
				header[1] == 0xD8 &&
				header[2] == 0xFF,

			"image/png" => bytesRead >= 8 &&
				header.AsSpan(0, 8).SequenceEqual(
					new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),

			"image/webp" => bytesRead >= 12 &&
				System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
				System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",

			_ => false
		};

		if (!valid)
			throw new ArgumentException(
				"Invalid image file.");

		stream.Position = 0;

		var container = _client.GetBlobContainerClient(containerName);
		await container.CreateIfNotExistsAsync();

		var blobName = $"{Guid.NewGuid():N}{extension}";
		var blob = container.GetBlobClient(blobName);

		await blob.UploadAsync(
			stream,
			new BlobUploadOptions
			{
				HttpHeaders = new BlobHttpHeaders
				{
					ContentType = contentType
				}
			});

		return blob.Uri.ToString();
	}

	public async Task<(Stream Content, string ContentType)> DownloadAsync(
		string blobUrl,
		string containerName)
	{
		var uri = new Uri(blobUrl);
		var path = uri.AbsolutePath.Trim('/');

		var marker = "/" + containerName + "/";
		var markerIndex = path.IndexOf(
			marker, StringComparison.OrdinalIgnoreCase);

		if (markerIndex < 0)
			throw new ArgumentException("Invalid blob location.");

		var blobName = Uri.UnescapeDataString(
			path[(markerIndex + marker.Length)..]);

		if (string.IsNullOrWhiteSpace(blobName) ||
			blobName.Contains('/') ||
			blobName.Contains('\\'))
			throw new ArgumentException("Invalid blob name.");

		var container = _client.GetBlobContainerClient(containerName);
		var blob = container.GetBlobClient(blobName);

		var download = await blob.DownloadStreamingAsync();

		return (
			download.Value.Content,
			download.Value.Details.ContentType
				?? "application/octet-stream"
		);
	}
}
