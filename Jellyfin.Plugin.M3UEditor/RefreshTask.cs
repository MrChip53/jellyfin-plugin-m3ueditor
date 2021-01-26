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
    public class RefreshTask : IScheduledTask
    {
        public string Name => "Refresh M3U Playlists";

        public string Key => "M3UEditor";

        public string Description => "Refreshes and reapplies the M3U edits for the M3U Editor plugin.";

        public string Category => "Live TV";

        private readonly ILogger<object> _logger;

        public RefreshTask(ILogger<object> logger)
        {
            _logger = logger;
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return RefreshData(cancellationToken, progress);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks }
            };
        }

        private async Task RefreshData(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.LogInformation("Refresh started.");
            var totalChannels = 0;
            var processedChannels = 0;

            Dictionary<string, string> files = new Dictionary<string, string>();

            foreach (var playlist in Plugin.Instance.M3UPlaylists)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        if (playlist.UserAgent != null && playlist.UserAgent.Length > 0)
                            client.Headers.Add(HttpRequestHeader.UserAgent, playlist.UserAgent);
                        string m3u = client.DownloadString(playlist.PlaylistUrl);
                        string[] lines = m3u.Split('\n');
                        totalChannels += (lines.Length - 1) / 2;
                        files.Add(playlist.PlaylistUrl, m3u);
                    }
                }
                catch (WebException ex)
                {
                    _logger.LogError("Error downloading M3U file\n" + ex.Message);
                    files.Add(playlist.PlaylistUrl, "");
                    continue;
                }
            } 
            foreach (var playlist in Plugin.Instance.M3UPlaylists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress.Report(Plugin.Instance.M3UPlaylists.IndexOf(playlist) / Plugin.Instance.M3UPlaylists.Count);

                string m3uFile = files[playlist.PlaylistUrl];

                if (m3uFile == null || m3uFile.Length < 1)
                    continue;

                var newItems = Helper.ParseM3U(m3uFile);


                if (!Plugin.Instance.M3UChannels.ContainsKey(playlist.PlaylistUrl))
                    Plugin.Instance.M3UChannels.Add(playlist.PlaylistUrl, new List<M3UItem>());

                var channels = Plugin.Instance.M3UChannels[playlist.PlaylistUrl];

                for (int i = channels.Count - 1; i >= 0; i--)
                {
                    if (channels[i].b64 == null)
                    {
                        channels.RemoveAt(i);
                        continue;
                    }

                    for(int j = newItems.Count - 1; j >= 0; j--)
                    {
                        if (channels[i].b64.Equals(newItems[j].b64))
                        {
                            channels[i].Url = newItems[j].Url;
                            newItems.RemoveAt(j);
                            break;
                        }
                        processedChannels += 1;
                        var percent = processedChannels / totalChannels;
                        if (percent > 99)
                            percent = 99;
                        progress.Report(percent);
                    }
                }

                if (newItems.Count > 0)
                    channels.AddRange(newItems);

                Plugin.Instance.M3UChannels[playlist.PlaylistUrl] = channels;
                
            }

            Plugin.Instance.TaskManager.CancelIfRunningAndQueue<SaveTask>();

            progress.Report(100);
        }
    }
}
