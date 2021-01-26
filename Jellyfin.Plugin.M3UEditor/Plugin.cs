using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Jellyfin.Plugin.M3UEditor.Configuration;
using Jellyfin.Plugin.M3UEditor.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.M3UEditor
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "M3U Editor";
        
        public override Guid Id => Guid.Parse("69384327-d223-447a-ae23-47767eed1749");

        public readonly ITaskManager TaskManager;

        public String DataPath;

        public List<M3UPlaylist> M3UPlaylists = new List<M3UPlaylist>();
        public Dictionary<string, List<M3UItem>> M3UChannels = new Dictionary<string, List<M3UItem>>();
        public readonly object fileLock = new object();

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ITaskManager taskManager) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            DataPath = applicationPaths.DataPath + System.IO.Path.DirectorySeparatorChar + "m3u_editor" + System.IO.Path.DirectorySeparatorChar;

            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }
            M3UPlaylists = Save.LoadM3UPlaylists();
            M3UChannels = Save.LoadM3UChannels(M3UPlaylists);

            TaskManager = taskManager;
        }

        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "m3u_main",
                    EmbeddedResourcePath = string.Format("{0}.Configuration.Web.m3u_main.html", GetType().Namespace),
                    EnableInMainMenu = true,
                    DisplayName = "M3U Editor",
                    MenuIcon = "tv"
                },
                new PluginPageInfo
                {
                    Name = "m3u_main.js",
                    EmbeddedResourcePath = string.Format("{0}.Configuration.Web.m3u_main.js", GetType().Namespace)
                },
                new PluginPageInfo
                {
                    Name = "m3u_playlists",
                    EmbeddedResourcePath = string.Format("{0}.Configuration.Web.m3u_playlists.html", GetType().Namespace)
                },
                new PluginPageInfo
                {
                    Name = "m3u_playlists.js",
                    EmbeddedResourcePath = string.Format("{0}.Configuration.Web.m3u_playlists.js", GetType().Namespace)
                },
                new PluginPageInfo
                {
                    Name = "m3u_channels",
                    EmbeddedResourcePath = string.Format("{0}.Configuration.Web.m3u_channels.html", GetType().Namespace)
                },
                new PluginPageInfo
                {
                    Name = "m3u_channels.js",
                    EmbeddedResourcePath = string.Format("{0}.Configuration.Web.m3u_channels.js", GetType().Namespace)
                }
            };
        }
    }
}