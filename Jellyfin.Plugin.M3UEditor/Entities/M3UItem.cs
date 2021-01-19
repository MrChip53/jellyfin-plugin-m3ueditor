using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.Plugin.M3UEditor.Entities
{
    public class M3UItem
    {
        public string b64 { get; set; }
        public string Url { get; set; }
        public string ExtInf { get; set; }
        public string Name { get; set; }
        public bool hidden { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        
    }
}
