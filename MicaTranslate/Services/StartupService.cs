using MicaTranslate.Core.Interfaces.Services;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace MicaTranslate.Services;

public class StartupService : IStartupService
{
    private const string TaskId = "MicaTranslateStartup";

    public async Task<bool> IsEnabledAsync()
    {
        var task = await StartupTask.GetAsync(TaskId);

        return task.State is
            StartupTaskState.Enabled or
            StartupTaskState.EnabledByPolicy;
    }

    public async Task<bool> SetEnabledAsync(bool enabled)
    {
        var task = await StartupTask.GetAsync(TaskId);

        if (enabled)
        {
            var state = await task.RequestEnableAsync();

            return state is
                StartupTaskState.Enabled or
                StartupTaskState.EnabledByPolicy;
        }

        task.Disable();

        return true;
    }
}
