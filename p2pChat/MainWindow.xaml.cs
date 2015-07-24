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
            textBox_Host.Text = _settings.Host;
            _listener = new Listener(textBlock_Log);
            _listener.Start();
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            if (_listener.IsBusy) {
                _listener.Stop();
            }
        }
    }
}
