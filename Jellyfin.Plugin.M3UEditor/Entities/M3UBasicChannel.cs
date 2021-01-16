using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.Plugin.M3UEditor.Entities
{
    public class M3UBasicChannel
    {
        public int Id { get; set; }
        public bool Hidden { get; set; }
        public string Name { get; set; }
    }
}
