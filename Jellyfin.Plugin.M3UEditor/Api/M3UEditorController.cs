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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

            Plugin.Instance.TaskManager.CancelIfRunningAndQueue<SaveTask>();

            return NoContent();
        }

        public class PlaylistId
        {
            public string PlaylistUrl { get; set; }
            public int PageNum { get; set; }
            public bool ShowHidden { get; set; }
            public string SearchString { get; set; }
        }

        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("GetChannels")]
        public ActionResult<String> GetChannelsRequest([FromBody] PlaylistId playlist)
        {
            List<M3UItem> m3UPlaylist = new List<M3UItem>();
            if (Plugin.Instance.M3UChannels.ContainsKey(playlist.PlaylistUrl))
            {
                m3UPlaylist = Plugin.Instance.M3UChannels[playlist.PlaylistUrl];

                if (m3UPlaylist.Count < (playlist.PageNum - 1) * 100)
                    return NotFound();
            }
            else
            {
                return NotFound();
            }

            Dictionary<string, object> returnChannels = new Dictionary<string, object>();

            List<M3UBasicChannel> basicChannels = new List<M3UBasicChannel>();

            int maxNum = playlist.PageNum * 100;
            if (maxNum > m3UPlaylist.Count)
                maxNum = m3UPlaylist.Count;

            int channelCount = 0;

            var currentChannel = 0;
            var startChannel = (playlist.PageNum - 1) * 100;

            foreach (var chan in m3UPlaylist)
            {
                if (!playlist.ShowHidden && chan.hidden)
                {
                    currentChannel++;
                    continue;
                }
                if (playlist.SearchString != null && playlist.SearchString.Length > 0 && (!chan.Name.ToLower().Contains(playlist.SearchString.ToLower())
                    && (chan.Attributes.ContainsKey("group-title") && !chan.Attributes["group-title"].ToLower().Contains(playlist.SearchString.ToLower()))))
                {
                    currentChannel++;
                    continue;
                }
                if (channelCount >= startChannel && channelCount < maxNum)
                {
                    basicChannels.Add(new M3UBasicChannel()
                    {
                        Id = currentChannel,
                        Name = chan.Name,
                        Hidden = chan.hidden
                    });
                }
                channelCount++;
                currentChannel++;
            }

            returnChannels.Add("channelsData", basicChannels);
            returnChannels.Add("channels", channelCount);
            returnChannels.Add("pages", Math.Ceiling(channelCount / 100.0));

            return Ok(returnChannels);
        }

        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("HideChannels")]
        public ActionResult<String> HideChannelsRequest([FromBody] PlaylistId playlist)
        {
            List<M3UItem> m3UPlaylist = new List<M3UItem>();
            if (Plugin.Instance.M3UChannels.ContainsKey(playlist.PlaylistUrl))
            {
                m3UPlaylist = Plugin.Instance.M3UChannels[playlist.PlaylistUrl];

                if (m3UPlaylist.Count < (playlist.PageNum - 1) * 100)
                    return NotFound();
            }
            else
            {
                return NotFound();
            }

            Dictionary<string, object> returnChannels = new Dictionary<string, object>();

            List<M3UBasicChannel> basicChannels = new List<M3UBasicChannel>();

            int maxNum = playlist.PageNum * 100;
            if (maxNum > m3UPlaylist.Count)
                maxNum = m3UPlaylist.Count;

            int channelCount = 0;

            var currentChannel = 0;
            var startChannel = (playlist.PageNum - 1) * 100;

            foreach (var chan in m3UPlaylist)
            {
                if (!playlist.ShowHidden && chan.hidden)
                {
                    currentChannel++;
                    continue;
                }
                if (playlist.SearchString != null && playlist.SearchString.Length > 0 && (!chan.Name.ToLower().Contains(playlist.SearchString.ToLower())
                    && (chan.Attributes.ContainsKey("group-title") && !chan.Attributes["group-title"].ToLower().Contains(playlist.SearchString.ToLower()))))
                {
                    currentChannel++;
                    continue;
                }
                chan.hidden = true;
            }

            Plugin.Instance.TaskManager.CancelIfRunningAndQueue<SaveTask>();

            return Ok("done");
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

            //var items = Helper.ParseM3U(m3uFile);

            //Plugin.Instance.M3UChannels.Add(newPlaylist.PlaylistUrl, items);

            //Save.SaveM3UChannels(Plugin.Instance.M3UChannels, newPlaylist.PlaylistUrl);

            Plugin.Instance.TaskManager.Execute<RefreshTask>();

            Save.SaveM3UPlaylists(Plugin.Instance.M3UPlaylists);

            return Ok(Plugin.Instance.M3UPlaylists);
        }

        public class DeletePlaylist
        {
            public string PlaylistUrl { get; set; }
        }

        /// <summary>
        /// Scans all movies and merges repeated ones.
        /// </summary>
        /// <reponse code="204">Library scan and merge started successfully. </response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [Authorize(Policy = "DefaultAuthorization")]
        [HttpPost("DeletePlaylist")]
        public ActionResult AddPlaylistRequest([FromBody] DeletePlaylist playlist)
        {
            for (int i = Plugin.Instance.M3UPlaylists.Count - 1; i >= 0; i--)
            {
                if (Plugin.Instance.M3UPlaylists[i].PlaylistUrl == playlist.PlaylistUrl)
                {
                    Save.DeleteM3UChannels(playlist.PlaylistUrl);
                    if (Plugin.Instance.M3UChannels.ContainsKey(playlist.PlaylistUrl))
                        Plugin.Instance.M3UChannels.Remove(playlist.PlaylistUrl);
                    Plugin.Instance.M3UPlaylists.RemoveAt(i);
                }
            }

            Save.SaveM3UPlaylists(Plugin.Instance.M3UPlaylists);

            return Ok("done");
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

            string Id = System.Text.Encoding.Default.GetString(Convert.FromBase64String(list.Id));
            string Outfile = null;
            lock (Plugin.Instance.fileLock)
            {
                Outfile = Save.LoadM3U(Id);
            }
            if (Outfile == null)
            {
                Outfile = Helper.CreateM3U(Id);
            }

            return Ok(Outfile);
        }
    }
}
