export default function (view) {

    var curChannels;
    var curChannelEdit;
    var curPage = 1;
    var showHidden = true;
    var hideTimeout;
    var reloadTimeout;

    Element.prototype.remove = function () {
        this.parentElement.removeChild(this);
    }

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

    function createListItem(cName, cId) {

        return `<div class="listItem listItem-border"> 
            <span class="material-icons listItemIcon play_arrow" ></span> 
                <div class="listItemBody"> 
                    <div class="listItemBodyText" id="lblChanName${cId}">${cName}</div>
                </div>
                <button type="button" is="paper-icon-button-light" id="btnEdit${cId}" channelid="${cId}" class="btnStartTask paper-icon-button-light" title="Edit Channel">
                    <span class="material-icons edit"></span>
                </button>
                        </div >`;
    }

    function getEditButton(cId) {
        return `<button type="button" is="paper-icon-button-light" id="btnEdit${cId}" channelid="${cId}" class="btnStartTask paper-icon-button-light" title="Edit Channel">
                    <span class="material-icons edit"></span>
                </button>`;
    }

    function getCancelSaveButtons(cId) {
        return `<button type="button" is="paper-icon-button-light" id="btnCancel${cId}" channelid="${cId}" class="btnStartTask paper-icon-button-light" title="Cancel Edit">
            <span class="material-icons cancel"></span>
        </button>
            <button type="button" is="paper-icon-button-light" id="btnSave${cId}" channelid="${cId}" class="btnStartTask paper-icon-button-light" title="Save Channel">
                <span class="material-icons save"></span>
            </button>`;
    }

    function insertEditForm(div, id) {
        let editFrag = document.createRange().createContextualFragment(`<form id="channelAttributeForm" channelid="${id}">
                <input type="hidden" id="channelAttrSelectionPlaylist" name="channelAttrSelectionPlaylist" value="">
                <div class="inputContainer">
                    <label class="inputeLabel inputLabelUnfocused" for="channelName">Channel name</label>
                    <input id="channelName" name="channelName" type="text" is="emby-input" />
                </div>
                <div class="selectContainer">
                    <label class="selectLabel" for="attrSelection">Pick an attribute of the channel to edit</label>
                    <select is="emby-select" id="attrSelection" name="attrSelection" class="emby-select-withcolor emby-select">
                    </select>
                </div>
                <div class="inputContainer">
                    <label class="inputeLabel inputLabelUnfocused" for="channelAttr">Attribute value</label>
                    <input id="channelAttr" name="channelAttr" type="text" is="emby-input" />
                </div>
            </form>`);
        div.parentElement.insertBefore(editFrag, div.nextSibling);
    }

    function addHideBtn(cId) {
        let hideBtn = document.createRange().createContextualFragment(getHideBtn(curChannels.channelsData[cId].Hidden, cId));

        let editMode = false;

        let oldBtn = document.getElementById(`hideBtn${cId}`);
        if (oldBtn != null) {
            oldBtn.remove();
        }

        let editBtn = document.getElementById('btnEdit' + cId);
        if (editBtn != null)
            editBtn.parentElement.insertBefore(hideBtn, editBtn);
        else {
            let cancelBtn = document.getElementById(`btnCancel${cId}`);
            cancelBtn.parentElement.insertBefore(hideBtn, cancelBtn);
            editMode = true;
        }

        addVisibleListener(cId);

        return editMode;
    }

    function VisibleListener(e) {
        let targetElem = e.target;
        if (targetElem.nodeName !== "BUTTON")
            targetElem = e.target.parentElement;

        let channelId = targetElem.getAttribute('channelid');
        var query_data = {
            m3UChannelId: curChannels.channelsData[channelId].Id,
            PlaylistUrl: document.querySelector('#playlistSelection').value
        };

        var request = {
            url: ApiClient.getUrl('M3UEditor/GetChannel'),
            type: 'POST',
            data: JSON.stringify(query_data),
            dataType: "json",
            contentType: 'application/json'
        };

        ApiClient.fetch(request).then(data => {

            data.hidden = !data.hidden;
            let isHidden = data.hidden;
            var query_data = {
                channel: data,
                PlaylistUrl: document.querySelector('#playlistSelection').value
            };

            var request = {
                url: ApiClient.getUrl('M3UEditor/SaveChannel'),
                type: 'POST',
                data: JSON.stringify(query_data),
                contentType: 'application/json'
            };

            ApiClient.fetch(request).then(data => {
                Dashboard.hideLoadingMsg();
                curChannels.channelsData[channelId].Hidden = isHidden;
                let editMode = addHideBtn(channelId);
                if (editMode)
                    curChannelEdit.Hidden = isHidden;
                Dashboard.alert('Channel visibility set.');
                if (!showHidden)
                    loadChannels(curPage);
            }).catch(function (error) {
                Dashboard.hideLoadingMsg();

                console.log(error.stack);
            });

            Dashboard.hideLoadingMsg();
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            console.log(error.stack);
        });
    }

    function addVisibleListener(cId) {
        document.getElementById(`hideBtn${cId}`).removeEventListener('click', VisibleListener);
        document.getElementById(`hideBtn${cId}`).addEventListener('click', VisibleListener, false);
    }

    function addCancelSaveListeners(i) {
        document.getElementById('btnCancel' + i).addEventListener("click", function (e) {
            var targetElem = e.target;
            if (targetElem.nodeName !== "BUTTON")
                targetElem = e.target.parentElement;
            var div = targetElem.parentElement;
            var cId = targetElem.getAttribute('channelid');

            curChannelEdit = null;

            targetElem.remove();
            div.querySelector(`#btnSave${cId}`).remove();
            div.parentElement.querySelector(`#channelAttributeForm`).remove();
            div.innerHTML += getEditButton(cId);
            addEditListeners(cId);
        }, false);

        document.getElementById('btnSave' + i).addEventListener("click", function (e) {
            var targetElem = e.target;
            if (targetElem.nodeName !== "BUTTON")
                targetElem = e.target.parentElement;
            var div = targetElem.parentElement;
            var cId = targetElem.getAttribute('channelid');

            //Save edits
            Dashboard.showLoadingMsg();
            curChannels.channelsData[cId].Name = curChannelEdit.Name;
            document.getElementById(`lblChanName${cId}`).textContent = curChannelEdit.Name;
            var query_data = {
                channel: curChannelEdit,
                PlaylistUrl: document.querySelector('#playlistSelection').value
            };

            var request = {
                url: ApiClient.getUrl('M3UEditor/SaveChannel'),
                type: 'POST',
                data: JSON.stringify(query_data),
                contentType: 'application/json'
            };

            ApiClient.fetch(request).then(data => {
                Dashboard.hideLoadingMsg();
                
                curChannelEdit = null;

                targetElem.remove();
                div.querySelector(`#btnCancel${cId}`).remove();
                div.parentElement.querySelector(`#channelAttributeForm`).remove();
                div.innerHTML += getEditButton(cId);
                addEditListeners(cId);

                Dashboard.alert('Channel saved.');
            }).catch(function (error) {
                Dashboard.hideLoadingMsg();
                curChannelEdit = null;

                targetElem.remove();
                div.querySelector(`#btnCancel${cId}`).remove();
                div.parentElement.querySelector(`#channelAttributeForm`).remove();
                div.innerHTML += getEditButton(cId);
                addEditListeners(cId);
                console.log(error.stack);
            });
        }, false);

        addVisibleListener(i);
    }

    function addEditListeners(i) {
        document.getElementById('btnEdit' + i).addEventListener("click", function (e) {
            var targetElem = e.target;
            if (targetElem.nodeName !== "BUTTON")
                targetElem = e.target.parentElement;
            var div = targetElem.parentElement;
            var cId = targetElem.getAttribute('channelid');
            targetElem.remove();
            let form = div.parentElement.querySelector(`#channelAttributeForm`);
            if (form !== null) {
                let oldId = form.getAttribute('channelid');
                let oldDiv = document.getElementById(`btnCancel${oldId}`).parentElement;
                oldDiv.querySelector(`#btnCancel${oldId}`).remove();
                oldDiv.querySelector(`#btnSave${oldId}`).remove();

                oldDiv.innerHTML += getEditButton(oldId);
                addEditListeners(oldId);

                form.remove();
            }
            div.innerHTML += getCancelSaveButtons(cId);
            insertEditForm(div, cId);

            //Load channel data
            Dashboard.showLoadingMsg();
            var query_data = {
                m3UChannelId: curChannels.channelsData[cId].Id,
                PlaylistUrl: document.querySelector('#playlistSelection').value
            };

            var request = {
                url: ApiClient.getUrl('M3UEditor/GetChannel'),
                type: 'POST',
                data: JSON.stringify(query_data),
                dataType: "json",
                contentType: 'application/json'
            };

            ApiClient.fetch(request).then(data => {
                curChannelEdit = data;
                var attrSelector = document.getElementById("attrSelection");
                attrSelector.innerHTML = '';

                if (curChannelEdit.Attributes["tvg-chno"] == undefined)
                    curChannelEdit.Attributes["tvg-chno"] = '';

                for (const [key, value] of Object.entries(curChannelEdit.Attributes)) {
                    if (key === "tvg-name")
                        continue;
                    var item = document.createElement("option");
                    item.text = key;
                    item.value = key;
                    attrSelector.appendChild(item);
                }

                document.querySelector('#channelName').value = curChannelEdit.Name;
                document.querySelector('#channelAttr').value = curChannelEdit.Attributes[attrSelector.value];

                document.querySelector("#attrSelection").addEventListener('change', function (event) {
                    document.querySelector("#channelAttr").value = curChannelEdit.Attributes[document.querySelector("#attrSelection").value];
                });

                document.querySelector("#channelAttr").addEventListener('change', function (event) {
                    if (curChannelEdit == null)
                        return;
                    curChannelEdit.Attributes[document.querySelector('#attrSelection').value] = document.querySelector("#channelAttr").value;
                });

                document.querySelector("#channelName").addEventListener('change', function (event) {
                    if (curChannelEdit == null)
                        return;
                    curChannelEdit.Name = document.querySelector("#channelName").value;
                    if (curChannelEdit.Attributes["tvg-name"] !== null)
                        curChannelEdit.Attributes["tvg-name"] = document.querySelector("#channelName").value;
                });

                Dashboard.hideLoadingMsg();
            }).catch(function (error) {
                Dashboard.hideLoadingMsg();
                console.log(error.stack);
            });

            addCancelSaveListeners(cId);
        }, false);

        addVisibleListener(i);
    }

    function loadChannels(pageNum) {
        Dashboard.showLoadingMsg();
        var query_data = {
            PlaylistUrl: document.querySelector('#playlistSelection').value,
            PageNum: pageNum,
            ShowHidden: showHidden,
            SearchString: document.getElementById('searchTxt').value
        };

        var request = {
            url: ApiClient.getUrl('M3UEditor/GetChannels'),
            type: 'POST',
            data: JSON.stringify(query_data),
            dataType: "json",
            contentType: 'application/json'
        };

        ApiClient.fetch(request).then(data => {
            curChannels = data;
            var channelList = document.getElementById("channelList");
            channelList.innerHTML = '';
            for (var i = 0; i < curChannels.channelsData.length; i++) {
                channelList.innerHTML += createListItem(curChannels.channelsData[i].Name, i);
            }

            for (var i = 0; i < curChannels.channelsData.length; i++) {
                addHideBtn(i);
                addEditListeners(i);
            }
            setupPageButtons(curPage);
            Dashboard.hideLoadingMsg();
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            console.log(error.stack);
        });
    }

    function loadPlaylists() {
        var request = {
            url: ApiClient.getUrl('M3UEditor/GetPlaylists'),
            type: 'POST',
            contentType: 'application/json',
            dataType: "json"
        };

        ApiClient.fetch(request).then(data => {
            var playlistSelector = document.getElementById("playlistSelection");
            playlistSelector.innerHTML = '';
            for (var i = 0; i < data.length; i++) {
                var item = document.createElement("option");
                item.text = data[i].PlaylistName;
                item.value = data[i].PlaylistUrl;
                playlistSelector.appendChild(item);
            }
            Dashboard.hideLoadingMsg();
            loadChannels(curPage);
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            console.log(error.stack);

        });
    }

    function getHideBtn(hidden, cId) {
        let btnType = 'visibility';
        let btnText = 'Hide Channel';
        if (hidden) {
            btnType = 'visibility_off';
            btnText = 'Show Channel';
        }

        return `<button type="button" is="paper-icon-button-light" id="hideBtn${cId}" channelid="${cId}" class="btnStartTask paper-icon-button-light" title="${btnText}">
                            <span class="material-icons ${btnType}"></span>
                        </button>`;
    }

    function getNextBtn() {
        return `<button type="button" is="paper-icon-button-light" id="nextPage" class="btnStartTask paper-icon-button-light" title="Next Page">
                            <span class="material-icons navigate_next"></span>
                        </button>`;
    }

    function getPrevBtn() {
        return `<button type="button" is="paper-icon-button-light" id="prevPage" class="btnStartTask paper-icon-button-light" title="Previous Page">
                            <span class="material-icons navigate_before"></span>
                        </button>`;
    }

    function getHiddenFilterBtn(icon) {
        return `<button type="button" is="paper-icon-button-light" id="btnFilterHidden" title="Show Hidden Channels" class="emby-input-iconbutton paper-icon-button-light">
                                <span class="material-icons ${icon}"></span>
                            </button>`;
    }

    function nextPageEvent(e) {
        curPage += 1;
        loadChannels(curPage);
    }

    function prevPageEvent(e) {
        curPage -= 1;
        loadChannels(curPage);
    }

    function HidePageEvent() {
        let hideEdits = curChannels.channelsData;
        let url = document.getElementById('playlistSelection').value;

        for (var i = 0; i < hideEdits.length; i++) {
            var query_data = {
                m3UChannelId: hideEdits[i].Id,
                PlaylistUrl: url
            };

            var request = {
                url: ApiClient.getUrl('M3UEditor/GetChannel'),
                type: 'POST',
                data: JSON.stringify(query_data),
                dataType: "json",
                contentType: 'application/json'
            };

            ApiClient.fetch(request).then(data => {
                data.hidden = true;

                var query_data = {
                    channel: data,
                    PlaylistUrl: url
                };

                var request = {
                    url: ApiClient.getUrl('M3UEditor/SaveChannel'),
                    type: 'POST',
                    data: JSON.stringify(query_data),
                    contentType: 'application/json'
                };

                ApiClient.fetch(request).then(data => {
                    Dashboard.hideLoadingMsg();
                    Dashboard.alert(`Channel visibility set for channel.`);
                    clearTimeout(hideTimeout);
                    hideTimeout = setTimeout(function () { loadChannels(curPage); }, 100);
                }).catch(function (error) {
                    Dashboard.hideLoadingMsg();

                    console.log(error.stack);
                });

                Dashboard.hideLoadingMsg();
            }).catch(function (error) {
                Dashboard.hideLoadingMsg();
                console.log(error.stack);
            });
        }
    }

    function HideAllPagesEvent() {
        Dashboard.showLoadingMsg();
        var query_data = {
            PlaylistUrl: document.querySelector('#playlistSelection').value,
            PageNum: 1,
            ShowHidden: showHidden,
            SearchString: document.getElementById('searchTxt').value
        };

        var request = {
            url: ApiClient.getUrl('M3UEditor/HideChannels'),
            type: 'POST',
            data: JSON.stringify(query_data),
            dataType: "json",
            contentType: 'application/json'
        };

        ApiClient.fetch(request).then(data => {
            curPage = 1;
            loadChannels(curPage);
            Dashboard.hideLoadingMsg();
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            console.log(error.stack);
        });
    }

    function setupPageButtons(page) {
        let prevBtn = document.getElementById('prevPage');
        let prevBool = true;

        let nextBtn = document.getElementById('nextPage');
        let nextBool = true;

        let hidePageBtn = document.getElementById('hidePage');
        let hideAllPagesBtn = document.getElementById('hideAllPages');

        let maxPages = curChannels.pages;

        let startChannel = (curPage - 1) * 100 + 1;
        
        let endChannel = curChannels.channels;
        if (curChannels.channelsData.length == 100) {
            endChannel = curPage * 100;
        }


        document.getElementById('pageText').textContent = `Shwoing Page ${curPage} of ${maxPages} - ${startChannel} to ${endChannel} of ${curChannels.channels} channels`;
        if (page == 1) {
            if (prevBtn != null)
                prevBtn.remove();
            prevBool = false;
        }
        if (page == curChannels.pages) {
            if (nextBtn != null)
                nextBtn.remove();
            nextBool = false;
        }

        if (prevBool) {
            if (prevBtn == null) {
                prevBtn = document.createRange().createContextualFragment(getPrevBtn());
                if (nextBtn == null)
                    document.getElementById('navigateContainer').appendChild(prevBtn);
                else
                    nextBtn.parentElement.insertBefore(prevBtn, nextBtn);
                prevBtn = document.getElementById('prevPage');
            }
            prevBtn.removeEventListener('click', prevPageEvent);
            prevBtn.addEventListener('click', prevPageEvent, false);
        }

        if (nextBool) {
            if (nextBtn == null) {
                nextBtn = document.createRange().createContextualFragment(getNextBtn());
                document.getElementById('navigateContainer').appendChild(nextBtn);
                nextBtn = document.getElementById('nextPage');
            }
            nextBtn.removeEventListener('click', nextPageEvent);
            nextBtn.addEventListener('click', nextPageEvent, false);
        }

        hidePageBtn.removeEventListener('click', HidePageEvent);
        hidePageBtn.addEventListener('click', HidePageEvent, false);

        hideAllPagesBtn.removeEventListener('click', HideAllPagesEvent);
        hideAllPagesBtn.addEventListener('click', HideAllPagesEvent, false);
        
    }

    function addFilterListener() {
        document.querySelector('#btnFilterHidden').addEventListener('click', function (e) {
            showHidden = !showHidden;
            let btn = document.getElementById('btnFilterHidden');
            let parent = btn.parentElement;
            if (showHidden) {
                btn.remove();
                parent.appendChild(document.createRange().createContextualFragment(getHiddenFilterBtn('visibility')))
            } else {
                btn.remove();
                parent.appendChild(document.createRange().createContextualFragment(getHiddenFilterBtn('visibility_off')));
            }
            curPage = 1;
            loadChannels(curPage);
            addFilterListener();
        }, false);
    }

    view.addEventListener("viewshow", function (e) {
        LibraryMenu.setTabs('m3u_channels', 2, getTabs);

        document.querySelector('#searchTxt').addEventListener('input', function (e) {
            curPage = 1;
            clearTimeout(reloadTimeout);
            reloadTimeout = setTimeout(function () { loadChannels(curPage); }, 100);
        }, false);

        addFilterListener();

        document.querySelector('#playlistSelection').addEventListener('change', function (e) {
            curPage = 1;
            loadChannels(curPage);
        }, false);

        loadPlaylists();
    });

}