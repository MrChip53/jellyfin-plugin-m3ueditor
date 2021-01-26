using Jellyfin.Plugin.M3UEditor.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.M3UEditor
{
    public class SaveTask : IScheduledTask
    {
        public string Name => "Save M3U Playlists";

        public string Key => "M3UEditorSave";

        public string Description => "Saves the M3U edits to files for the M3U Editor plugin. This task is auto queued after an edit is made.";

        public string Category => "Live TV";

        private readonly ILogger<object> _logger;

        public SaveTask(ILogger<object> logger)
        {
            _logger = logger;
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return SaveData(cancellationToken, progress);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return null;
        }

        private async Task SaveData(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.LogInformation("Save started.");

            foreach (var playlist in Plugin.Instance.M3UPlaylists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Save.SaveM3UChannels(Plugin.Instance.M3UChannels, playlist.PlaylistUrl);

                lock (Plugin.Instance.fileLock)
                {
                    Save.SaveM3UFile(playlist.PlaylistUrl, cancellationToken, progress);
                }
                
            }

            progress.Report(100);
        }
    }
}
