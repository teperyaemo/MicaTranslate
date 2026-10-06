using MicaTranslate.Core.Interfaces.Services;
using MicaTranslate.Core.Models;
using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Windows.Storage;

namespace MicaTranslate.Services;

internal class SettingsService : ISettingsService
{
    private readonly IStartupService _startupService;

    private readonly string _folderPath;
    private readonly string _filePath;

    private readonly JsonSerializerOptions options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.Cyrillic, UnicodeRanges.BasicLatin),
        WriteIndented = true
    };

    public AppSettings Settings { get; private set; } = new();

    public SettingsService(IStartupService startupService)
    {
        _startupService = startupService;

        _folderPath = GetAppDataFolder();
        _filePath = Path.Combine(_folderPath, "settings.json");
    }

    public async Task LoadAsync()
    {
        if (!Directory.Exists(_folderPath))
            Directory.CreateDirectory(_folderPath);

        if (!File.Exists(_filePath))
        {
            await SaveAsync();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json);

            if (loaded != null)
                Settings = loaded;

            Settings.RunAtStartup = await _startupService.IsEnabledAsync();
        }
        catch
        {
            //ignored
        }
    }

    public async Task SaveAsync()
    {
        if (!Directory.Exists(_folderPath))
            Directory.CreateDirectory(_folderPath);

        var json = JsonSerializer.Serialize(Settings, options);

        await File.WriteAllTextAsync(_filePath, json);
    }

    private static string GetAppDataFolder()
    {
        try
        {
            return ApplicationData.Current.LocalFolder.Path;
        }
        catch
        {
            string appName = "MicaTranslate";
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataPath, appName);

            Directory.CreateDirectory(appFolder);
            return appFolder;
        }
    }
}