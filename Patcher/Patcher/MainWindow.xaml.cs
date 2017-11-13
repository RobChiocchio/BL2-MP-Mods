using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using IWshRuntimeLibrary;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Popup = System.Windows.MessageBox;

namespace Patcher
{
    public partial class MainWindow : Window
    {
        public static volatile Button buttonPatchStatic;
        public static volatile TaskbarItemInfo taskbarInfoStatic;
        public static volatile ProgressBar progressBarStatic;
        public static BackgroundWorker patcherWorker = new BackgroundWorker(); //replace threading

        public volatile Boolean debug = false; //go through the motions without copying all of the files
        public volatile string gameExec = ""; //init gameExec
        public volatile string gameDir = ""; //init gameDir
        public volatile string cooppatchFile = ""; //init cooppatchFile
        public volatile string path = @"C:\\"; //init default path
        public volatile int gameID; //init game id

        public MainWindow()
        {
            InitializeComponent();

            progressBar.Maximum = 100;
            progressBar.Value = 0;

            buttonPatchStatic = buttonPatch;
            taskbarInfoStatic = taskbarInfo;
            progressBarStatic = progressBar;

            patcherWorker.DoWork += new DoWorkEventHandler(patcherWorker_DoWork);
            patcherWorker.RunWorkerCompleted += patcherWorker_RunWorkerCompleted;
            patcherWorker.ProgressChanged += patcherWorker_ProgressChanged;
            patcherWorker.WorkerReportsProgress = true;
        }

        public void button_Click(object sender, RoutedEventArgs e) // patch borderlands2
        {
            gameID = (comboBoxGame.SelectedIndex + 2); //calculate id of game from index of selected dropdown item

            buttonPatchStatic.IsEnabled = false; //disable patch button
            buttonPatchStatic.Visibility = Visibility.Hidden; //hide the patch button
            taskbarInfoStatic.ProgressState = TaskbarItemProgressState.Normal;
            taskbarInfoStatic.ProgressValue = 0; //reset progress to 0
            progressBarStatic.Visibility = Visibility.Visible; //make visible
            //progressBarStatic.IsIndeterminate = true; //MARQUEE style

            switch (gameID) //depending on game, set variables accordingly
            {
                case 3:
                    gameExec = "BorderlandsPreSequel.exe";
                    gameDir = "BorderlandsPreSequel";
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
            fileDialog.InitialDirectory = @"C:\\Program Files (x86)\\Steam\\SteamApps\\common\\" + gameDir + "\\Binaries\\Win32"; //I guess this isnt working
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

        private void patcherWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBarStatic.Value = e.ProgressPercentage; //loading bar
            taskbarInfoStatic.ProgressValue = e.ProgressPercentage; //taskbar
        }

        private void patcherWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            buttonPatchStatic.IsEnabled = true; //disable patch button
            buttonPatchStatic.Visibility = Visibility.Visible; //hide the patch button
            //progressBarStatic.IsIndeterminate = false; //disable MARQUEE style
            progressBarStatic.Visibility = Visibility.Hidden; //make the loading bar invisible
            taskbarInfoStatic.ProgressState = TaskbarItemProgressState.None; //hide the loading bar in the taskbar
            Popup.Show("Done! A Shortcut was placed on your desktop. Press '~' in game to open up console.");
        }

        public void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) //This function is taken straight from stackoverflow thanks to Konrad Rudolph. Rewrite
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                if (source.FullName != dir.FullName && dir.Name != "server") //prevent infinite copy loop
                {
                    CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
                }
            foreach (FileInfo file in source.GetFiles())
                if (file.Name != "dbghelp.dll") //dbhhelp was causing issued for god knows why
                {
                    try
                    {
                        file.CopyTo(System.IO.Path.Combine(target.FullName, file.Name));
                    }
                    catch (IOException)
                    {
                        //log
                    }
                }
        }

        private void patcherWorker_DoWork(object sender, DoWorkEventArgs e) //the main function
        {
            DirectoryInfo iBL = new DirectoryInfo(path); //bl = path to Borderlands exe
            DirectoryInfo inputDir = new DirectoryInfo(iBL + @"..\\..\\..\\..\\"); //convert to directory - IDK why I need more ..\\s then I actually should but it works so who cares
            DirectoryInfo outputDir = new DirectoryInfo(inputDir + @"\\server"); //convert to directory
            DirectoryInfo oBL = new DirectoryInfo(outputDir + @"\\Binaries\\Win32\\" + gameExec);
            DirectoryInfo iUPK = new DirectoryInfo(inputDir + @"\\WillowGame\\CookedPCConsole\\WillowGame.upk"); // engine = path to WillowGame.upk
            DirectoryInfo oUPK = new DirectoryInfo(outputDir + @"\\WillowGame\\CookedPCConsole\\WillowGame.upk"); // engine = path to WillowGame.upk

            String decompress = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "decompress.exe");

            patcherWorker.ReportProgress(10); //set loading to 10%

            if (System.IO.File.Exists(iBL.FullName)) //if borderlands exec exists
            {
                // -- COPY INPUT TO OUTPUT --
                try
                {
                    if (outputDir.Exists) //if the server folder exists
                    {
                        System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("It looks like a patched version of Borderlands already exists, would you like to replace it?", "ERROR: Output folder already exists!", System.Windows.Forms.MessageBoxButtons.YesNo);

                        switch (dialogResult)
                        {
                            case System.Windows.Forms.DialogResult.Yes:
                                if (!debug) //if not in debug mode
                                {
                                    outputDir.Delete(true); //delete the server folder recursively
                                }
                                break;
                            case System.Windows.Forms.DialogResult.No:
                                break;
                            default: //for safety, I guess
                                break;
                        }
                    }

                    if (!debug) //if not in debug mode
                    {
                        CopyFilesRecursively(inputDir, outputDir);
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
                    catch (IOException)
                    {
                        //log
                    }
                }

                patcherWorker.ReportProgress(50); //set loadingprogress to 50%
                // -- RENAME UPK AND DECOMPRESSEDSIZE --
                try //incase it's already moved
                {
                    System.IO.File.Move(oUPK.FullName + ".uncompressed_size", oUPK.FullName + ".uncompressed_size.bak"); //backup Engine.upk.uncompressed_size
                    System.IO.File.Copy(oUPK.FullName, oUPK.FullName + ".bak"); //backup upk
                }
                catch (IOException)
                {
                    //log
                }

                patcherWorker.ReportProgress(60); //set loadingprogress to 60%
                // -- DECOMPRESS UPK --
                //var decompressing = System.Diagnostics.Process.Start(decompress, "-game=border -out=" + outputDir + @"\\WillowGame\\CookedPCConsole\\ " + iUPK.FullName); //decompress WillowGame.UPK
                var decompressing = System.Diagnostics.Process.Start(decompress, "-game=border -log=decompress.log " + '"' + iUPK.FullName + '"'); //decompress WillowGame.UPK
                decompressing.WaitForExit(); //wait for decompress.exe to finish
                FileInfo decompressedUPK = new FileInfo(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), @"unpacked\\", iUPK.Name));
                try
                {
                    decompressedUPK.CopyTo(oUPK.FullName, true); //move upk to cookedpcconsole
                }
                catch (IOException)
                {
                    Popup.Show("ERROR: Could not find decompressed UPK"); //for debugging
                }

                patcherWorker.ReportProgress(70); //set loadingprogress to 70%
                // -- HEX EDITING --
                switch (gameID)
                {
                    case 3: //tps
                        try
                        {
                            var streamUPKTPS = new FileStream(oUPK.FullName, FileMode.Open, FileAccess.ReadWrite);

                            // -- DEVELOPER MODE --
                            streamUPKTPS.Position = 0x0079ACE7;
                            streamUPKTPS.WriteByte(0x27);

                            // -- EVERY PLAYER GETS THEIR OWN TEAM --
                            streamUPKTPS.Position = 0x0099D50F;
                            streamUPKTPS.WriteByte(0x04);
                            streamUPKTPS.Position = 0x0099D510;
                            streamUPKTPS.WriteByte(0x00);
                            streamUPKTPS.Position = 0x0099D511;
                            streamUPKTPS.WriteByte(0x82);
                            streamUPKTPS.Position = 0x0099D512;
                            streamUPKTPS.WriteByte(0xB1);
                            streamUPKTPS.Position = 0x0099D513;
                            streamUPKTPS.WriteByte(0x00);
                            streamUPKTPS.Position = 0x0099D514;
                            streamUPKTPS.WriteByte(0x00);
                            streamUPKTPS.Position = 0x0099D515;
                            streamUPKTPS.WriteByte(0x06);
                            streamUPKTPS.Position = 0x0099D516;
                            streamUPKTPS.WriteByte(0x44);
                            streamUPKTPS.Position = 0x0099D517;
                            streamUPKTPS.WriteByte(0x00);
                            streamUPKTPS.Position = 0x0099D518;
                            streamUPKTPS.WriteByte(0x04);
                            streamUPKTPS.Position = 0x0099D519;
                            streamUPKTPS.WriteByte(0x24);
                            streamUPKTPS.Position = 0x0099D51A;
                            streamUPKTPS.WriteByte(0x00);

                            streamUPKTPS.Close();
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
                        // -- HEX EDIT UPK --
                        try
                        {
                            var streamUPK2 = new FileStream(oUPK.FullName, FileMode.Open, FileAccess.ReadWrite);

                            // -- DEVELOPER MODE --
                            streamUPK2.Position = 0x006924C7;
                            streamUPK2.WriteByte(0x27);

                            // -- EVERY PLAYER GETS THEIR OWN TEAM --
                            streamUPK2.Position = 0x007F9151;
                            streamUPK2.WriteByte(0x04);
                            streamUPK2.Position = 0x007F9152;
                            streamUPK2.WriteByte(0x00);
                            streamUPK2.Position = 0x007F9153;
                            streamUPK2.WriteByte(0xC6);
                            streamUPK2.Position = 0x007F9154;
                            streamUPK2.WriteByte(0x8B);
                            streamUPK2.Position = 0x007F9155;
                            streamUPK2.WriteByte(0x00);
                            streamUPK2.Position = 0x007F9156;
                            streamUPK2.WriteByte(0x00);
                            streamUPK2.Position = 0x007F9157;
                            streamUPK2.WriteByte(0x06);
                            streamUPK2.Position = 0x007F9158;
                            streamUPK2.WriteByte(0x44);
                            streamUPK2.Position = 0x007F9159;
                            streamUPK2.WriteByte(0x00);
                            streamUPK2.Position = 0x007F915A;
                            streamUPK2.WriteByte(0x04);
                            streamUPK2.Position = 0x007F915B;
                            streamUPK2.WriteByte(0x24);
                            streamUPK2.Position = 0x007F915C;
                            streamUPK2.WriteByte(0x00);
                            streamUPK2.Close();
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
                }

                patcherWorker.ReportProgress(90); //set loadingprogress to 90%
                // -- CREATE SHORTCUT --
                try
                {
                    WshShell shell = new WshShell();
                    IWshRuntimeLibrary.IWshShortcut shortcut = shell.CreateShortcut(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\\" + gameDir + " COOP.lnk") as IWshShortcut;
                    shortcut.Arguments = "-log -debug -codermode -nosplash -exec=" + cooppatchFile;
                    shortcut.TargetPath = oBL.FullName;
                    shortcut.WindowStyle = 1;
                    shortcut.Description = "Robeth's Borderlands COOP patch";
                    shortcut.WorkingDirectory = (Directory.GetParent(oBL.FullName)).FullName;
                    shortcut.IconLocation = (oBL + ",1");
                    shortcut.Save();
                }
                catch (IOException)
                {
                    Popup.Show("ERROR: Failed to create shortcut");
                }

                // -- ENABLE CONSOLE -- RIPPED STRAIGHT FROM BUGWORM's BORDERLANDS2PATCHER!!!!!
                try
                {
                    int i; //for temp[i]
                    string tmpPath = @"\\my games\\" + gameDir + "\\willowgame\\Config\\WillowInput.ini";
                    string iniPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + tmpPath;
                    string[] iniLine = System.IO.File.ReadAllLines(iniPath);
                    for (i = 0; i < iniLine.Length; i++)
                    {
                        if (iniLine[i].StartsWith("ConsoleKey="))
                            break;
                    }
                    iniLine[i] = "ConsoleKey=Tilde";
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
