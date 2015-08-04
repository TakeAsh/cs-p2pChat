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
using System.Windows.Threading;
using TakeAshUtility;

namespace p2pChat {

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow :
        Window {

        private static Properties.Settings _settings = Properties.Settings.Default;

        private Listener _listenerV4;
        private Listener _listenerV6;
        private Talker _talker;
        private Paragraph _paragraph = new Paragraph();

        public MainWindow() {
            InitializeComponent();
            textBox_Host.Text = _settings.Host;
            group_Config.Visibility = Visibility.Collapsed;
            textBox_Port.Text = _settings.Port.ToString();
            textBox_NetworkTimeout.Text = _settings.NetworkTimeout.ToString();
            textBox_Name.Text = _settings.MyName.ToDefaultIfNullOrEmpty(Dns.GetHostName());
            _listenerV4 = CreateListener(false);
            _listenerV6 = CreateListener(true);
            textBlock_Log.Document = new FlowDocument(_paragraph) {
                FontFamily = new FontFamily(_settings.FontName),
                FontSize = _settings.FontSize,
                PagePadding = new Thickness(0),
            };
        }

        private void ShowMessage(string message) {
            _paragraph.Inlines.Add(new Run(message));
            _paragraph.Inlines.Add(new LineBreak());
        }

        private Listener CreateListener(bool useIPv6) {
            return new Listener(
                textBox_Port.Text.TryParse(_settings.Port),
                textBox_NetworkTimeout.Text.TryParse(_settings.NetworkTimeout),
                useIPv6,
                PropertyChangedWithValueHandler,
                MessageReceivedEventHandler
            );
        }

        private void ToggleListener() {
            if (_listenerV4.IsBusy || _listenerV6.IsBusy) {
                _listenerV4.Stop();
                _listenerV6.Stop();
                image_ListenStatus.Source = ResourceHelper.GetImage("Images/Wait.png");
            } else {
                try {
                    _listenerV4.Dispose();
                    _listenerV4 = CreateListener(false);
                    _listenerV4.Start();
                    _listenerV6.Dispose();
                    _listenerV6 = CreateListener(true);
                    _listenerV6.Start();
                    image_ListenStatus.Source = ResourceHelper.GetImage("Images/Play.png");
                }
                catch (Exception ex) {
                    ShowMessage(ex.GetAllMessages());
                }
            }
        }

        private void Disconnect() {
            if (_talker == null) {
                return;
            }
            _talker.Dispose();
            _talker = null;
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            if (_listenerV4.IsBusy) {
                _listenerV4.Dispose();
            }
            if (_listenerV6.IsBusy) {
                _listenerV6.Dispose();
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
                    textBox_Port.Text.TryParse(_settings.Port);
                _talker = new Talker(
                    host,
                    port,
                    textBox_NetworkTimeout.Text.TryParse(_settings.NetworkTimeout),
                    PropertyChangedWithValueHandler
                );
                _settings.Host = textBox_Host.Text;
                _settings.Save();
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
                _settings.NetworkTimeout = textBox_NetworkTimeout.Text.TryParse(_settings.NetworkTimeout);
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

        private void PropertyChangedWithValueHandler(
            object sender,
            PropertyChangedWithValueEventArgs e
        ) {
            var talker = sender as Talker;
            if (talker != null) {
                switch (e.PropertyName) {
                    case "Connected":
                        var connected = (bool)e.NewValue;
                        Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() => {
                                if (connected) {
                                    ShowMessage("Connect: " + talker.Host + ":" + talker.Port);
                                    button_Connect.IsEnabled = false;
                                    button_Disconnect.IsEnabled = true;
                                    button_Send.IsEnabled = true;
                                } else {
                                    ShowMessage("Disconnect: " + talker.Host + ":" + talker.Port);
                                    button_Connect.IsEnabled = true;
                                    button_Disconnect.IsEnabled = false;
                                    button_Send.IsEnabled = false;
                                }
                            })
                        );
                        break;
                    case "Message":
                        var message = e.NewValue as string;
                        Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() => ShowMessage(message))
                        );
                        break;
                }
            }
            var listener = sender as Listener;
            if (listener != null) {
                switch (e.PropertyName) {
                    case "Message":
                        var message = e.NewValue as string;
                        Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() => ShowMessage(message))
                        );
                        break;
                }
            }
        }

        private void MessageReceivedEventHandler(
            INotifyMessageReceived sender,
            MessageReceivedEventArgs e
        ) {
            var listener = sender as Listener;
            if (listener == null) {
                return;
            }
            var message = e.Message.SplitTrim(new[] { "\t" });
            var body = message.LastOrDefault();
            switch (body) {
                case ":Now":
                    e.Response = DateTime.Now.ToString("g");
                    break;
                case ":Me":
                    e.Response = message.FirstOrDefault();
                    break;
                default:
                    e.Response = body;
                    break;
            }
            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => ShowMessage(e.Message))
            );
        }
    }
}
