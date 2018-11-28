const electron = require("electron");
const app = electron.app; // control app
const BrowserWindow = electron.BrowserWindow; // create native browser window
const ipcMain = electron.ipcMain;
const {autoUpdater} = require("electron-updater");
const log = require("electron-log");
const util = require('util');

// Keep a global reference of the window object, if you don't, the window will
// be closed automatically when the JavaScript object is garbage collected.
let mainWindow;

log.transports.file.level = "debug";
log.transports.console.level = "debug";

autoUpdater.logger = log;
autoUpdater.logger.transports.file.level = "info"; //does this override the previous setting?
log.info("Starting patcher version " + autoUpdater.currentVersion);
console.debug("Starting patcher version " + autoUpdater.currentVersion);

const Sentry = require("@sentry/electron"); // Do I even care about Sentry? Should I just remove it?
Sentry.init({dsn: "https://67ebae4288c24fdcb79c7f14cff030ab@sentry.io/1332146"});

function createWindow() {
    // Create the browser window.
    mainWindow = new BrowserWindow({
        width: 300,
        height: 140,
        minWidth: 300,
        minHeight: 140,
        frame: false, // remove frame from windows apps
        titleBarStyle: "hidden", // hide mac titlebar
        transparent: true, //allow rounded corners
        icon: "static/icon.png",
    });

    //mainWindow.setIcon()

    // and load the index.html of the app.
    mainWindow.loadFile("index.html");

    // Open the DevTools.
    //mainWindow.webContents.openDevTools();

    // Emitted when the window is closed.
    mainWindow.on("closed", function () {
        // Dereference the window object, usually you would store windows
        // in an array if your app supports multi windows, this is the time
        // when you should delete the corresponding element.
        mainWindow = null;
    });
}

autoUpdater.on("checking-for-update", () => {
    log.info("Checking for update...");
});

autoUpdater.on("update-available", (info) => {
    log.info("Update available.");
});

autoUpdater.on("update-not-available", (info) => {
    log.info("Update not available.");
});

autoUpdater.on("error", (err) => {
    log.info("Error in auto-updater. " + err);
});

autoUpdater.on("download-progress", (progressObj) => {
    let logMessage = "Download speed: " + progressObj.bytesPerSecond;
    logMessage = logMessage + " - Downloaded " + progressObj.percent + "%";
    logMessage = logMessage + " (" + progressObj.transferred + "/" + progressObj.total + ")";
    log.info(logMessage);
});

autoUpdater.on("update-downloaded", (info) => {
    log.info("Update downloaded");
});

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on("ready", function() {
    createWindow();
    autoUpdater.checkForUpdatesAndNotify();
});

// Quit when all windows are closed.
app.on("window-all-closed", function () {
    if (process.platform !== "darwin") { // OSX quit fix
        app.quit();
    }
});

app.on("activate", function () {
    // On OS X it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if (mainWindow === null) {
        createWindow();
    }
});