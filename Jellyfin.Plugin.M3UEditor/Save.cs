using Jellyfin.Plugin.M3UEditor.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Jellyfin.Plugin.M3UEditor
{
    public class Save
    {
        public static void SaveM3UPlaylists(List<M3UPlaylist> m3uLists)
        {
            using (StreamWriter writer = System.IO.File.CreateText(Plugin.Instance.DataPath + "m3u_playlists.json"))
            {
                writer.Write(JsonSerializer.Serialize(m3uLists));
            }
        }

        public static List<M3UPlaylist> LoadM3UPlaylists()
        {
            if (File.Exists(Plugin.Instance.DataPath + "m3u_playlists.json"))
                return JsonSerializer.Deserialize<List<M3UPlaylist>>(File.ReadAllText(Plugin.Instance.DataPath + "m3u_playlists.json"));

            return new List<M3UPlaylist>();
        }

        public static void SaveM3UChannels(Dictionary<string, List<M3UItem>> m3uChannels, string key = null)
        {
            if (key != null)
            {
                using (StreamWriter writer = System.IO.File.CreateText(Plugin.Instance.DataPath + Convert.ToBase64String(Encoding.UTF8.GetBytes(key)) + ".json"))
                {
                    writer.Write(JsonSerializer.Serialize(m3uChannels[key]));
                }
            } else
            {
                foreach(var kvp in m3uChannels)
                {
                    using (StreamWriter writer = System.IO.File.CreateText(Plugin.Instance.DataPath + Convert.ToBase64String(Encoding.UTF8.GetBytes(kvp.Key)) + ".json"))
                    {
                        writer.Write(JsonSerializer.Serialize(kvp.Value));
                    }
                }
            }
        }

        public static Dictionary<string, List<M3UItem>> LoadM3UChannels(List<M3UPlaylist> lists)
        {
            Dictionary<string, List<M3UItem>> channelList = new Dictionary<string, List<M3UItem>>();
            foreach (var list in lists)
            {
                if (File.Exists(Plugin.Instance.DataPath + Convert.ToBase64String(Encoding.UTF8.GetBytes(list.PlaylistUrl)) + ".json"))
                {
                    List<M3UItem> channels = JsonSerializer.Deserialize<List<M3UItem>>(File.ReadAllText(Plugin.Instance.DataPath + Convert.ToBase64String(Encoding.UTF8.GetBytes(list.PlaylistUrl)) + ".json"));
                    channelList.Add(list.PlaylistUrl, channels);
                }
            }
            return channelList;
        }
    }
}
