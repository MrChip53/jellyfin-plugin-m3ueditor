using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.Plugin.M3UEditor.Entities
{
    class ErrorResponse
    {
        public string ErrorMsg { get; set; }
        public int ErrorCode { get; set; }
    }
}
