export default function (view) {

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

    view.addEventListener("viewshow", function (e) {
        LibraryMenu.setTabs('m3u_main', 0, getTabs);
    });
}