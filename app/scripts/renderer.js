(function(){
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

        buttonPatch.addEventListener("click", function(e) {
            patch();
        });

        //check if admin, if not, popup or request restart as admin?
    }

    document.onreadystatechange = function () {
        if (document.readyState == "complete") {
            init();
        }
    }
})();