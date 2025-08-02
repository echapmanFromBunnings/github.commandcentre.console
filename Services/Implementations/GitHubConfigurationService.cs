using GitHubWorkflowManager.Models;
using GitHubWorkflowManager.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace GitHubWorkflowManager.Services.Implementations;

public class GitHubConfigurationService : IGitHubConfigurationService
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<GitHubConfigurationService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _configFilePath;
    private readonly IDataProtector _protector;
    private const string ConfigCacheKey = "github_configuration";

    public GitHubConfigurationService(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<GitHubConfigurationService> logger,
        IMemoryCache cache,
        IWebHostEnvironment environment)
    {
        _dataProtectionProvider = dataProtectionProvider;
        _logger = logger;
        _cache = cache;
        _configFilePath = Path.Combine(environment.ContentRootPath, "App_Data", "github-config.json");
        _protector = _dataProtectionProvider.CreateProtector("GitHubConfiguration");
        
        // Ensure the App_Data directory exists
        var appDataDir = Path.GetDirectoryName(_configFilePath);
        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir!);
        }
    }

    public async Task<GitHubConfiguration?> GetConfigurationAsync()
    {
        // Check cache first
        if (_cache.TryGetValue(ConfigCacheKey, out GitHubConfiguration? cachedConfig))
        {
            _logger.LogInformation("Returning cached configuration");
            return cachedConfig;
        }

        try
        {
            _logger.LogInformation("Attempting to read configuration from: {ConfigFilePath}", _configFilePath);
            
            if (!File.Exists(_configFilePath))
            {
                _logger.LogInformation("Configuration file does not exist");
                return null;
            }

            var encryptedContent = await File.ReadAllTextAsync(_configFilePath);
            if (string.IsNullOrEmpty(encryptedContent))
            {
                _logger.LogWarning("Configuration file exists but is empty");
                return null;
            }

            _logger.LogInformation("Configuration file found, attempting to decrypt...");
            var decryptedContent = _protector.Unprotect(encryptedContent);
            var configuration = JsonSerializer.Deserialize<GitHubConfiguration>(decryptedContent);
            
            if (configuration != null)
            {
                // Cache for 10 minutes
                _cache.Set(ConfigCacheKey, configuration, TimeSpan.FromMinutes(10));
                _logger.LogInformation("Configuration successfully loaded, decrypted, and cached");
            }
            
            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading GitHub configuration");
            return null;
        }
    }

    public async Task SaveConfigurationAsync(GitHubConfiguration configuration)
    {
        try
        {
            _logger.LogInformation("Saving configuration to: {ConfigFilePath}", _configFilePath);
            _logger.LogInformation("Configuration details - Owner: {Owner}, Repo: {Repo}, Token length: {TokenLength}", 
                configuration.RepositoryOwner, configuration.RepositoryName, configuration.AccessToken?.Length ?? 0);

            var jsonContent = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var encryptedContent = _protector.Protect(jsonContent);
            await File.WriteAllTextAsync(_configFilePath, encryptedContent);
            
            // Update cache
            _cache.Set(ConfigCacheKey, configuration, TimeSpan.FromMinutes(10));
            
            _logger.LogInformation("GitHub configuration saved successfully to {ConfigFilePath}", _configFilePath);
            
            // Verify the file was actually written
            if (File.Exists(_configFilePath))
            {
                var fileInfo = new FileInfo(_configFilePath);
                _logger.LogInformation("Configuration file verified - Size: {FileSize} bytes", fileInfo.Length);
            }
            else
            {
                _logger.LogError("Configuration file was not created after save operation");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving GitHub configuration");
            throw;
        }
    }

    public Task<bool> ValidateConfigurationAsync(GitHubConfiguration configuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuration.AccessToken) ||
                string.IsNullOrWhiteSpace(configuration.RepositoryOwner) ||
                string.IsNullOrWhiteSpace(configuration.RepositoryName))
            {
                return Task.FromResult(false);
            }

            // Additional validation can be added here
            // For now, just check if the token format looks valid
            var isValid = configuration.AccessToken.StartsWith("ghp_") || 
                         configuration.AccessToken.StartsWith("github_pat_") ||
                         configuration.AccessToken.StartsWith("gho_") ||
                         configuration.AccessToken.StartsWith("ghu_") ||
                         configuration.AccessToken.StartsWith("ghs_") ||
                         configuration.AccessToken.StartsWith("ghr_");
            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating GitHub configuration");
            return Task.FromResult(false);
        }
    }

    public Task ClearConfigurationAsync()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
                _cache.Remove(ConfigCacheKey); // Clear cache
                _logger.LogInformation("GitHub configuration cleared successfully");
            }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing GitHub configuration");
            throw;
        }
    }

    public async Task<bool> IsConfiguredAsync()
    {
        try
        {
            _logger.LogInformation("Checking configuration at path: {ConfigFilePath}", _configFilePath);
            
            var config = await GetConfigurationAsync();
            var isConfigured = config?.IsConfigured ?? false;
            
            _logger.LogInformation("Configuration check result: {IsConfigured}, Config exists: {ConfigExists}", 
                isConfigured, config != null);
                
            if (config != null)
            {
                _logger.LogInformation("Config details - Owner: {Owner}, Repo: {Repo}, Token length: {TokenLength}", 
                    config.RepositoryOwner, config.RepositoryName, config.AccessToken?.Length ?? 0);
            }
            
            return isConfigured;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if configured");
            return false;
        }
    }
}
