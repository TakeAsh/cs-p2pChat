using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
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
            group_Config.Visibility = Visibility.Collapsed;
            textBox_Port.Text = _settings.Port.ToString();
            textBox_Name.Text = _settings.MyName.ToDefaultIfNullOrEmpty(Dns.GetHostName());
            _listener = new Listener(textBlock_Log);
        }

        private void ShowMessage(string message) {
            textBlock_Log.Text += message + "\n";
        }

        private void ToggleListener() {
            if (_listener.IsBusy) {
                _listener.Stop();
                image_ListenStatus.Source = ResourceHelper.GetImage("Images/Wait.png");
            } else {
                try {
                    _listener.Start();
                    image_ListenStatus.Source = ResourceHelper.GetImage("Images/Play.png");
                }
                catch (Exception ex) {
                    ShowMessage(ex.GetAllMessages());
                }
            }
        }

        private void Disconnect() {
            ShowMessage("Disconnect: " + _talker.Host + ":" + _talker.Port);
            _talker.Dispose();
            _talker = null;
            button_Connect.IsEnabled = true;
            button_Disconnect.IsEnabled = false;
            button_Send.IsEnabled = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            if (_listener.IsBusy) {
                _listener.Dispose();
            }
        }

        private void button_Connect_Click(object sender, RoutedEventArgs e) {
            try {
                var uri = ("tcp://" + textBox_Host.Text).TryParse<Uri>();
                var host = !String.IsNullOrEmpty(uri.Host) ?
                    uri.Host :
                    textBox_Host.Text;
                var port = uri.Port > 0 ?
                    uri.Port :
                    _settings.Port;
                _talker = new Talker(host, port, textBlock_Log);
                _settings.Host = textBox_Host.Text;
                _settings.Save();
                button_Connect.IsEnabled = false;
                button_Disconnect.IsEnabled = true;
                button_Send.IsEnabled = true;
                ShowMessage("Connect: " + host + ":" + port);
            }
            catch (Exception ex) {
                ShowMessage(ex.GetAllMessages());
            }
        }

        private void button_Disconnect_Click(object sender, RoutedEventArgs e) {
            Disconnect();
        }

        private void button_Config_Click(object sender, RoutedEventArgs e) {
            if (group_Config.Visibility == Visibility.Collapsed) {
                group_Config.Visibility = Visibility.Visible;
            } else {
                group_Config.Visibility = Visibility.Collapsed;
                _settings.Port = textBox_Port.Text.TryParse(_settings.Port);
                _settings.Save();
            }
        }

        private void button_Send_Click(object sender, RoutedEventArgs e) {
            if (_talker == null ||
                String.IsNullOrEmpty(textBox_Name.Text) ||
                String.IsNullOrEmpty(textBox_Message.Text)) {
                return;
            }
            try {
                _talker.Talk(textBox_Name.Text + "\t" + textBox_Message.Text);
                textBox_Message.Text = null;
                _settings.MyName = textBox_Name.Text;
                _settings.Save();
            }
            catch (Exception ex) {
                ShowMessage(ex.Message);
                Disconnect();
            }
        }

        private void button_ListeningStatus_Click(object sender, RoutedEventArgs e) {
            ToggleListener();
        }
    }
}
