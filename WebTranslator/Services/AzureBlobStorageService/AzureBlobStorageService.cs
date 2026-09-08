using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using WebTranslator.Services.AIVisionService;
using WebTranslator.Services.Api;

namespace WebTranslator.Services.AzureBlobStorage
{
    public record FileInfo(
        string name,
        long size,
        string? type,
        DateTimeOffset lastModified);

    public record ContainerInfo(
        string name,
        int fileCount,
        long totalSize);

    public enum BlobSasPermission
    {
        R = 1,
        W,
        RW,
        RD
    }
    public enum ContainerSasPermission
    {
        L = 1
    }
    public class AzureBlobStorageService
    {
        private readonly IApiConfiguration<BlobStorageApiConfig> _apiConfiguration;
        private readonly ILogger<AzureBlobStorageService> _logger;
        private readonly BlobServiceClient _serviceClient;

        public AzureBlobStorageService(
            IApiConfiguration<BlobStorageApiConfig> apiConfiguration,
            ILogger<AzureBlobStorageService> logger)
        {
            _apiConfiguration = apiConfiguration;
            _logger = logger;

            var config = _apiConfiguration.GetConfig();
            if (config?.ConnectionString == null)
                throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

            _serviceClient = new BlobServiceClient(config.ConnectionString);
        }

        public BlobContainerClient GetContainer(string containerName)
        {
            return _serviceClient.GetBlobContainerClient(containerName);
        }
        private BlobClient GetBlob(
            string containerName,
            string blobName)
        {
            return GetExistingContainer(containerName).GetBlobClient(blobName);
        }

        private BlobContainerClient GetExistingContainer(string containerName)
        {
            var container = GetContainer(containerName);

            if (!container.Exists())
                throw new InvalidOperationException(
                    $"Container '{containerName}' does not exist.");

            return container;
        }
        private BlobClient GetExistingBlob(
            string containerName,
            string blobName)
        {
            var blob = GetBlob(containerName, blobName);

            if (!blob.Exists())
                throw new FileNotFoundException($"Blob '{blobName}' does not exist.");

            return blob;
        }
        private static BlobSasPermissions GetBlobSasPermissions(BlobSasPermission permission)
        {
            return permission switch
            {
                BlobSasPermission.R  => BlobSasPermissions.Read,
                BlobSasPermission.W  => BlobSasPermissions.Write,
                BlobSasPermission.RW => BlobSasPermissions.Read |
                           BlobSasPermissions.Write,
                BlobSasPermission.RD => BlobSasPermissions.Read |
                           BlobSasPermissions.Delete,

                _ => throw new InvalidOperationException(
                    $"Unsupported permission: {permission}.")
            };
        }
        private static BlobContainerSasPermissions GetBlobContainerSasPermissions(ContainerSasPermission permission)
        {
            return permission switch
            {
                ContainerSasPermission.L => BlobContainerSasPermissions.List,

                _ => throw new InvalidOperationException(
                    $"Unsupported permission: {permission}.")
            };
        }
        public bool ContainerExists(string containerName)
        {
            return GetContainer(containerName).Exists();
        }

        public void CreateContainer(string containerName)
        {
            GetContainer(containerName).CreateIfNotExists();
        }

        public void Upload(
            string containerName,
            string srcPath,
            string targetName,
            bool withOverwrite = false)
        {
            GetBlob(containerName, targetName)
                .Upload(srcPath, overwrite: withOverwrite);
        }

        public List<string> ListContainer(string containerName)
        {
            var container = GetExistingContainer(containerName);
            List<string> blobs = new();

            foreach (var blob in container.GetBlobs())
                blobs.Add(blob.Name);

            return blobs;
        }
        public static List<string> ListContainerFromSas(Uri uri)
        {
            var container = new BlobContainerClient(uri);
            List<string> blobs = new();

            foreach (var blob in container.GetBlobs())
                blobs.Add(blob.Name);

            return blobs;
        }
        public void LoadFrom(
            string containerName,
            string blobName,
            string targetPath)
        {
            GetExistingBlob(containerName, blobName)
                .DownloadTo(Path.Combine(targetPath, Path.GetFileName(blobName)));
        }

        public static void LoadFromSas(
            Uri uri,
            string targetName,
            string targetPath)
        {
            var blob = new BlobClient(uri);

            if (!blob.Exists())
                throw new FileNotFoundException(
                    $"File '{uri}' does not exist.");

            blob.DownloadTo(
                Path.Combine(targetPath, Path.GetFileName(targetName)));
        }
        public void Update(
            string containerName,
            string blobName,
            string srcPath)
        {
            GetExistingBlob(containerName, blobName)
                .Upload(srcPath, overwrite: true);
        }

        public FileInfo GetFileInfo(
            string containerName,
            string blobName)
        {
            var blob = GetExistingBlob(containerName, blobName);
            var properties = blob.GetProperties().Value;

            return new FileInfo(
                name: blob.Name,
                size: properties.ContentLength,
                type: properties.ContentType,
                lastModified: properties.LastModified);
        }

        public void Delete(
            string containerName,
            string blobName)
        {
            GetExistingBlob(containerName, blobName)
                .Delete();
        }

        public ContainerInfo GetContainerInfo(string containerName)
        {
            var container = GetExistingContainer(containerName);

            int count = 0;
            long totalSize = 0;

            foreach (var item in container.GetBlobs())
            {
                count++;
                totalSize += item.Properties.ContentLength ?? 0;
            }

            return new ContainerInfo(
                name: container.Name,
                fileCount: count,
                totalSize: totalSize);
        }
        public Uri CreateBlobSASURI(
            string containerName,
            string blobName,
            DateTimeOffset expirationDate,
            BlobSasPermission permission)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = expirationDate
            };

            sasBuilder.SetPermissions(GetBlobSasPermissions(permission));

            return GetExistingBlob(containerName, blobName)
                    .GenerateSasUri(sasBuilder);
        }
        public Uri CreateContainerSASURI(
            string containerName,
            DateTimeOffset expirationDate,
            ContainerSasPermission permission)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                Resource = "c",
                ExpiresOn = expirationDate
            };

            sasBuilder.SetPermissions(GetBlobContainerSasPermissions(permission));

            return GetExistingContainer(containerName).GenerateSasUri(sasBuilder);
        }
    }
}