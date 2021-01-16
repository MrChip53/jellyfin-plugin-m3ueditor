using MediaBrowser.Model.Plugins;
using System;

namespace Jellyfin.Plugin.M3UEditor.Configuration
{
    public enum SomeOptions
    {
        OneOption,
        AnotherOption
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        public string API_KEY { get; set; }

        public PluginConfiguration()
        {
            API_KEY = Guid.NewGuid().ToString();
        }
    }
}