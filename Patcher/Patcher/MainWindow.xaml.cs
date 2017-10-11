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

using WinForms = System.Windows.Forms;
using Popup = System.Windows.MessageBox;
using System.IO;

namespace CoopPatcher
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

        static Microsoft.Win32.RegistryKey InstallLocation = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64).OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 49520"); //get BL2 install dir from registry

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
            DirectoryInfo inputDir = new DirectoryInfo(InstallLocation.GetValue("InstallLocation") as string); //convert to directory
            DirectoryInfo outputDir = new DirectoryInfo(inputDir + "/server"); //convert to directory
            DirectoryInfo iBL2 = new DirectoryInfo(inputDir + "/Binaries/Win32/Borderlands2.exe"); //bl2 = path to Borderlands2.exe
            DirectoryInfo oBL2 = new DirectoryInfo(outputDir + "/Binaries/Win32/Borderlands2.exe"); //bl2 = path to Borderlands2.exe
            DirectoryInfo iUPK = new DirectoryInfo(inputDir + "/WillowGame/CookedPCConsole/Engine.upk"); // engine = path to Engine.upk
            DirectoryInfo oUPK = new DirectoryInfo(outputDir + "/WillowGame/CookedPCConsole/Engine.upk"); // engine = path to Engine.upk

            String decompress = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "decompress.exe");

            if (File.Exists(iBL2.FullName)) //if borderlands2.exe exists
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
                //File.Copy("../CoopPatch/cooppatch.txt", outputDir + "/Binaries/cooppatch.txt", false);

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

                // -- DONE --
                Popup.Show("Done!");
            }
            else
            {
                Popup.Show("Borderlands2.exe not found.");
            }
        }

        private void desktopShortcut() //copy shortcut to desktop
        {
            // -- COPY SHORTCUT TO DESKTOP --

        }
 
        private void enableConsole() //add console hotkey to config files
        {
            // -- BACKUP CONFIG --

            // -- EDIT CONFIG --

        }

        private void patchHexEdit() //patch Borderlands2.exe and UPKs
        {

        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void checkBox2_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
