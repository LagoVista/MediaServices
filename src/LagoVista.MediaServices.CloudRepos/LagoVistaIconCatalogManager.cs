using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.MediaServices.Interfaces;
using LagoVista.MediaServices.Models.Icons;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.MediaServices.CloudRepos
{
    public class LagoVistaIconCatalogManager : ILagoVistaIconCatalogManager
    {
        private const string PublicIconContainerName = "lagovistaicons";

        private readonly IMediaServicesConnectionSettings _settings;
        private readonly IAdminLogger _adminLogger;

        public LagoVistaIconCatalogManager(IMediaServicesConnectionSettings settings, IAdminLogger adminLogger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _adminLogger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
        }

        public async Task<InvokeResult<LagoVistaIconMasterCatalogDocument>> GetMasterCatalogAsync(string orgNamespace)
        {
            if (String.IsNullOrWhiteSpace(orgNamespace))
                return InvokeResult<LagoVistaIconMasterCatalogDocument>.FromError("Organization namespace is required.");

            var normalizedOrgNamespace = NormalizePathSegment(orgNamespace);
            var catalogPath = $"{normalizedOrgNamespace}/catalog/master.json";

            return await ReadCatalogAsync<LagoVistaIconMasterCatalogDocument>(catalogPath);
        }

        public async Task<InvokeResult<LagoVistaIconCatalogDocument>> GetFamilyCatalogAsync(string orgNamespace, string familyKey)
        {
            if (String.IsNullOrWhiteSpace(orgNamespace))
                return InvokeResult<LagoVistaIconCatalogDocument>.FromError("Organization namespace is required.");

            if (String.IsNullOrWhiteSpace(familyKey))
                return InvokeResult<LagoVistaIconCatalogDocument>.FromError("Icon family key is required.");

            var normalizedOrgNamespace = NormalizePathSegment(orgNamespace);
            var normalizedFamilyKey = NormalizePathSegment(familyKey);
            var catalogPath = $"{normalizedOrgNamespace}/catalog/{normalizedFamilyKey}.json";

            return await ReadCatalogAsync<LagoVistaIconCatalogDocument>(catalogPath);
        }

        private async Task<InvokeResult<TCatalog>> ReadCatalogAsync<TCatalog>(string catalogPath) where TCatalog : class
        {
            try
            {
                var storage = CreateStorage();
                var catalogResult = await storage.GetFileAsync(PublicIconContainerName, catalogPath);

                if (!catalogResult.Successful)
                    return catalogResult.ToInvokeResult<TCatalog>();

                if (catalogResult.Result == null || catalogResult.Result.Length == 0)
                    return InvokeResult<TCatalog>.FromError($"Icon catalog was not found at '{catalogPath}'.");

                var json = Encoding.UTF8.GetString(catalogResult.Result);
                var catalog = JsonConvert.DeserializeObject<TCatalog>(json);

                if (catalog == null)
                    return InvokeResult<TCatalog>.FromError($"Icon catalog at '{catalogPath}' could not be read.");

                return InvokeResult<TCatalog>.Create(catalog);
            }
            catch (Exception ex)
            {
                return InvokeResult<TCatalog>.FromException($"Could not read icon catalog '{catalogPath}'.", ex);
            }
        }

        private CloudFileStorage CreateStorage()
        {
            if (_settings.IconStorageConnection == null)
                throw new InvalidOperationException("Media storage connection is required.");

            if (String.IsNullOrWhiteSpace(_settings.IconStorageConnection.AccountId))
                throw new InvalidOperationException("Media storage account id is required.");

            if (String.IsNullOrWhiteSpace(_settings.IconStorageConnection.AccessKey))
                throw new InvalidOperationException("Media storage access key is required.");

            return new CloudFileStorage(_settings.IconStorageConnection.AccountId, _settings.IconStorageConnection.AccessKey, _adminLogger);
        }

        private static string NormalizePathSegment(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return null;

            var builder = new StringBuilder();
            foreach (var character in value.Trim().ToLowerInvariant())
            {
                if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9'))
                {
                    builder.Append(character);
                }
                else if (character == '-' || character == '_')
                {
                    builder.Append(character);
                }
                else if (character == ' ' || character == '.' || character == ':')
                {
                    builder.Append('-');
                }
            }

            var normalized = builder.ToString().Trim('-');
            while (normalized.Contains("--"))
                normalized = normalized.Replace("--", "-");

            return normalized;
        }
    }
}
