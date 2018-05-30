using IWshRuntimeLibrary;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using Popup = System.Windows.MessageBox;

namespace Patcher
{
    public partial class MainWindow : Window
    {
        private static BackgroundWorker patcherWorker = new BackgroundWorker(); //replace threading

        private volatile Boolean debug; //go through the motions without copying all of the files
        private volatile string gameExec = ""; //init gameExec
        private volatile string gameDir = ""; //init gameDir
        private volatile string cooppatchFile = ""; //init cooppatchFile
        private volatile string path = @"C:\"; //init default path
        private volatile string consoleKey; //init -- set in button_Click
        private volatile string fileCopying = "files..."; //current file copying
        private volatile int gameID; //init game id
        private volatile ArrayList mods = new ArrayList();
        private readonly double heightDefault = 180;
        private readonly double heightLoading = 115;

        public MainWindow()
        {
            InitializeComponent();
            AdminRelauncher(); //if not in admin mode, relaunch
            progressBar.Maximum = 100;
            progressBar.Value = 0;

            Height = heightDefault; //override just in case

            patcherWorker.DoWork += new DoWorkEventHandler(patcherWorker_DoWork);
            patcherWorker.RunWorkerCompleted += patcherWorker_RunWorkerCompleted;
            patcherWorker.ProgressChanged += patcherWorker_ProgressChanged;
            patcherWorker.WorkerReportsProgress = true;
            patcherWorker.WorkerSupportsCancellation = true;
        }

        private void AdminRelauncher()
        {
            if (!IsRunAsAdmin())
            {
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = Assembly.GetEntryAssembly().CodeBase;

                proc.Verb = "runas";

                try
                {
                    Process.Start(proc);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    Popup.Show("This program must be run as an administrator! \n\n" + ex.ToString());
                }
            }
        }

        private bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public void sceneMenu() //show all main menu elements
        {
            buttonPatch.IsEnabled = true; //disable patch button
            menuClose.IsEnabled = true;
            menuDebug.IsEnabled = true;

            buttonPatch.Visibility = Visibility.Visible; //show the patch button
            comboBoxGame.Visibility = Visibility.Visible;
            comboBoxConsoleKey.Visibility = Visibility.Visible;
            labelConsoleKey.Visibility = Visibility.Visible;
            checkBoxCommunityPatch.Visibility = Visibility.Visible;
            //progressBarStatic.IsIndeterminate = false; //disable MARQUEE style
            progressBar.Visibility = Visibility.Hidden; //make the loading bar invisible
            labelProgressText.Visibility = Visibility.Hidden; //make invisible
            taskbarInfo.ProgressState = TaskbarItemProgressState.None; //hide the loading bar in the taskbar
            Height = heightDefault; //resize back to original size
        }

        public void sceneLoading() //show loading screen elements
        {
            buttonPatch.IsEnabled = false; //disable patch button
            menuClose.IsEnabled = false; //prevent patcher from being closed
            menuDebug.IsEnabled = false; //prevent toggling debug mode after starting patching

            buttonPatch.Visibility = Visibility.Hidden; //hide the patch button
            comboBoxGame.Visibility = Visibility.Hidden;
            comboBoxConsoleKey.Visibility = Visibility.Hidden;
            labelConsoleKey.Visibility = Visibility.Hidden;
            checkBoxCommunityPatch.Visibility = Visibility.Hidden;
            progressBar.Visibility = Visibility.Visible; //make visible
            labelProgressText.Visibility = Visibility.Visible; //make visible
            taskbarInfo.ProgressState = TaskbarItemProgressState.Normal;
            taskbarInfo.ProgressValue = 0; //reset progress to 0
            Height = heightLoading; //shorten window
        }

        private void comboBoxGame_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxGame != null && checkBoxCommunityPatch != null)
            {
                switch (comboBoxGame.SelectedIndex + 1)
                {
                    case 2: //borderlands 2
                        checkBoxCommunityPatch.IsEnabled = true; //enable option for community patch
                        break;

                    default: //other game
                        checkBoxCommunityPatch.IsEnabled = false; //disable community patch option
                        checkBoxCommunityPatch.IsChecked = false; //uncheck
                        break;
                }
            }
        }

        public void buttonPatch_Click(object sender, RoutedEventArgs e) // patch borderlands2
        {
            gameID = (comboBoxGame.SelectedIndex + 1); //calculate id of game from index of selected dropdown item
            debug = menuDebug.IsChecked; //enable debug mode if the menu option is checked

            sceneLoading(); //change window to loading scene

            consoleKey = comboBoxConsoleKey.Text; //console key
            mods.Add(cooppatchFile);
            if (checkBoxCommunityPatch.IsChecked == true && gameID == 2) //Community patch - only with Borderlands 2
            {
                mods.Add("Patch.txt");
            }

            switch (gameID) //depending on game, set variables accordingly
            {
                case 3: //tps
                    gameExec = "BorderlandsPreSequel.exe";
                    gameDir = "BorderlandsPreSequel";
                    cooppatchFile = "cooppatch.txt";
                    break;

                case 1: ///Borderlands 1
                    gameExec = "Borderlands.exe";
                    gameDir = "Borderlands";
                    cooppatchFile = "cooppatch.txt";
                    break;

                default: //2 or incase some how there isnt a variable
                    gameExec = "Borderlands2.exe";
                    gameDir = "Borderlands 2";
                    cooppatchFile = "cooppatch.txt";
                    break;
            }

            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            fileDialog.Filter = "Borderlands|*.exe";
            fileDialog.Title = "Open " + gameExec;
            fileDialog.InitialDirectory = @"C:\Program Files (x86)\Steam\SteamApps\common\" + gameDir + @"\Binaries\Win32"; //I guess this isnt working
            fileDialog.RestoreDirectory = true; //this either
            var result = fileDialog.ShowDialog(); //open file picker dialog

            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    path = fileDialog.FileName;
                    break;

                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    break;
            }

            patcherWorker.RunWorkerAsync(); //run the patch function
        }

        private void menuOptions_Click(object sender, RoutedEventArgs e)
        {
        }

        private void menuExperimental_Click(object sender, RoutedEventArgs e)
        {
            comboItemBL1.IsEnabled = menuExperimental.IsEnabled; //only enable bl1 when experimental mode is on
            comboItemBLTPS.IsEnabled = menuExperimental.IsEnabled; //only enable bl1 when experimental mode is on
        }

        private void menuClose_Click(object sender, RoutedEventArgs e)
        {
            patcherWorker.CancelAsync(); //stop patching process
            try
            {
                Close(); //close program
            }
            catch (Exception)
            {
                //log
            }
        }

        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            Popup.Show("Robeth's Unlimited COOP Mod & Robeth's Unlimited COOP Patch made by Rob 'Robeth' Chiocchio.", "About");
        }

        private void menuReportBug_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://steamcommunity.com/groups/bl2unlimitedcoop/discussions/0/1479856439030633321/"); //issues post in Steam group
        }

        private void menuHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://steamcommunity.com/sharedfiles/filedetails/?id=1151711689"); //Steam Guide
        }

        private void menuLFG_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://steamcommunity.com/groups/bl2unlimitedcoop/discussions/0/1488866180610449590/"); //LFG post in Steam group
        }

        private void patcherWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage; //loading bar
            taskbarInfo.ProgressValue = (double)e.ProgressPercentage / 100; //taskbar

            switch (e.ProgressPercentage)
            {
                case 5:
                    labelProgressText.Content = "Removing old files";
                    break;

                case 10:
                    labelProgressText.Content = "Copying " + fileCopying; //current file copying
                    break;

                case 40:
                    labelProgressText.Content = "Downloading patches";
                    break;

                case 45:
                    labelProgressText.Content = "Installing DLL loader";
                    break;

                case 50:
                    labelProgressText.Content = "EXPLOSIONS?!?!?!?!";//"Hacking your Minecraft account";
                    break;

                case 60:
                    labelProgressText.Content = "Decompressing some stuff";
                    break;

                case 70:
                    labelProgressText.Content = "Making a sandwich";
                    break;

                case 75:
                    labelProgressText.Content = "Norton sucks";//"Installing viruses";
                    break;

                case 80:
                    labelProgressText.Content = "Recombobulation the flux capacitor";
                    break;

                case 90:
                    labelProgressText.Content = "Climaxing";
                    break;

                case 100:
                    labelProgressText.Content = "All done";
                    Popup.Show("Done! A Shortcut was placed on your desktop. Press '~' in game to open up console.", "Info");
                    break;

                default:
                    //default
                    break;
            }
        }

        private void patcherWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            sceneMenu(); //restore menu window
        }

        //private void pat

        public void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) //This function is taken straight from stackoverflow thanks to Konrad Rudolph. Rewrite
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                if (source.FullName != dir.FullName && source.Name != "server") //prevent infinite copy loop
                {
                    CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
                }
            }
            foreach (FileInfo file in source.GetFiles())
            {
                if (file.Name != "dbghelp.dll") //dbhhelp was causing issued for god knows why
                {
                    try
                    {
                        file.CopyTo(System.IO.Path.Combine(target.FullName, file.Name));
                        fileCopying = file.Name; //update the current copying file with the name of the file being copied
                        patcherWorker.ReportProgress(10);
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("ERROR: Could not copy file " + file.Name);
                    }
                }
            }
        }

        private void patcherWorker_DoWork(object sender, DoWorkEventArgs e) //the main function
        {
            DirectoryInfo iBL = new DirectoryInfo(path); //bl = path to Borderlands exe
            DirectoryInfo inputDir = new DirectoryInfo(iBL + @"..\..\..\..\"); //convert to directory
            DirectoryInfo outputDir = new DirectoryInfo(inputDir + @"\server"); //convert to directory

            DirectoryInfo oBL;
            DirectoryInfo iWillowGame;
            DirectoryInfo oWillowGame;
            DirectoryInfo iEngine;
            DirectoryInfo oEngine;

            if (gameID == 1) //if Borderlands 1
            {
                oBL = new DirectoryInfo(outputDir + @"\Binaries\" + gameExec);
                iWillowGame = new DirectoryInfo(inputDir + @"\WillowGame\CookedPC\WillowGame.u"); // path to WillowGame.upk
                oWillowGame = new DirectoryInfo(outputDir + @"\WillowGame\CookedPC\WillowGame.u"); // path to WillowGame.upk
                iEngine = new DirectoryInfo(inputDir + @"\WillowGame\CookedPC\Engine.u"); // path to Engine.upk
                oEngine = new DirectoryInfo(outputDir + @"\WillowGame\CookedPC\Engine.u"); // path to Engine.upk
            }
            else
            {
                oBL = new DirectoryInfo(outputDir + @"\Binaries\Win32\" + gameExec);
                iWillowGame = new DirectoryInfo(inputDir + @"\WillowGame\CookedPCConsole\WillowGame.upk"); // path to WillowGame.upk
                oWillowGame = new DirectoryInfo(outputDir + @"\WillowGame\CookedPCConsole\WillowGame.upk"); // path to WillowGame.upk
                iEngine = new DirectoryInfo(inputDir + @"\WillowGame\CookedPCConsole\Engine.upk"); // path to Engine.upk
                oEngine = new DirectoryInfo(outputDir + @"\WillowGame\CookedPCConsole\Engine.upk"); // path to Engine.upk
            }

            String decompress = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), @"bin\decompress.exe");
            Boolean skipCopy = false;

            patcherWorker.ReportProgress(10); //set loading to 10%

            if (System.IO.File.Exists(iBL.FullName)) //if borderlands exec exists
            {
                // -- COPY INPUT TO OUTPUT --
                try
                {
                    if (outputDir.Exists) //if the server folder exists
                    {
                        System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("It looks like a patched version of Borderlands already exists, would you like to update it? Clicking 'Yes' will attempt to update the existing files, clicking 'No' will overwrite the old patch data, and 'Cancel' will stop the patcher altogether.", "ERROR: Output folder already exists!", System.Windows.Forms.MessageBoxButtons.YesNoCancel);

                        switch (dialogResult)
                        {
                            case System.Windows.Forms.DialogResult.Yes:
                                skipCopy = true;//skip the copy
                                break;

                            case System.Windows.Forms.DialogResult.No:
                                if (!debug && !skipCopy) //if not in debug mode
                                {
                                    patcherWorker.ReportProgress(5); //set loadingprogress to 5%
                                    outputDir.Delete(true); //delete the server folder recursively
                                    patcherWorker.ReportProgress(10); //set loadingprogress to 10%
                                }
                                skipCopy = false;//continue
                                break;

                            default: //case System.Windows.Forms.DialogResult.Cancel:
                                skipCopy = true;
                                patcherWorker.CancelAsync(); //cancel
                                patcherWorker.Dispose();
                                Close(); //terminate thread
                                break;
                        }
                    }

                    if (!debug && !skipCopy) //if not in debug mode
                    {
                        CopyFilesRecursively(inputDir, outputDir); //backup borderlands to server subdir
                    }
                }
                catch (IOException)
                {
                    Popup.Show("ERROR: Cannot copy Borderlands. Does it already exist?");
                }

                patcherWorker.ReportProgress(40); //set loadingprogress to 40%
                // -- COPY PATCHES TO BINARIES --
                using (WebClient myWebClient = new WebClient()) //download file
                {
                    try
                    {
                        myWebClient.DownloadFile("https://raw.githubusercontent.com/RobethX/BL2-MP-Mods/master/CoopPatch/" + cooppatchFile, outputDir.FullName + @"\Binaries\" + cooppatchFile);
                    }
                    catch (WebException)
                    {
                        //log
                    }

                    //Community patch
                    if (mods.Contains("Patch.txt"))
                    {
                        try
                        {
                            myWebClient.DownloadFile("https://www.dropbox.com/s/kxvf8w3ul4zuh93/Patch.txt", outputDir.FullName + @"\Binaries\Patch.txt");
                        }
                        catch (WebException)
                        {
                            //log
                        }
                    }
                }

                if (!(System.IO.File.Exists(outputDir.FullName + @"\Binaries\Win32\binkw23.dll"))) //if the patch is not already installed
                {
                    patcherWorker.ReportProgress(45); //set loadingprogress to 45%

                    DirectoryInfo iPlugins = new DirectoryInfo(@"bin\Plugins");
                    DirectoryInfo oPlugins = new DirectoryInfo(outputDir + @"\Binaries\Plugins");

                    System.IO.File.Move(outputDir.FullName + @"\Binaries\Win32\binkw32.dll", outputDir.FullName + @"\Binaries\Win32\binkw23.dll"); //rename binkw32 to binkw23
                    System.IO.File.Copy(@"bin\bink32.dll", outputDir.FullName + @"\Binaries\Win32\binkw32.dll", true); //copy patched dll to win32 - overwrite
                    CopyFilesRecursively(iPlugins, oPlugins); //copy plugins to borderlands
                }

                patcherWorker.ReportProgress(50); //set loadingprogress to 50%
                                                  // -- RENAME UPK AND DECOMPRESSEDSIZE --
                try //incase it's already moved
                {
                    System.IO.File.Move(oWillowGame.FullName + ".uncompressed_size", oWillowGame.FullName + ".uncompressed_size.bak"); //backup WillowGame.upk.uncompressed_size
                    System.IO.File.Copy(oWillowGame.FullName, oWillowGame.FullName + ".bak"); //backup upk
                    System.IO.File.Move(oEngine.FullName + ".uncompressed_size", oEngine.FullName + ".uncompressed_size.bak"); //backup Engine.upk.uncompressed_size
                    System.IO.File.Copy(oEngine.FullName, oEngine.FullName + ".bak"); //backup upk
                }
                catch (IOException)
                {
                    //log
                }

                patcherWorker.ReportProgress(60); //set loadingprogress to 60%

                // -- DECOMPRESS UPK --

                //var decompressing = System.Diagnostics.Process.Start(decompress, "-game=border -out=" + outputDir + @"\WillowGame\CookedPCConsole\ " + iUPK.FullName); //decompress WillowGame.UPK
                var decompressingWillowGame = System.Diagnostics.Process.Start(decompress, "-game=border -log=decompress.log " + '"' + iWillowGame.FullName + '"'); //decompress WillowGame.UPK
                decompressingWillowGame.WaitForExit(); //wait for decompress.exe to finish
                var decompressingEngine = System.Diagnostics.Process.Start(decompress, "-game=border -log=decompress.log " + '"' + iEngine.FullName + '"'); //decompress Engine.UPK
                decompressingEngine.WaitForExit(); //wait for decompress.exe to finish
                FileInfo decompressedWillowGame = new FileInfo(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), @"unpacked\", iWillowGame.Name));
                FileInfo decompressedEngine = new FileInfo(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), @"unpacked\", iEngine.Name));

                try
                {
                    decompressedWillowGame.CopyTo(oWillowGame.FullName, true); //move upk to cookedpcconsole
                    decompressedEngine.CopyTo(oEngine.FullName, true); //move upk to cookedpcconsole
                }
                catch (IOException)
                {
                    Popup.Show("ERROR: Could not find decompressed UPK"); //for debugging
                }

                // -- DELETE UNPACKED FOLDER --

                try
                {
                    Directory.Delete(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), @"unpacked\"), true); //delete Unpacked folder recursively
                }
                catch (IOException)
                {
                    //log
                }

                patcherWorker.ReportProgress(70); //set loadingprogress to 70%

                // -- HEX EDITING --

                switch (gameID)
                {
                    case 3: //tps
                        try
                        {
                            var streamWillowGameUPKTPS = new FileStream(oWillowGame.FullName, FileMode.Open, FileAccess.ReadWrite);

                            // -- DEVELOPER MODE --
                            streamWillowGameUPKTPS.Position = 0x0079ACE7;
                            streamWillowGameUPKTPS.WriteByte(0x27);

                            // -- EVERY PLAYER GETS THEIR OWN TEAM --
                            streamWillowGameUPKTPS.Position = 0x0099D50F;
                            streamWillowGameUPKTPS.WriteByte(0x04);
                            streamWillowGameUPKTPS.Position = 0x0099D510;
                            streamWillowGameUPKTPS.WriteByte(0x00);
                            streamWillowGameUPKTPS.Position = 0x0099D511;
                            streamWillowGameUPKTPS.WriteByte(0x82);
                            streamWillowGameUPKTPS.Position = 0x0099D512;
                            streamWillowGameUPKTPS.WriteByte(0xB1);
                            streamWillowGameUPKTPS.Position = 0x0099D513;
                            streamWillowGameUPKTPS.WriteByte(0x00);
                            streamWillowGameUPKTPS.Position = 0x0099D514;
                            streamWillowGameUPKTPS.WriteByte(0x00);
                            streamWillowGameUPKTPS.Position = 0x0099D515;
                            streamWillowGameUPKTPS.WriteByte(0x06);
                            streamWillowGameUPKTPS.Position = 0x0099D516;
                            streamWillowGameUPKTPS.WriteByte(0x44);
                            streamWillowGameUPKTPS.Position = 0x0099D517;
                            streamWillowGameUPKTPS.WriteByte(0x00);
                            streamWillowGameUPKTPS.Position = 0x0099D518;
                            streamWillowGameUPKTPS.WriteByte(0x04);
                            streamWillowGameUPKTPS.Position = 0x0099D519;
                            streamWillowGameUPKTPS.WriteByte(0x24);
                            streamWillowGameUPKTPS.Position = 0x0099D51A;
                            streamWillowGameUPKTPS.WriteByte(0x00);

                            streamWillowGameUPKTPS.Close();
                        }
                        catch (IOException)
                        {
                            Popup.Show("ERROR: Could not modify upk files");
                        }

                        patcherWorker.ReportProgress(75); //set loadingprogress to 75%

                        // -- HEX EDIT ENGINE.UPK --

                        try
                        {
                            var streamEngineTPS = new FileStream(oBL.FullName, FileMode.Open, FileAccess.ReadWrite);
                            // -- DON'T UPDATE PLAYERCOUNT --

                            //streamEngineTPS.Position = 0x003F69A4;
                            //streamEngineTPS.WriteByte(0x1E);
                            streamEngineTPS.Close();
                        }
                        catch (IOException)
                        {
                            Popup.Show("ERROR: Could not modify upk files");
                        }

                        patcherWorker.ReportProgress(80); //set loadingprogress to 80%

                        // -- HEX EDIT BORDERLANDSPRESEQUEL.EXE --

                        try
                        {
                            var streamBLTPS = new FileStream(oBL.FullName, FileMode.Open, FileAccess.ReadWrite);
                            streamBLTPS.Position = 0x00D8BD1F;
                            streamBLTPS.WriteByte(0xFF);
                            for (long i = 0x018E9C33; i <= 0x018E9C39; i++)
                            {
                                streamBLTPS.Position = i;
                                streamBLTPS.WriteByte(0x00);
                            }
                            streamBLTPS.Position = 0x01D3D699; //find willowgame.upk
                            streamBLTPS.WriteByte(0x78); //willowgame.upk > xillowgame.upk
                            streamBLTPS.Close();
                        }
                        catch (IOException)
                        {
                            Popup.Show("ERROR: Could not modify executable");
                        }
                        break;

                    case 2: //bl2

                        // -- HEX EDIT WILLOWGAME --

                        try
                        {
                            var streamWillowGame2 = new FileStream(oWillowGame.FullName, FileMode.Open, FileAccess.ReadWrite);

                            // -- DEVELOPER MODE --
                            streamWillowGame2.Position = 0x006924C7;
                            streamWillowGame2.WriteByte(0x27);

                            // -- EVERY PLAYER GETS THEIR OWN TEAM --
                            streamWillowGame2.Position = 0x007F9151;
                            streamWillowGame2.WriteByte(0x04);
                            streamWillowGame2.Position = 0x007F9152;
                            streamWillowGame2.WriteByte(0x00);
                            streamWillowGame2.Position = 0x007F9153;
                            streamWillowGame2.WriteByte(0xC6);
                            streamWillowGame2.Position = 0x007F9154;
                            streamWillowGame2.WriteByte(0x8B);
                            streamWillowGame2.Position = 0x007F9155;
                            streamWillowGame2.WriteByte(0x00);
                            streamWillowGame2.Position = 0x007F9156;
                            streamWillowGame2.WriteByte(0x00);
                            streamWillowGame2.Position = 0x007F9157;
                            streamWillowGame2.WriteByte(0x06);
                            streamWillowGame2.Position = 0x007F9158;
                            streamWillowGame2.WriteByte(0x44);
                            streamWillowGame2.Position = 0x007F9159;
                            streamWillowGame2.WriteByte(0x00);
                            streamWillowGame2.Position = 0x007F915A;
                            streamWillowGame2.WriteByte(0x04);
                            streamWillowGame2.Position = 0x007F915B;
                            streamWillowGame2.WriteByte(0x24);
                            streamWillowGame2.Position = 0x007F915C;
                            streamWillowGame2.WriteByte(0x00);

                            // -- PREVENT MENU FROM CANCELLING FAST TRAVEL --
                            streamWillowGame2.Position = 0x006BEAF6;
                            streamWillowGame2.WriteByte(0x27);

                            // -- MORE CACHED PLAYERS --
                            streamWillowGame2.Position = 0x00832B20;
                            streamWillowGame2.WriteByte(0x39);

                            streamWillowGame2.Close();
                        }
                        catch (IOException)
                        {
                            Popup.Show("ERROR: Could not modify upk files");
                        }

                        patcherWorker.ReportProgress(75); //set loadingprogress to 75%

                        // -- HEX EDIT ENGINE.UPK --

                        try
                        {
                            var streamEngine2 = new FileStream(oBL.FullName, FileMode.Open, FileAccess.ReadWrite);

                            // -- EffectiveNumPlayers --> NumSpectators --
                            /*
                            streamEngine2.Position = 0x003F699F;
                            streamEngine2.WriteByte(0x22);
                            streamEngine2.Position = 0x003F7085;
                            streamEngine2.WriteByte(0x22);
                            */
                            streamEngine2.Position = 0x003FC015;
                            streamEngine2.WriteByte(0x22);

                            // -- NumPlayers --> EffectiveNumPlayers --
                            /*
                            streamEngine2.Position = 0x003F69A4;
                            streamEngine2.WriteByte(0x1E);
                            streamEngine2.Position = 0x003F708A;
                            streamEngine2.WriteByte(0x1E);
                            */
                            streamEngine2.Position = 0x003FC01A;
                            streamEngine2.WriteByte(0x1E);

                            streamEngine2.Close();
                        }
                        catch (IOException)
                        {
                            Popup.Show("ERROR: Could not modify upk files");
                        }

                        patcherWorker.ReportProgress(80); //set loadingprogress to 80%

                        // -- HEX EDIT BORDERLANDS2.EXE --
                        try
                        {
                            var streamBL2 = new FileStream(oBL.FullName, FileMode.Open, FileAccess.ReadWrite);
                            streamBL2.Position = 0x004F2590;
                            streamBL2.WriteByte(0xFF);
                            for (long i = 0x01B94B0C; i <= 0x01B94B10; i++)
                            {
                                streamBL2.Position = i;
                                streamBL2.WriteByte(0x00);
                            }
                            streamBL2.Position = 0x01EF17F9; //find upk
                            streamBL2.WriteByte(0x78); //willowgame.upk > xillowgame.upk
                            streamBL2.Close();
                        }
                        catch (IOException)
                        {
                            Popup.Show("ERROR: Could not modify executable");
                        }
                        break;

                    case 1: //bl1

                        // -- HEX EDIT WILLOWGAME.U --

                        /*
                        try
                        {
                            var streamWillowGame1 = new FileStream(oWillowGame.FullName, FileMode.Open, FileAccess.ReadWrite);

                            streamWillowGame1.Close();
                        }
                        catch (IOException)
                        {
                            Popup.Show("ERROR: Could not modify upk files");
                        }
                        */

                        patcherWorker.ReportProgress(75); //set loadingprogress to 75%

                        // -- HEX EDIT ENGINE.U --

                        try
                        {
                            var streamEngine1 = new FileStream(oBL.FullName, FileMode.Open, FileAccess.ReadWrite);

                            // -- Enable Console --
                            streamEngine1.Position = 0x00396938;
                            streamEngine1.WriteByte(0x06);
                            streamEngine1.Position = 0x00396939;
                            streamEngine1.WriteByte(0x52);
                            streamEngine1.Close();
                        }
                        catch (IOException)
                        {
                            Popup.Show("ERROR: Could not modify upk files");
                        }

                        patcherWorker.ReportProgress(80); //set loadingprogress to 80%

                        // -- HEX EDIT BORDERLANDS.EXE --

                        /*
                        try
                        {
                            var streamBL1 = new FileStream(oBL.FullName, FileMode.Open, FileAccess.ReadWrite);
                            streamBL1.Position = 0x004F2590;
                            streamBL1.WriteByte(0xFF);
                            for (long i = 0x01B94B0C; i <= 0x01B94B10; i++)
                            {
                                streamBL1.Position = i;
                                streamBL1.WriteByte(0x00);
                            }
                            streamBL1.Position = 0x01EF17F9; //find upk
                            streamBL1.WriteByte(0x78); //willowgame.upk > xillowgame.upk
                            streamBL1.Close();
                        }
                        catch (IOException)
                        {
                            Popup.Show("ERROR: Could not modify executable");
                        }
                        */
                        break;

                    default: //if not bl1, bl2, or tps
                             //log
                        break;
                }

                patcherWorker.ReportProgress(90); //set loadingprogress to 90%

                // -- CREATE SHORTCUT --

                try
                {
                    string shortcutName = @"\" + gameDir + " - Robeth's Unlimited COOP Mod.lnk";
                    WshShell shell = new WshShell();
                    IWshRuntimeLibrary.IWshShortcut shortcut = shell.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + shortcutName) as IWshShortcut;

                    shortcut.Arguments = "-log";
                    shortcut.TargetPath = oBL.FullName;
                    shortcut.WindowStyle = 1;
                    shortcut.Description = "Robeth's Borderlands COOP patch";
                    shortcut.WorkingDirectory = (Directory.GetParent(oBL.FullName)).FullName;
                    shortcut.IconLocation = (oBL + ",1");
                    shortcut.Save();

                    string appStartMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "Robeth's Borderlands COOP Mod");
                    if (!Directory.Exists(appStartMenuPath))
                    {
                        Directory.CreateDirectory(appStartMenuPath);
                    }
                    System.IO.File.Copy((Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + shortcutName), (appStartMenuPath + shortcutName), true); //copy shortcut from desktop to shell : programs and overwrite old version
                }
                catch (IOException)
                {
                    Popup.Show("ERROR: Failed to create shortcut");
                }

                // -- ENABLE CONSOLE -- RIPPED STRAIGHT FROM BUGWORM's BORDERLANDS2PATCHER!!!!!

                try
                {
                    int i; //for temp[i]
                    string tmpPath = @"\my games\" + gameDir + @"\willowgame\Config\WillowInput.ini";
                    string iniPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + tmpPath;
                    string[] iniLine = System.IO.File.ReadAllLines(iniPath);
                    for (i = 0; i < iniLine.Length; i++)
                    {
                        if (iniLine[i].StartsWith("ConsoleKey="))
                        {
                            break;
                        }
                    }
                    iniLine[i] = "ConsoleKey=" + consoleKey;
                    System.IO.File.WriteAllLines(iniPath, iniLine);
                }
                catch (IOException)
                {
                    Popup.Show("ERROR: Failed to enable console");
                }
                //END OF BUGWORM'S CODE

                // -- DONE --
                patcherWorker.ReportProgress(100); //set loadingprogress to 100%
            }
            else
            {
                Popup.Show("ERROR: " + gameExec + " not found.");
            }
        }
    }
}