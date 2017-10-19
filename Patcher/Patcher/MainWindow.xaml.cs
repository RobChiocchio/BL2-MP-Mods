using System;
using System.Windows;
using IWshRuntimeLibrary;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Popup = System.Windows.MessageBox;

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

            //Thread tGUI = Thread.CurrentThread;
            //tGUI.Name = "GUIThread";
        }

        private void button_Click(object sender, RoutedEventArgs e) // patch borderlands2
        {
            buttonPatch.IsEnabled = false; //disable button
            //main.patch(); //run the patch function
            ThreadStart patcher = new ThreadStart(main.patch);
            Thread patcherThread = new Thread(patcher);
            patcherThread.SetApartmentState(ApartmentState.STA);
            patcherThread.Start(); //run the patch function
            buttonPatch.IsEnabled = true; //enable button when done
        }
    }
}
