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

namespace Jellyfin.Plugin.M3UEditor
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "M3U Editor";
        
        public override Guid Id => Guid.Parse("69384327-d223-447a-ae23-47767eed1749");

        public String DataPath;

        public List<M3UPlaylist> M3UPlaylists = new List<M3UPlaylist>();
        public Dictionary<string, List<M3UItem>> M3UChannels = new Dictionary<string, List<M3UItem>>();

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            DataPath = applicationPaths.DataPath + System.IO.Path.DirectorySeparatorChar + "m3u_editor" + System.IO.Path.DirectorySeparatorChar;

            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }
            M3UPlaylists = Save.LoadM3UPlaylists();
            M3UChannels = Save.LoadM3UChannels(M3UPlaylists);
        }

        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "m3u_editor_main",
                    EmbeddedResourcePath = string.Format("{0}.Configuration.configPage.html", GetType().Namespace),
                    EnableInMainMenu = true,
                    DisplayName = "M3U Editor"
                }
            };
        }
    }
}