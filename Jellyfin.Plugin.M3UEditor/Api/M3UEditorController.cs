using System;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Devices;

using System.Net.Mime;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;
using System.Collections.Generic;
using System.Text.Json;
using Jellyfin.Plugin.M3UEditor.Entities;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace Jellyfin.Plugin.M3UEditor.Api
{
    /// <summary>
    /// The Merge Versions api controller.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class M3UEditorController : ControllerBase
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<Object> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="TMDbBoxSetsController"/>.

        public M3UEditorController(ILibraryManager libraryManager,
            ILogger<Object> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            logger.LogInformation(Plugin.Instance.DataPath);
        }

        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("GetApiKey")]
        public ActionResult<String> GetApiKeyRequest()
        {
            Dictionary<string, string> key = new Dictionary<string, string>();
            key.Add("key", Plugin.Instance.Configuration.API_KEY);

            return Ok(key);
        }

        public class ChannelId
        {
            public string PlaylistUrl { get; set; }
            public int M3UChannelId { get; set; }
        }

        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("GetChannel")]
        public ActionResult<String> GetChannelRequest([FromBody] ChannelId channel)
        {
            List<M3UItem> m3UPlaylist = new List<M3UItem>();
            if (Plugin.Instance.M3UChannels.ContainsKey(channel.PlaylistUrl))
            {
                m3UPlaylist = Plugin.Instance.M3UChannels[channel.PlaylistUrl];
            }
            else
            {
                return NotFound();
            }
            return Ok(m3UPlaylist[channel.M3UChannelId]);
        }

        public class SaveChannel
        {
            public M3UItem channel { get; set; }
            public string PlaylistUrl { get; set; }
        }

        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("SaveChannel")]
        public ActionResult SaveChannelRequest([FromBody] SaveChannel channel)
        {
            List<M3UItem> m3UPlaylist = new List<M3UItem>();
            if (Plugin.Instance.M3UChannels.ContainsKey(channel.PlaylistUrl))
            {
                m3UPlaylist = Plugin.Instance.M3UChannels[channel.PlaylistUrl];
            }
            else
            {
                return NotFound();
            }

            for (int i = 0; i < m3UPlaylist.Count; i++)
            {
                if (m3UPlaylist[i].Url.Equals(channel.channel.Url))
                    m3UPlaylist[i] = channel.channel;
            }

            Plugin.Instance.M3UChannels[channel.PlaylistUrl] = m3UPlaylist;

            Save.SaveM3UChannels(Plugin.Instance.M3UChannels, channel.PlaylistUrl);

            return NoContent();
        }

        public class PlaylistId
        {
            public string PlaylistUrl { get; set; }
        }

        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("GetChannels")]
        public ActionResult<String> GetChannelsRequest([FromBody] PlaylistId playlist)
        {
            List<M3UItem> m3UPlaylist = new List<M3UItem>();
            if (Plugin.Instance.M3UChannels.ContainsKey(playlist.PlaylistUrl))
            {
                m3UPlaylist = Plugin.Instance.M3UChannels[playlist.PlaylistUrl];
            }
            else
            {
                return NotFound();
            }

            List<M3UBasicChannel> basicChannels = new List<M3UBasicChannel>();

            for (int i = 0; i < m3UPlaylist.Count; i++)
            {
                basicChannels.Add(new M3UBasicChannel()
                {
                    Id = i,
                    Name = m3UPlaylist[i].Name,
                    Hidden = m3UPlaylist[i].hidden
                });
            }

            return Ok(basicChannels);
        }

        public class AddPlaylist
        {
            public string PlaylistName { get; set; }
            public string PlaylistUrl { get; set; }
            public string UserAgent { get; set; }
        }

        /// <summary>
        /// Scans all movies and merges repeated ones.
        /// </summary>
        /// <reponse code="204">Library scan and merge started successfully. </response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("AddPlaylist")]
        public ActionResult<String> AddPlaylistRequest([FromBody] AddPlaylist newPlaylist)
        {
            foreach (M3UPlaylist item in Plugin.Instance.M3UPlaylists)
            {
                if (item.PlaylistUrl == newPlaylist.PlaylistUrl)
                {
                    return Ok(Plugin.Instance.M3UPlaylists);
                }
            }

            string m3uFile;

            try
            {
                using (WebClient client = new WebClient())
                {
                    if (newPlaylist.UserAgent.Length > 0)
                        client.Headers.Add(HttpRequestHeader.UserAgent, newPlaylist.UserAgent);
                    m3uFile = client.DownloadString(newPlaylist.PlaylistUrl);
                }
            }
            catch (WebException ex)
            {
                _logger.LogError("Error downloading M3U file\n" + ex.Message);
                return Ok(new ErrorResponse()
                {
                    ErrorMsg = "Error downloading M3U file.",
                    ErrorCode = -1
                });
            }

            Plugin.Instance.M3UPlaylists.Add(new M3UPlaylist()
            {
                PlaylistName = newPlaylist.PlaylistName,
                PlaylistUrl = newPlaylist.PlaylistUrl,
                UserAgent = newPlaylist.UserAgent
            });

            var items = Helper.ParseM3U(m3uFile);
            
            Plugin.Instance.M3UChannels.Add(newPlaylist.PlaylistUrl, items);

            Save.SaveM3UChannels(Plugin.Instance.M3UChannels, newPlaylist.PlaylistUrl);

            Save.SaveM3UPlaylists(Plugin.Instance.M3UPlaylists);

            return Ok(Plugin.Instance.M3UPlaylists);
        }

        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("GetPlaylists")]
        public ActionResult<String> GetPlaylistRequest()
        {
            return Ok(Plugin.Instance.M3UPlaylists);
        }

        public class M3UList
        {
            public string Id { get; set; }
            public string key { get; set; }
        }

        [HttpGet("GetM3UList")]
        [Produces(MediaTypeNames.Text.Plain)]
        public ActionResult<String> GetM3UListRequest([FromQuery] M3UList list)
        {
            if (!list.key.Equals(Plugin.Instance.Configuration.API_KEY))
                return Ok();

            List<M3UItem> channels = Plugin.Instance.M3UChannels[list.Id];

            string Outfile = "#EXTM3U\n";

            foreach (M3UItem item in channels)
            {
                if (item.hidden)
                    continue;
                Outfile += item.ExtInf;
                foreach(var kvp in item.Attributes)
                {
                    Outfile += " " + kvp.Key + "=\"" + kvp.Value + "\"";
                }
                Outfile += "," + item.Name + "\n" + item.Url + "\n";
            }

            return Ok(Outfile);
        }
    }
}
