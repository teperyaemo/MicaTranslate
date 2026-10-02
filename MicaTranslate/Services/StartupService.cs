using MicaTranslate.Core.Interfaces.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace MicaTranslate.Services;

public class StartupService : IStartupService
{
    private const string TaskId = "MicaTranslateStartup";
    private const string RegistryKeyName = "MicaTranslate";

    private static bool IsAppPackaged()
    {
        try
        {
            return !string.IsNullOrEmpty(Package.Current.Id.Name);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsEnabledAsync()
    {
        if (IsAppPackaged())
        {
            try
            {
                var task = await StartupTask.GetAsync(TaskId);
                return task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            }
            catch
            {
                return false;
            }
        }
        else
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            var value = key?.GetValue(RegistryKeyName) as string;
            return !string.IsNullOrEmpty(value) && value.Equals(GetExecutablePath(), StringComparison.OrdinalIgnoreCase);
        }
    }

    public async Task<bool> SetEnabledAsync(bool enabled)
    {
        if (IsAppPackaged())
        {
            try
            {
                var task = await StartupTask.GetAsync(TaskId);
                if (enabled)
                {
                    var state = await task.RequestEnableAsync();
                    return state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
                }
                else
                {
                    task.Disable();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        else
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);

                if (key == null) return false;

                if (enabled)
                {
                    key.SetValue(RegistryKeyName, GetExecutablePath());
                }
                else
                {
                    if (key.GetValue(RegistryKeyName) != null)
                    {
                        key.DeleteValue(RegistryKeyName);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private static string GetExecutablePath()
    {
        return Environment.ProcessPath!;
    }
}
