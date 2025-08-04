using System.IO.Compression;
using System.Security.Authentication;
using Google.Apis.Auth.OAuth2;

namespace LibtraceableDownloader;

class Program
{
  struct DownloadRequest
  {
    public string ArtifactName { get; set; }
    public string RuntimeIdentifier { get; set; }
    public string Version { get; set; }
    public string Token { get; set; }
  }

  static async Task Main(string[] args)
  {
    using var loggerFactory = LoggerFactory.Create(builder => { builder.AddSimpleConsole(options => { options.SingleLine = true; options.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; }); });
    ILogger logger = loggerFactory.CreateLogger<Program>();

    logger.LogInformation("Downloading libtraceable modules...");

    string libtraceableVersion = "0.1.98-rc.249";
    string token = await GetAuthToken();
    await DownloadAndSaveFile(
      new DownloadRequest
      {
        ArtifactName = "libtraceable_rhel_7_x86_64",
        RuntimeIdentifier = "linux-x64",
        Version = libtraceableVersion,
        Token = token
      }, logger);
    await DownloadAndSaveFile(
    new DownloadRequest
    {
      ArtifactName = "libtraceable_rhel_7_aarch64",
      RuntimeIdentifier = "linux-arm64",
      Version = libtraceableVersion,
      Token = token
    }, logger);

    logger.LogInformation("Modules downloaded successfully.");
  }

  private static async Task DownloadAndSaveFile(DownloadRequest request, ILogger logger)
  {
    logger.LogInformation($"Downloading {request.ArtifactName} for {request.RuntimeIdentifier}");

    string url = $"https://artifactregistry.googleapis.com/download/v1/projects/traceable-actions-386017/locations/us/repositories/packages/files/libtraceable:{request.Version}:{request.ArtifactName}-{request.Version}.zip:download?alt=media";
    string savePath = $"{Directory.GetCurrentDirectory()}/{request.RuntimeIdentifier}/";
    string filePath = $"{Directory.GetCurrentDirectory()}/{request.RuntimeIdentifier}/libtraceable.zip";
    CreateDirectoryIfNotExisting(savePath);
    await DownloadFileAsync(url, filePath, request.Token, logger);
    ZipFile.ExtractToDirectory(filePath, savePath, overwriteFiles: true);
    Cleanup(savePath, logger);
  }

  static async Task DownloadFileAsync(string url, string savePath, string token, ILogger logger)
  {
    using HttpClient client = new();
    if (string.IsNullOrWhiteSpace(token))
    {
      throw new InvalidCredentialException("Artifactory credentials not found");
    }
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    try
    {
      using HttpResponseMessage response = await client.GetAsync(url);
      using HttpContent content = response.Content;
      if (response.IsSuccessStatusCode)
      {
        using Stream fileStream = System.IO.File.Create(savePath);
        await content.CopyToAsync(fileStream);
      }
      else
      {
        logger.LogCritical(
            $"Failed to download file. HTTP Status Code: {response.StatusCode}"
        );
      }
    }
    catch (Exception ex)
    {
      logger.LogCritical($"An error occurred: {ex.Message}");
    }
  }

  private static void Cleanup(string directory, ILogger logger)
  {
    try
    {
      if (Directory.Exists(directory))
      {
        string[] files = Directory.GetFiles(directory);

        foreach (string file in files)
        {
          if (Path.GetFileName(file) != "libtraceable.so")
          {
            System.IO.File.Delete(file);
          }
        }
        logger.LogInformation("Files cleaned up successfully.");
      }
      else
      {
        logger.LogInformation("Directory does not exist.");
      }
    }
    catch (Exception ex)
    {
      logger.LogError($"An error occurred: {ex.Message}");
    }
  }

  private static void CreateDirectoryIfNotExisting(string path)
  {
    if (!Directory.Exists(path))
    {
      Directory.CreateDirectory(path);
    }
  }

  private static async Task<string> GetAuthToken()
  {
    GoogleCredential credential = await GoogleCredential.GetApplicationDefaultAsync();

    if (IsRunningInGitHubActions())
    {
        var builder = new ImpersonatedCredential.Initializer("artifact-registry-reader@traceable-actions-386017.iam.gserviceaccount.com")
        {
          Scopes = new[] { "https://www.googleapis.com/auth/cloud-platform" },
          Lifetime = TimeSpan.FromSeconds(300)
        };

        credential = credential.Impersonate(builder);
    } else if (credential.IsCreateScopedRequired)
    {
      credential = credential.CreateScoped(new[]
      {
            "https://www.googleapis.com/auth/cloud-platform"
        });
    }

    return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
  }
    
    private static bool IsRunningInGitHubActions()
{
    // GitHub Actions sets the GITHUB_ACTIONS environment variable to "true"
    string githubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
    
    return !string.IsNullOrEmpty(githubActions) && githubActions.Equals("true", StringComparison.OrdinalIgnoreCase);
}
}
