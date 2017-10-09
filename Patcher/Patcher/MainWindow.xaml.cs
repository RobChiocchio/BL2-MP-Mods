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

        static Microsoft.Win32.RegistryKey b2il = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64).OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 49520"); //get BL2 install dir from registry

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

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void button1_Click(object sender, RoutedEventArgs e) //choose input directory button
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    textBoxInputDir.Text = file;
                    textBoxInputDir.ToolTip = file;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    textBoxInputDir.Text = null;
                    textBoxInputDir.ToolTip = null;
                    break;
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e) //choose output directory button
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    textBoxOutputDir.Text = file;
                    textBoxOutputDir.ToolTip = file;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    textBoxOutputDir.Text = null;
                    textBoxOutputDir.ToolTip = null;
                    break;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e) // patch borderlands2
        {
            DirectoryInfo inputDir = new DirectoryInfo(textBoxInputDir.Text); //convert to directory
            DirectoryInfo outputDir = new DirectoryInfo(textBoxOutputDir.Text); //convert to directory
            DirectoryInfo bl2 = new DirectoryInfo("/Binaries/Win32/Borderlands2.exe"); //bl2 = path to Borderlands2.exe
            DirectoryInfo upk = new DirectoryInfo("/WillowGame/CookedPCConsole/Engine.upk"); // engine = path to Engine.upk

            if (File.Exists(inputDir + bl2.FullName)) //if borderlands2.exe exists
            {
                // -- COPY INPUT TO OUTPUT --
                try
                {
                    CopyFilesRecursively(inputDir, outputDir);

                }
                catch (IOException)
                {
                    Popup.Show("Error: ");
                }

                // -- COPY PATCHES TO BINARIES --
                File.Copy("../CoopPatch/cooppatch.txt", outputDir + "/Binaries/cooppatch.txt", false);

                // -- RENAME UPK AND DECOMPRESSEDSIZE --
                System.IO.File.Move(outputDir + upk.FullName, outputDir + upk.FullName + ".bak"); //backup Engine.upk
                System.IO.File.Move(outputDir + upk.FullName + ".uncompressed_size", outputDir + upk.FullName + ".uncompressed_size.bak"); //backup Engine.upk.uncompressed_size

                // -- DECOMPRESS UPK --
                System.Diagnostics.Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location + "decompress.exe", "-game=border -out=" + outputDir + "/WillowGame/CookedPCConsole/ " + inputDir + upk.FullName); //decompress Engine.UPK

                // -- HEX EDIT UPK --
                var streamUPK = new FileStream(outputDir + upk.FullName, FileMode.Open, FileAccess.ReadWrite);
                //streamUPK.Position;
                //streamUPK.Write([0xff, 0xff, 0xff, 0xff], );

                // -- HEX EDIT BORDERLANDS2.EXE --
                var streamBL2 = new FileStream(outputDir + bl2.FullName, FileMode.Open, FileAccess.ReadWrite);
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
