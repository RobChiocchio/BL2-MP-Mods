'use strict'; // don't ignore bad code

const electron = require('electron');
const app = electron.app; // control app
const BrowserWindow = electron.BrowserWindow; // create native browser window
const ipcMain = electron.ipcMain;

if (require('electron-squirrel-startup')) return;

require('update-electron-app'); //auto update based off repo in package.json

// Keep a global reference of the window object, if you don't, the window will
// be closed automatically when the JavaScript object is garbage collected.
let mainWindow

function createWindow() {
  // Create the browser window.
  mainWindow = new BrowserWindow({
    width: 360,
    height: 200,
    minWidth: 360,
    minHeight: 200,
    frame: false, // remove frame from windows apps
    titleBarStyle: 'hidden', // hide mac titlebar
    transparent: true, //allow rounded corners
    icon: 'images/icon.png',
  });

  //mainWindow.setIcon()

  // and load the index.html of the app.
  mainWindow.loadFile('index.html');

  // Open the DevTools.
  //mainWindow.webContents.openDevTools();

  // Emitted when the window is closed.
  mainWindow.on('closed', function () {
    // Dereference the window object, usually you would store windows
    // in an array if your app supports multi windows, this is the time
    // when you should delete the corresponding element.
    mainWindow = null;
  });
}

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on('ready', createWindow);

// Quit when all windows are closed.
app.on('window-all-closed', function () {
  if (process.platform !== 'darwin') { // OSX quit fix
    app.quit();
  }
});

app.on('activate', function () {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (mainWindow === null) {
    createWindow();
  }
});