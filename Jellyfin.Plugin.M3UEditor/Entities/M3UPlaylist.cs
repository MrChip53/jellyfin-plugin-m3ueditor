using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.Plugin.M3UEditor.Entities
{
    public class M3UPlaylist
    {
        public string PlaylistName { get; set; }
        public string PlaylistUrl { get; set; }
        public string UserAgent { get; set; }
    }
}
