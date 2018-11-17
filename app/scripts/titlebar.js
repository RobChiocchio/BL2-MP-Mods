(function(){
    const electron = require('electron');
    const remote = electron.remote;

    function init() {
        document.getElementById("buttonMinimize").addEventListener("click", function(e) {
            const window = remote.getCurrentWindow();
            window.minimize();
        });

        document.getElementById("buttonMaximize").addEventListener("click", function(e) {
            const window = remote.getCurrentWindow();
            if (!window.isMaximized()) {
                window.maximize();
            } else {
                window.unmaximize();
            }
        });

        document.getElementById("buttonClose").addEventListener("click", function(e) {
            const window = remote.getCurrentWindow();
            window.close();
        });
    }

    document.onreadystatechange = function () {
        if (document.readyState == "complete") {
            init();
        }
    }
})();