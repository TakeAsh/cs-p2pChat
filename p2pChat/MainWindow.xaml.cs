using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TakeAshUtility;

namespace p2pChat {

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow :
        Window {

        private static Properties.Settings _settings = Properties.Settings.Default;

        private Listener _listener;

        public MainWindow() {
            InitializeComponent();
            /*
            IPAddress serverAddress;
            if (!IPAddress.TryParse(_settings.Host, out serverAddress)) {
                textBlock_Status.Text = "Invalid Host";
                return;
            }
            */
            _listener = new Listener(_settings.Port, textBlock_Log);
            _listener.Start();
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            if (_listener.IsBusy) {
                _listener.Stop();
            }
        }
    }
}
