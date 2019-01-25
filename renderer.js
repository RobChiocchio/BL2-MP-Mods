const exec = require("child_process");
const electron = require("electron");
const remote = electron.remote;
const dialog = remote.dialog;
//const log = require("electron-log");
const os = require("os");
const fs = require("fs");

const JSON5 = require("json5"); // To parse the patches
var mods = JSON5.parse(fs.readFileSync("./scripts/mods.json5")); // Load patches from JSON5 file TODO: replace sync with async/await
var defaults = JSON5.parse(fs.readFileSync("./scripts/defaults.json5")); // TODO: replace sync with async/await
//const hexedit = require("./scripts/hexedit.js");

// Google Analytics
const ua = require("universal-analytics");
var visitor = ua("UA-130033914-1");

/* 
// Sentry crash reporting
const Sentry = require("@sentry/electron");
Sentry.init({dsn: "https://67ebae4288c24fdcb79c7f14cff030ab@sentry.io/1332146"});
// TODO: implement a meaningful crash handler
 */

// LogRocket error reporting
// import LogRocket from 'logrocket';
const LogRocket = require("logrocket");
LogRocket.init("jy6eo5/borderlands-electron-patcher", {
    release: remote.app.getVersion(),
    console: {
        isEnabled: true,
        shouldAggregateConsoleErrors: true,
    },
    shouldCaptureIP: false, // For security reasons IP capture is disabled
});

LogRocket.identify(visitor.cid); // Set LogRocket ID to Google Analytics ID

LogRocket.getSessionURL(function (sessionURL) { // Log LogRocket session URL to console for convenience
    visitor.event("LogRocket", sessionURL); // Log session URL to Google Analytics

    // Log basic information
    LogRocket.info("Version test: " +  remote.app.getVersion());
    LogRocket.info(os.type() + " " + os.release());
    LogRocket.info("LogRocket session URL: " + sessionURL);

    /* Sentry.configureScope(scope => { // Sentry integration
        scope.addEventProcessor(async (event) => {
            event.extra.sessionURL = LogRocket.sessionURL;
            return event;
        });
    }); */
});

var buttonPatch = document.getElementById("buttonPatch");
var selectGame = document.getElementById("selectGame");
var loadingBar = document.getElementById("loadingBar");
var loadingBarProgress = document.getElementById("loadingBarProgress");
var statusText = document.getElementById("statusText");

const notifications = [
    {
        title: "Info",
        body: "Done! A Shortcut was placed on your desktop. Press '~' in game to open up console.",
        icon: "static/icon.png",
    },
];

var skipCopy = false;
var game; // Contains all of the game's info for patching

function copyStringToClipboard (str) {
    var element = document.createElement("textarea"); // Create new element
    element.value = str; // Set value (string to be copied)
    // Set non-editable to avoid focus and move outside of view
    element.setAttribute("readonly", ""); 
    element.style = {position: "absolute", left: "-9999px"};
    document.body.appendChild(element);
    element.select(); // Select text inside element
    document.execCommand("copy"); // Copy text to clipboard
    document.body.removeChild(element); // Remove temporary element
}

function reportStatus(status){
    statusText.innerText = status; // Set loading status info
    LogRocket.info(status); // Log status with LogRocket 
}

function patch(){
    LogRocket.track("Patching started"); // Track how many times the patcher process is started
    reportStatus("Initializing");

    buttonPatch.style.display = "none"; // Hide patch button
    selectGame.style.display = "none"; // Hide game selector
    loadingBar.style.display = "block"; // Show loading bar
    statusText.style.display = "block"; // Show status text

    //testLoadingBar();
    //return; //debug

    switch (selectGame.selectedIndex)
    {
        case defaults.game.bl1.id:
            game = defaults.game.bl1;
            break;
        case defaults.game.bl2.id:
            game = defaults.game.bl2;
            break;
        case defaults.game.bltps.id:
            game = defaults.game.bltps;
            break;
        default: //throw error
            throw "InvalidGame";
    }

    // -- FIND GAME FOLDER --
    reportStatus("Looking for " + selectGame[selectGame.selectedIndex].innerText);

    iBL = dialog.showOpenDialog({ properties: ["openFile"] }, { filters: [{ extensions: ["exe"]}]}, function (fileNames) {
        if (fileNames == undefined) {
            LogRocket.warn("No file selected");
            //close patcher?
        }
    }); //path to Borderlands exe
    //check for cancel
    LogRocket.info("Input game path: " + iBL);

    var iRootDirectoryPath = fs.realpath(iBL + "\\..\\..\\..\\..");
    var oRootDirectoryPath = fs.realpath(iRootDirectoryPath + "\\server");

    // TODO: is this all obsolete?
    var oBL;
    var iWillowGame;
    var oWillowGame;
    var iEngine;
    var oEngine;

    oBL = fs.realpath(oRootDirectoryPath + game.executablePath);
    iWillowGame = fs.realpath(iRootDirectoryPath + game.contentDirectoryRelativePath + "WillowGame" + game.packageExtension); // path to WillowGame.upk
    oWillowGame = fs.realpath(oRootDirectoryPath +  game.contentDirectoryRelativePath + "WillowGame" + game.packageExtension); // path to WillowGame.upk
    iEngine = fs.realpath(iRootDirectoryPath +  game.contentDirectoryRelativePath + "Engine" + game.packageExtension); // path to Engine.upk
    oEngine = fs.realpath(oRootDirectoryPath +  game.contentDirectoryRelativePath + "Engine" + game.packageExtension); // path to Engine.upk

    //TODO: // -- BACKUP BORDERLANDS --
    reportStatus("Backing up game files");

    //TODO: // -- COPY PATCHES TO BINARIES --
    reportStatus("Copying patches to the binaries folder");

    // ========== OLD!!! ==========

    /* OLD!!!! DEBUG, REMOVE ETC
    var streamWillowGame = fs.createWriteStream(oWillowGame, "hex");
    var bufferWillowGame = new Buffer(something, "hex"); //TODO

    for(var patch in patches.WillowGame) { // Run all of the patches for the WillowGame package
        hexedit(patch, streamWillowGame);
    }

    // -- DEVELOPER MODE
    fs.write(streamWillowGame, [ 0x27 ], 0, 1, 0x006925C7, (err) => {
        if (err) {
            LogRocket.error("Failed to enable developer mode in WillowGame.upk");
            throw err;
        }
    });

    // -- EVERY PLAYER GETS THEIR OWN TEAM --
    fs.write(streamWillowGame, [ 0x04, 0x00, 0xC6, 0x8B, 0x00, 0x00, 0x06, 0x44, 0x00, 0x04, 0x24, 0x00 ], 0, 12, 0x007F9151, (err) => {
        if (err) {
            LogRocket.error("Failed to give every player their own team in WillowGame.upk");
            throw err;
        }
    });

    // -- PREVENT MENU FROM CANCELLING FAST TRAVEL --
    fs.write(streamWillowGame, [ 0x27 ], 0, 1, 0x006BEAF6, (err) => {
        if (err) {
            LogRocket.error("Failed to disable menus cancelling fast travel in WillowGame.upk");
            throw err;
        }
    });

    // -- MORE CACHED PLAYERS --
    fs.write(streamWillowGame, [ 0x39 ], 0, 1, 0x00832B20, (err) => {
        if (err) {
            LogRocket.error("Failed to increase cached players size in WillowGame.upk");
            throw err;
        }
    });

    fs.close(streamWillowGame, (err) => {
        if (err) {
            LogRocket.warn("Failed to close WillowGame.upk");
            throw err;
        }
    });
    */

    //TODO: // -- HEX EDIT EXE
    /*
    fs.open(fileNames[0], "rw", (err, fd) => {
        if (err) {
            LogRocket.error("Could not open file " + fileNames[0]);
            throw err;
        }
        fs.close(fd);
    });
    */

    /* ========== NEW METHOD ========== */

   var patchedPackages = []; // Track which packages have been decompressed and pached in the EXE

   for (mod in mods) {
        if (mod.game == game.id) { // If the mod is for the right game
            for (patch in mod.patches) {
                if (patch.originalData.length != patch.length || patch.modifiedData.length != patch.length || patch.lengh <= 0) { // Validate patch data
                    // TODO: ERROR, stop process (revert on error?)
                } // TODO, catch any other missing non-optionals or invalid values

                var packagePath = oRootDirectoryPath + game.contentDirectoryRelativePath + package + game.packageExtension;
                if (!patchedPackages.includes((modification.package).toLowerCase())) { // Check if package is on list yet
                    // TODO: check if package is already decompressed?
                    patchedPackages.push((modification.package).toLowerCase()); // Add package to list of packages to fix in exe
                    reportStatus("Decompressing " + modification.package + game.packageExtension);
                    exec.spawnSync("bin\\decompress.exe", ["-game=border", "\"" + packagePath + "\""]); // Decompress package TODO: replace sync with async/await

                    reportStatus("Backing up compressed package files");
                    try { // Back up uncompressed_size and the compressed package
                        fs.renameSync(packagePath + ".uncompressed_size", packagePath + ".uncompressed_size.bak"); // TODO: replace sync with async/await
                        fs.copyFileSync(packagePath, packagePath + ".bak"); // TODO: replace sync with async/await
                    } catch (err) {
                        LogRocket.error("Failed to back up upks");
                        LogRocket.error(err, err.stack); // does this work with log.error? if not should I use err.message? write a custom function? TODO
                    }

                    reportStatus("Installing decompressed " + modification.package);
                    /*
                    try { // Copy decompressed package to packagePath
                        fs.copyFileSync(); // TODO: replace sync with async/await
                    } catch (err) {
                        LogRocket.error("Could not find decompressed UPKs")
                    }
                    */
                    
                }

                reportStatus("Applying patch: \"" + patch.description + "\""); // TODO: is this too much? should I only do this for each mod?

                var position = patch.position; // Because if it is undefined, I can figure out what it is and redefine it

                if (patch.position == null) { // If the absolute position property is not set
                    // TODO, search for original data and get position, seek to position in writeStream
                    //var buffer = fs.readFileSync(packagePath, "hex"); // Load the package to buffer TODO: replace sync with async/await
                }

                var streamPackage = fs.createWriteStream(packagePath, {flags: "r+", encoding: "hex", start: position, end: position + patch.length}); // Open package filestream TODO: add callback (maybe?)
                streamPackage.write(patch.modifiedData); // TODO: ADD CALLBACK and Check and revert to original data if necessary
                streamPackage.end(); // Close filestream
                // fs.close(streamPackage, (err) => { // Close filestream -- TODO: IS THIS NECESSARY OR IS END ENOUGH?
                //     if (err) {
                //         LogRocket.warn("Failed to close package"); // TODO: print package name
                //         throw err;
                //     }
                // });
            }
        }
   }

   reportStatus("Enabling console");
   // TODO: enable console in exe
   // TODO: add console hotkey to config (if not already there)

   reportStatus("Enabling modified packages");
   for (package in patchedPackages) {
       // TODO: change name in exe
   }

   // TODO: close exe filestream

    reportStatus("Creating shortcuts");
    //TODO: // -- CREATE SHORTCUT ----

    reportStatus("Cleaning up");
    // TODO: cleanup
    /*
    try { // Delete unpacked folder
        fs.rmdir( => {

        })
    }
    */
    // -- DONE --

    reportStatus("Done!");

    if (!window.isFocused()) { //if not focused, notify
        new Notification(notifications[0].title, notifications[0]); //finished notification
    }

    LogRocket.track("Patching finished"); // Track how successful the patching is
}

function init() {
    document.getElementById("buttonHelp").addEventListener("click", function(e) {
        dialog.showMessageBox({
            type: "none",
            buttons: [ "How-to guide and FAQ", "Get additional help", "Copy session ID to clipboard", "Close" ], //, "Report a bug"
            cancelId: 3,
            title: "Help",
            message: "ID: " + visitor.cid, // Analytics ID to give to me to check error logs
            detail: "Robeth's Unlimited COOP Mod & Patcher made by Rob Chiocchio"
        }, function(response) {
            switch(response) {
                case 0: // Open Steam guide in browser
                    electron.shell.openExternal("https://steamcommunity.com/sharedfiles/filedetails/?id=1151711689");
                    break;
                case 1: // Open Steam group page in browser
                    electron.shell.openExternal("https://steamcommunity.com/groups/bl2unlimitedcoop");
                    break;
                case 2: // Copy Analytics ID to clipboard
                    copyStringToClipboard(visitor.cid);
                    break;
                default: // Close dialog is set to 3
                    break;
            }

        });
    });

    document.getElementById("buttonMinimize").addEventListener("click", function(e) {
        const window = remote.getCurrentWindow();
        window.minimize();
    });

    /*
    document.getElementById("buttonMaximize").addEventListener("click", function(e) {
        const window = remote.getCurrentWindow();
        if (!window.isMaximized()) {
            window.maximize();
        } else {
            window.unmaximize();
        }
    });
    */

    document.getElementById("buttonClose").addEventListener("click", function(e) {
        const window = remote.getCurrentWindow();
        window.close();
    });

    buttonPatch.addEventListener("click", function(e) {
        buttonPatch.disabled = true;
        patch();
    });

        //check if admin, if not, popup or request restart as admin?
}

document.onreadystatechange = function () {
    if (document.readyState === "complete") {
        init();
    }
};