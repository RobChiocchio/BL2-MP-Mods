using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using System.Net;

using WinForms = System.Windows.Forms;
using Popup = System.Windows.MessageBox;
using System.IO;

namespace Patcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) //This function is taken straight from stackoverflow thanks to Konrad Rudolph. Rewrite
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                if (!(source.FullName == dir.FullName)) //prevent infinite copy loop
                {
                    CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
                }
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(System.IO.Path.Combine(target.FullName, file.Name));
        }

        private void button_Click(object sender, RoutedEventArgs e) // patch borderlands2
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            fileDialog.Filter = "Borderlands|*.exe";
            fileDialog.Title = "Open Borderlands2.exe";
            fileDialog.InitialDirectory = "C:\\Program Files (x86)\\Steam\\SteamApps\\common\\Borderlands 2\\Binaries\\Win32"; //I guess this isnt working
            fileDialog.RestoreDirectory = true; //this either
            var result = fileDialog.ShowDialog();
            string path = "C:\\";
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    path = fileDialog.FileName;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    break;
            }
            DirectoryInfo iBL2 = new DirectoryInfo(path); //bl2 = path to Borderlands2.exe
            DirectoryInfo inputDir = new DirectoryInfo(iBL2 + "..\\..\\"); //convert to directory
            DirectoryInfo outputDir = new DirectoryInfo(inputDir + "\\server"); //convert to directory
            DirectoryInfo oBL2 = new DirectoryInfo(outputDir + "\\Binaries\\Win32\\Borderlands2.exe"); 
            DirectoryInfo iUPK = new DirectoryInfo(inputDir + "\\WillowGame\\CookedPCConsole\\Engine.upk"); // engine = path to Engine.upk
            DirectoryInfo oUPK = new DirectoryInfo(outputDir + "\\WillowGame\\CookedPCConsole\\Engine.upk"); // engine = path to Engine.upk

            String decompress = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "decompress.exe");

            if (System.IO.File.Exists(iBL2.FullName)) //if borderlands2.exe exists
            {
                // -- COPY INPUT TO OUTPUT --
                try
                {
                    CopyFilesRecursively(inputDir, outputDir);

                }
                catch (IOException)
                {
                    //log
                }

                // -- COPY PATCHES TO BINARIES -- 
                using (WebClient myWebClient = new WebClient()) //download file
                {
                    var patchFile = "https://raw.githubusercontent.com/RobethX/BL2-MP-Mods/master/CoopPatch/cooppatch.txt";
                    // Download the Web resource and save it into the current filesystem folder.
                    myWebClient.DownloadFile(patchFile, outputDir.FullName + "\\Binaries\\cooppatch.txt");
                }

                // -- RENAME UPK AND DECOMPRESSEDSIZE --
                try //incase it's already moved
                {
                    System.IO.File.Move(oUPK.FullName, oUPK.FullName + ".bak"); //backup Engine.upk
                    System.IO.File.Move(oUPK.FullName + ".uncompressed_size", oUPK.FullName + ".uncompressed_size.bak"); //backup Engine.upk.uncompressed_size
                }
                catch (IOException)
                {
                    //log
                }

                // -- DECOMPRESS UPK --
                var decompressing = System.Diagnostics.Process.Start(decompress, "-game=border -out=" + outputDir + "/WillowGame/CookedPCConsole/ " + iUPK.FullName); //decompress Engine.UPK
                decompressing.WaitForExit(); //wait for decompress.exe to finish

                // -- HEX EDIT UPK --
                var streamUPK = new FileStream(oUPK.FullName, FileMode.Open, FileAccess.ReadWrite);
                //streamUPK.Position;
                Byte[] replaceUPK = { 0x04, 0x3A, 0x53, 0x38, 0x00, 0x00, 0x04, 0x47 };
                streamUPK.Write(replaceUPK, 4164347, 12);

                // -- HEX EDIT BORDERLANDS2.EXE --
                var streamBL2 = new FileStream(oBL2.FullName, FileMode.Open, FileAccess.ReadWrite);
                streamBL2.Position = 0x004F2590;
                streamBL2.WriteByte(0xff);
                for (long i = 0x01B94B0C; i <= 0x01B94B10; i++)
                {
                    streamBL2.Position = i;
                    streamBL2.WriteByte(0x00);
                }
                streamBL2.Position = 0x01EF16FD; //find upk
                streamBL2.WriteByte(0x78); //willowgame.upk > xillowgame.upk
                streamBL2.Close();

                // -- CREATE SHORTCUT --
                WshShell shell = new WshShell();
                IWshRuntimeLibrary.IWshShortcut shortcut = shell.CreateShortcut(
    Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Borderlands 2 COOP.lnk") as IWshRuntimeLibrary.IWshShortcut;
                shortcut.Arguments = "-log -codermode -debug -nosplash";
                shortcut.TargetPath = oBL2.FullName;
                shortcut.WindowStyle = 1;
                shortcut.Description = "Robeth's Borderlands 2 COOP patch";
                shortcut.WorkingDirectory = (oBL2.FullName + "..\\");
                shortcut.IconLocation = (oBL2 + ",1");
                shortcut.Save();

                // -- ENABLE CONSOLE -- RIPPED STRAIGHT FROM BUGWORM's BORDERLANDS2PATCHER!!!!!
                try
                {
                    string tmppath = "\\my games\\borderlands 2\\willowgame\\Config\\WillowInput.ini";
                    string iniPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + tmppath;
                    string[] temp = System.IO.File.ReadAllLines(path);
                    int i;
                    for (i = 1; i <= temp.Length; i++)
                    {
                        if (temp[i].StartsWith("ConsoleKey="))
                            break;
                    }
                    temp[i] = "ConsoleKey=~";
                    System.IO.File.WriteAllLines(path, temp);
                }
                catch
                {
                    //log
                }
                //END OF BUGWORM'S CODE

                // -- DONE --
                Popup.Show("Done! Press '~' in game to open up console.");
            }
            else
            {
                Popup.Show("Borderlands2.exe not found.");
            }
        }
    }
}
