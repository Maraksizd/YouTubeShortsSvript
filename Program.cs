using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

class YouTubeShortsUploader
{
    private static readonly List<string> VideoExtensions = new() { ".mp4", ".mov", ".avi", ".mkv" };
    private readonly string _videosFolder;
    private readonly YouTubeService _youtubeService;

    public YouTubeShortsUploader(string clientSecretsFile, string videosFolder)
    {
        _videosFolder = videosFolder;
        _youtubeService = Authenticate(clientSecretsFile).Result;
    }

    private async Task<YouTubeService> Authenticate(string clientSecretsFile)
    {
        using var stream = new FileStream(clientSecretsFile, FileMode.Open, FileAccess.Read);
        var credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.Load(stream).Secrets,
            new[] { YouTubeService.Scope.YoutubeUpload },
            "user",
            CancellationToken.None
        );

        return new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credentials,
            ApplicationName = "YouTube Shorts Uploader"
        });
    }

    private (string title, string description) GetVideoDetails(string videoPath)
    {
        string title = Path.GetFileNameWithoutExtension(videoPath);
        string description = "Uploaded";
        return (title, description);
    }

    public async Task<string?> UploadShort(string videoPath)
    {
        try
        {
            var (title, description) = GetVideoDetails(videoPath); // додавання заголовку і опису
            var video = new Video
            {
                Snippet = new VideoSnippet { Title = title, Description = description, CategoryId = "22" }, // You can change the category ID
                Status = new VideoStatus { PrivacyStatus = "public" }
            };

            using var fileStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read);
            var videosInsertRequest = _youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
            var response = await videosInsertRequest.UploadAsync();

            if (response.Status == UploadStatus.Completed)
            {
                Console.WriteLine($"Видео {title} успешно загружено. ID: https://www.youtube.com/shorts/{videosInsertRequest.ResponseBody.Id}");
                return videosInsertRequest.ResponseBody.Id;
            }
            else
            {
                Console.WriteLine($"Ошибка загрузки {videoPath}: {response.Exception.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке видео {videoPath}: {ex.Message}");
            return null;
        }
    }

    public async Task UploadAllShorts()
    {
        var videoFiles = Directory.GetFiles(_videosFolder)
            .Where(f => VideoExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));

        foreach (var videoPath in videoFiles)
        {
            await UploadShort(videoPath);
        }
    }
}

class Program
{
    static async Task Main()
    {
        string clientSecretsFile = FindCredentialsFile();

        string videosFolder = "D:\\VideoForShorts";

        var uploader = new YouTubeShortsUploader(clientSecretsFile, videosFolder);
        await uploader.UploadAllShorts();
    }

    static string FindCredentialsFile()
    {
        string credentialsFile = "D:\\Programing\\Projects\\c#\\ScriptYTshorts2.0\\client_secret_144541564557-dheneeebl984o737f7bdft9lmu2jbj1j.apps.googleusercontent.com.json"; // Вкажи точний шлях до файлу
        if (File.Exists(credentialsFile))
        {
            return credentialsFile;
        }
        else
        {
            throw new FileNotFoundException("Не найден JSON файл с учетными данными OAuth");
        }
    }

}
