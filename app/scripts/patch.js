
const exec = require('child_process');
const electron = require('electron');
const remote = electron.remote;
const dialog = remote.dialog;

var buttonPatch = document.getElementById("buttonPatch");
var selectGame = document.getElementById("selectGame");
var loadingBar = document.getElementById("loadingBar");
var loadingBarProgress = document.getElementById("loadingBarProgress");
var statusText = document.getElementById("statusText");

var game = "";
var path = "";
var gameExec = "";
var gameDir = "";
var cooppatchFile = "cooppatch.txt";
var fileCopying = "";

function progressChanged(percent){
    loadingBarProgress.style.width = percent + '%';

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
            statusText.innerText = "Modifying " + gameExec + ".exe";
            break;

        case 90:
            statusText.innerText = "Enabling console";
            break;

        case 95:
            statusText.innerText = "Finishing up";
            break;

        case 100:
            statusText.innerText = "Done!";
            /*
            smalltalk.alert("Info", "Done! A Shortcut was placed on your desktop. Press '~' in game to open up console.").then(() => {
                const window = remote.getCurrentWindow();
                window.close();
            });
            */
            break;
    }

    console.info(statusText.innerText);
}

function testLoadingBar(){
	var id = setInterval(frame, 10);
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

    dialog.showOpenDialog({ properties: ['openFile'] }, { filters: [{ extensions: ['exe']}]}, function (_path) {
        path = _path;
        console.log(path);
    });

    //TODO: check to make sure game exists, did not click cancel

    progressChanged(10);
    //TODO: // -- BACKUP BORDERLANDS --

    progressChanged(40);
    //TODO: // -- COPY PATCHES TO BINARIES --

    progressChanged(50);
    //TODO: // -- RENAME UPK AND DECOMPRESSEDSIZE --
    
    progressChanged(60);
    //TODO: // -- DECOMPRESS UPK FILES --
    //extractUPK("test.upk");

    progressChanged(70);
    //TODO: // -- HEX EDIT WILLOWGAME --

    progressChanged(75);
    //TODO: // -- HEX EDIT ENGINE --

    progressChanged(80);
    //TODO: // -- HEX EDIT EXE
    
    progressChanged(90);
    //TODO: // -- ENABLE CONSOLE --

    progressChanged(95);
    //TODO: // -- CREATE SHORTCUT ----

    progressChanged(100);
    // -- DONE --

    testLoadingBar();
}

    document.onreadystatechange = function () {
    if (document.readyState == "complete") {
        init();
    }
}