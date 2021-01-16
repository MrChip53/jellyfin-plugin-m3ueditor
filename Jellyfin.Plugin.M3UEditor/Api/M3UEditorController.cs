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

        public class ChannelId
        {
            public string PlaylistUrl { get; set; }
            public int M3UChannelId { get; set; }
        }

        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("GetChannel")]
        public ActionResult<String> GetChannelsRequest([FromBody] ChannelId channel)
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
            _logger.LogInformation(JsonSerializer.Serialize(m3UPlaylist[channel.M3UChannelId]));
            return Ok(JsonSerializer.Serialize(m3UPlaylist[channel.M3UChannelId]));
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

            return Ok(JsonSerializer.Serialize(basicChannels));
        }

        public class AddPlaylist
        {
            public string PlaylistName { get; set; }
            public string PlaylistUrl { get; set; }
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
                    return Ok(JsonSerializer.Serialize(Plugin.Instance.M3UPlaylists));
                }
            }
            Plugin.Instance.M3UPlaylists.Add(new M3UPlaylist()
            {
                PlaylistName = newPlaylist.PlaylistName,
                PlaylistUrl = newPlaylist.PlaylistUrl
            });

            string m3uFile;
            using (WebClient client = new WebClient())
            {
                m3uFile = client.DownloadString(newPlaylist.PlaylistUrl);
            }

            String[] m3uSplit = m3uFile.Split('\n');

            List<M3UItem> items = new List<M3UItem>();

            for (int i = 0; i < m3uSplit.Length; i++)
            {
                if (m3uSplit[i].Length == 0 || m3uSplit[i].Equals("#EXTM3U"))
                    continue;

                if (m3uSplit[i].StartsWith("#EXTINF"))
                {
                    //Parse info
                    M3UItem newItem = new M3UItem();
                    newItem.Attributes = new Dictionary<string, string>();
                    string[] m3uattrs = Regex.Split(m3uSplit[i], "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    newItem.ExtInf = m3uattrs[0];
                    foreach (string s in m3uattrs)
                    {
                        if (s.Contains("=") && s.Contains("\""))
                        {
                            Match mc = Regex.Match(s, "(.*?)=\"(.*?)\"");
                            if (mc.Groups.Count == 3)
                            {
                                newItem.Attributes.Add(mc.Groups[1].Value, mc.Groups[2].Value);
                            }
                        }
                    }
                    string[] namesplit = m3uSplit[i].Split("\",");
                    newItem.Name = namesplit[1].TrimStart();
                    newItem.Url = m3uSplit[i + 1];
                    items.Add(newItem);
                }
            }
            Plugin.Instance.M3UChannels.Add(newPlaylist.PlaylistUrl, items);

            Save.SaveM3UChannels(Plugin.Instance.M3UChannels, newPlaylist.PlaylistUrl);

            Save.SaveM3UPlaylists(Plugin.Instance.M3UPlaylists);

            return Ok(JsonSerializer.Serialize(Plugin.Instance.M3UPlaylists));
        }

        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("GetPlaylists")]
        public ActionResult<String> GetPlaylistRequest()
        {
            return Ok(JsonSerializer.Serialize(Plugin.Instance.M3UPlaylists));
        }

        public class M3UList
        {
            public string Id { get; set; }
        }

        [HttpGet("GetM3UList")]
        [Produces(MediaTypeNames.Text.Plain)]
        public ActionResult<String> GetM3UListRequest([FromQuery] M3UList list)
        {
            List<M3UItem> channels = Plugin.Instance.M3UChannels[list.Id];

            string Outfile = "#EXTM3U\n";

            foreach (M3UItem item in channels)
            {
                if (item.hidden)
                    continue;
                Outfile += item.ExtInf + " ";
                foreach(var kvp in item.Attributes)
                {
                    Outfile += kvp.Key + "=\"" + kvp.Value + "\" ";
                }
                Outfile += ", " + item.Name + "\n" + item.Url + "\n";
            }

            return Ok(Outfile);
        }
    }
}
