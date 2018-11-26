const exec = require('child_process');
const electron = require('electron');
const remote = electron.remote;
const dialog = remote.dialog;
const log = require('electron-log');
const fs = require('fs');

var buttonPatch = document.getElementById("buttonPatch");
var selectGame = document.getElementById("selectGame");
var loadingBar = document.getElementById("loadingBar");
var loadingBarProgress = document.getElementById("loadingBarProgress");
var statusText = document.getElementById("statusText");

const notifications = [
    {
        title: "Info",
        body: "Done! A Shortcut was placed on your desktop. Press '~' in game to open up console.",
        icon: 'static/icon.png',
    },
];

var skipCopy = false;

var game; //defined by switch
var gameExec; //name of executable, defined below in switch
var gameDir; //name of root directory, defined below in switch
var cooppatchFile = "cooppatch.txt";
var fileCopying;

function progressChanged(percent){
    loadingBarProgress.style.width = percent + '%';

    const window = remote.getCurrentWindow();
    window.setProgressBar(percent / 100);

    switch(percent)
    {
        case 0:
            statusText.innerText = "Looking for " + selectGame[selectGame.selectedIndex].innerText;
            break;

        case 5:
            statusText.innerText = "Removing old patch files";
            break;

        case 10:
            statusText.innerText = "Copying " + fileCopying; //current file copying
            break;

        case 40:
            statusText.innerText = "Installing patches";
            break;

        /*
        case 45:
            statusText.innerText = "Installing DLL loader";
            break;
        */

        case 50:
            statusText.innerText = "Backing up unmodified UPK files";
            break;

        case 60:
            statusText.innerText = "Decompressing UPK files";
            break;

        case 70:
            statusText.innerText = "Modifying WillowGame.upk";
            break;

        case 75:
            statusText.innerText = "Modifying Engine.upk";
            break;

        case 80:
            statusText.innerText = "Modifying " + gameExec;
            break;

        case 90:
            statusText.innerText = "Enabling console";
            break;

        case 95:
            statusText.innerText = "Finishing up";
            break;

        case 100:
            statusText.innerText = "Done!";
            window.setProgressBar(-1); //Disable task bar loading
            /*
            smalltalk.alert("Info", "Done! A Shortcut was placed on your desktop. Press '~' in game to open up console.").then(() => {
                const window = remote.getCurrentWindow();
                window.close();
            });
            */

            if (!window.isFocused()) { //if not focused, notify
                new Notification(notifications[0].title, notifications[0]); //finished notification
            }

            break;
    }

    log.info(percent + "% " + statusText.innerText);
}

function testLoadingBar(){
	var id = setInterval(frame, 20);
	var width = 0;
	function frame(){
		if (width >= 100) {
			clearInterval(id);
		} else {
			width++; 
			progressChanged(width); 
		}
	}
}

function extractUPK(path){
    exec.spawnSync("bin/decompress.exe", ["-game=border", '"' + path + '"']);
}

function patch(){
    game = selectGame[selectGame.selectedIndex].value; //set game to value of game selector
    buttonPatch.style.display = "none"; //hide patch button
    selectGame.style.display = "none"; //hide game selector
    loadingBar.style.display = "block"; //show loading bar
    statusText.style.display = "block"; //show status text

    testLoadingBar();
    return; //debug

    switch (game)
    {
        case "bl1":
            gameExec = "Borderlands.exe";
            gameDir = "Borderlands";
            cooppatchFile = "cooppatch.txt";
            break;
        case "bltps":
            gameExec = "BorderlandsPreSequel.exe";
            gameDir = "BorderlandsPreSequel";
            cooppatchFile = "cooppatch.txt";
            break;
        default: //borderlands 2 or incase some how there isnt a variable
            gameExec = "Borderlands2.exe";
            gameDir = "Borderlands 2";
            cooppatchFile = "cooppatch.txt";
            break;
    }

    progressChanged(0);
    // -- FIND GAME FOLDER --

    iBL = dialog.showOpenDialog({ properties: ['openFile'] }, { filters: [{ extensions: ['exe']}]}, function (fileNames) {
        if (fileNames == undefined) {
            log.warn("No file selected");
            //close patcher?
        }
    }); //path to Borderlands exe
    //check for cancel
    log.info("Input game path: " + iBL);

    var iRootDir = fs.realpath(iBL + '\\..\\..\\..\\..');
    var oRootDir = fs.realpath(iRootDir + '\\server');

    var oBL;
    var iWillowGame;
    var oWillowGame;
    var iEngine;
    var oEngine;

    if (game == "bl1") //if Borderlands 1
    {
        oBL = fs.realpath(oRootDir + "\\Binaries\\" + gameExec);
        iWillowGame = fs.realpath(iRootDir + "\\WillowGame\\CookedPC\\WillowGame.u"); // path to WillowGame.upk
        oWillowGame = fs.realpath(oRootDir + "\\WillowGame\\CookedPC\\WillowGame.u"); // path to WillowGame.upk
        iEngine = fs.realpath(iRootDir + "\\WillowGame\\CookedPC\\Engine.u"); // path to Engine.upk
        oEngine = fs.realpath(oRootDir + "\\WillowGame\\CookedPC\\Engine.u"); // path to Engine.upk
    }
    else
    {
        oBL = fs.realpath(oRootDir + "\\Binaries\\Win32\\" + gameExec);
        iWillowGame = fs.realpath(iRootDir + "\\WillowGame\\CookedPCConsole\\WillowGame.upk"); // path to WillowGame.upk
        oWillowGame = fs.realpath(oRootDir + "\\WillowGame\\CookedPCConsole\\\WillowGame.upk"); // path to WillowGame.upk
        iEngine = fs.realpath(iRootDir + "\\WillowGame\\CookedPCConsole\\Engine.upk"); // path to Engine.upk
        oEngine = fs.realpath(oRootDir + "\\WillowGame\\CookedPCConsole\\Engine.upk"); // path to Engine.upk
    }

    progressChanged(10);
    //TODO: // -- BACKUP BORDERLANDS --

    progressChanged(40);
    //TODO: // -- COPY PATCHES TO BINARIES --

    progressChanged(50);
    // -- RENAME UPK AND DECOMPRESSEDSIZE --

    try {
        fs.renameSync(oWillowGame + ".uncompressed_size", oWillowGame + ".uncompressed_size.bak");
        fs.copyFileSync(oWillowGame, oWillowGame + ".bak");
        fs.renameSync(oEngine + ".uncompressed_size", oEngine + ".uncompressed_size.bak");
        fs.copyFileSync(oEngine, oEngine + ".bak");
    } catch (err) {
        log.error("Failed to back up upks");
        log.error(err, err.stack); // does this work with log.error? if not should I use err.message? write a custom function? TODO
    }
    
    progressChanged(60);
    //TODO: // -- DECOMPRESS UPK FILES --
    //extractUPK("test.upk");

    /*
    try {
        fs.copyFileSync();
        fs.copyFileSync();
    } catch (err) {
        log.error("Could not find decompressed UPKs")
    }
    */

    // -- DELETE UNPACKED FOLDER --

    /*
    try {
        fs.rmdir( => {

        })
    }
    */

    progressChanged(70);
    // -- HEX EDIT WILLOWGAME --

    var streamWillowGame = fs.createWriteStream(oWillowGame);
    var buff = new Buffer(something, "hex"); //TODO

    // -- DEVELOPER MODE
    fs.write(streamWillowGame, [ 0x27 ], 0, 1, 0x006925C7, (err) => {
        if (err) {
            log.error("Failed to enable developer mode in WillowGame.upk");
            throw err;
        }
    });

    // -- EVERY PLAYER GETS THEIR OWN TEAM --
    fs.write(streamWillowGame, [ 0x04, 0x00, 0xC6, 0x8B, 0x00, 0x00, 0x06, 0x44, 0x00, 0x04, 0x24, 0x00 ], 0, 12, 0x007F9151, (err) => {
        if (err) {
            log.error("Failed to give every player their own team in WillowGame.upk");
            throw err;
        }
    });

    // -- PREVENT MENU FROM CANCELLING FAST TRAVEL --
    fs.write(streamWillowGame, [ 0x27 ], 0, 1, 0x006BEAF6, (err) => {
        if (err) {
            log.error("Failed to disable menus cancelling fast travel in WillowGame.upk");
            throw err;
        }
    });

    // -- MORE CACHED PLAYERS --
    fs.write(streamWillowGame, [ 0x39 ], 0, 1, 0x00832B20, (err) => {
        if (err) {
            log.error("Failed to increase cached players size in WillowGame.upk");
            throw err;
        }
    });

    fs.close(streamWillowGame, (err) => {
        if (err) {
            log.warn("Failed to close WillowGame.upk");
            throw err;
        }
    })

    progressChanged(75);
    //TODO: // -- HEX EDIT ENGINE --

    progressChanged(80);
    //TODO: // -- HEX EDIT EXE
    /*
    fs.open(fileNames[0], 'rw', (err, fd) => {
        if (err) {
            log.error("Could not open file " + fileNames[0]);
            throw err;
        }
        fs.close(fd);
    });
    */
    
    progressChanged(90);
    //TODO: // -- ENABLE CONSOLE --
    //only add hotkey if console is not already enabled

    progressChanged(95);
    //TODO: // -- CREATE SHORTCUT ----

    //progressChanged(100);
    // -- DONE --
}

function init() {
    document.getElementById("buttonHelp").addEventListener("click", function(e) {
        dialog.showMessageBox({
            type: "none",
            buttons: [ "How-to guide and FAQ", "Get additional help", "Report a bug", "Close" ],
            title: "Help",
            
            detail: "Robeth's Unlimited COOP Mod & Patcher made by Rob Chiocchio"
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
    if (document.readyState == "complete") {
        init();
    }
}