using System;
using System.Windows;
using IWshRuntimeLibrary;
using System.Net;
using System.IO;
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
        }

        private async void button_Click(object sender, RoutedEventArgs e) // patch borderlands2
        {
            buttonPatch.IsEnabled = false; //disable button
            string patchResult = await main.patch(); //run the patch function
            buttonPatch.IsEnabled = true; //enable button when done
        }
    }
}
