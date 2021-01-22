export default function (view) {

    var apiKey;

    const getConfigurationPageUrl = (name) => {
        return 'configurationpage?name=' + encodeURIComponent(name);
    }

    function getTabs() {
        var tabs = [
            {
                href: getConfigurationPageUrl('m3u_main'),
                name: 'Stats'
            },
            {
                href: getConfigurationPageUrl('m3u_playlists'),
                name: 'Playlists'
            },
            {
                href: getConfigurationPageUrl('m3u_channels'),
                name: 'Channels'
            }];
        return tabs;
    }

    function getApiKey() {
        var request = {
            url: ApiClient.getUrl('M3UEditor/GetApiKey'),
            type: 'POST',
            contentType: 'application/json',
            dataType: "json"
        };

        ApiClient.fetch(request).then(data => {
            apiKey = data.key;
            Dashboard.hideLoadingMsg();
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            Dashboard.alert({
                message: error.stack
            });

        });
    }

    function deletePlaylist(pUrl) {
        var query_data = {
            PlaylistUrl: pUrl
        };

        var request = {
            url: ApiClient.getUrl('M3UEditor/DeletePlaylist'),
            type: 'POST',
            data: JSON.stringify(query_data),
            dataType: "json",
            contentType: 'application/json'
        };

        ApiClient.fetch(request).then(data => {
            Dashboard.hideLoadingMsg();
            refreshPlaylists();
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            Dashboard.alert({
                message: error.stack
            });

        });
    }

    function copyUrl(pUrl) {

        var copyUrl = ApiClient.getUrl('M3UEditor/GetM3UList?Id=' + encodeURI(btoa(pUrl)) + "&key=" + encodeURI(apiKey));

        if (!navigator.clipboard) {
            //fallbackCopyTextToClipboard(copyUrl);
            return;
        }
        navigator.clipboard.writeText(copyUrl).then(function () {
            console.log('Async: Copying to clipboard was successful!');
            Dashboard.alert('Copied to clipboard');
        }, function (err) {
            console.error('Async: Could not copy text: ', err);
            Dashboard.alert('Failed to copy to clipboard');
        });
    }

    function createListItem(pName, pUrl, pId) {

        return `<div class="listItem listItem-border"> 
            <span class="material-icons listItemIcon play_arrow" ></span> 
                <div class="listItemBody two-line"> 
                    <h3 class="listItemBodyText">${pName}</h3> 
                    <div class="secondary listItemBodyText">${pUrl}</div>
                </div>
                <button type="button" is="paper-icon-button-light" id="btnCopy${pId}" url="${pUrl}" class="btnStartTask paper-icon-button-light" title="Copy Import URL">
                    <span class="material-icons content_copy"></span>
                </button>
                <button type="button" is="paper-icon-button-light" id="btnDelete${pId}" url="${pUrl}" class="btnStartTask paper-icon-button-light" title="Delete">
                    <span class="material-icons delete"></span>
                </button>
                        </div >`;
    }

    function refreshPlaylists() {
        var request = {
            url: ApiClient.getUrl('M3UEditor/GetPlaylists'),
            type: 'POST',
            contentType: 'application/json',
            dataType: "json"
        };

        ApiClient.fetch(request).then(data => {
            Dashboard.showLoadingMsg();
            var playlistList = document.getElementById("playlistList");
            playlistList.innerHTML = '';
            for (var i = 0; i < data.length; i++) {
                var pId = data[i].PlaylistUrl;
                var itemHtml = createListItem(data[i].PlaylistName, data[i].PlaylistUrl, i);
                playlistList.innerHTML += itemHtml;
            }
            for (var i = 0; i < data.length; i++) {
                document.getElementById('btnCopy' + i).addEventListener("click", function (e) {
                    var url = e.target.getAttribute('url');
                    if (e.target.nodeName === "SPAN")
                        url = e.target.parentElement.getAttribute('url');
                    copyUrl(url);
                }, false);

                document.getElementById('btnDelete' + i).addEventListener("click", function (e) {
                    var url = e.target.getAttribute('url');
                    if (e.target.nodeName === "SPAN")
                        url = e.target.parentElement.getAttribute('url');
                    deletePlaylist(url);
                }, false);
            }
            Dashboard.hideLoadingMsg();
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            Dashboard.alert({
                message: error.stack
            });
        });
    }

    document.querySelector('#newPlaylist').addEventListener('submit', function (event) {
        Dashboard.showLoadingMsg();

        var query_data = {
            PlaylistName: document.querySelector('#playlistName').value,
            PlaylistUrl: document.querySelector('#playlistUrl').value,
            UserAgent: document.querySelector('#userAgent').value
        };

        var request = {
            url: ApiClient.getUrl('M3UEditor/AddPlaylist'),
            type: 'POST',
            data: JSON.stringify(query_data),
            dataType: "json",
            contentType: 'application/json'
        };

        ApiClient.fetch(request).then(data => {
            if (data.ErrorCode == null) {
                var playlistList = document.getElementById("playlistList");
                playlistList.innerHTML = '';
                for (var i = 0; i < data.length; i++) {
                    var itemHtml = createListItem(data[i].PlaylistName, data[i].PlaylistUrl, btoa(data[i].PlaylistUrl));
                    playlistList.innerHTML += itemHtml;
                }
            } else {
                alert(data.ErrorMsg);
            }
            Dashboard.hideLoadingMsg();
            refreshPlaylists();
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            Dashboard.alert({
                message: error.stack
            });

        });

        event.preventDefault();
        return false;
    });

    view.addEventListener("viewshow", function (e) {
        LibraryMenu.setTabs('m3u_playlists', 1, getTabs);

        getApiKey();

        refreshPlaylists();
    });

}