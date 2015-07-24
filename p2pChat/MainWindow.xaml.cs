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
        private Talker _talker;

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

        private void button_Connect_Click(object sender, RoutedEventArgs e) {
            _talker = new Talker(_settings.Host, textBlock_Log);
            button_Connect.IsEnabled = false;
            button_Disconnect.IsEnabled = true;
            button_Send.IsEnabled = true;
        }

        private void button_Disconnect_Click(object sender, RoutedEventArgs e) {
            _talker.Dispose();
            _talker = null;
            button_Connect.IsEnabled = true;
            button_Disconnect.IsEnabled = false;
            button_Send.IsEnabled = false;
        }

        private void button_Send_Click(object sender, RoutedEventArgs e) {
            if (_talker == null) {
                return;
            }
            _talker.Talk(textBox_Name.Text + "\t" + textBox_Message.Text);
            textBox_Message.Text = null;
        }
    }
}
