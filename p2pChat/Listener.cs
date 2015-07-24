using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using TakeAshUtility;

namespace p2pChat {
    
    public class Listener {

        private const int BufferSize = 1024;

        private static Properties.Settings _settings = Properties.Settings.Default;

        private TextBox _log;
        private TcpListener _listener;
        private BackgroundWorker _worker;

        public Listener(TextBox log) {
            _log = log;
            _listener = new TcpListener(IPAddress.IPv6Any, _settings.Port);
            _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
            _worker = CreateWorker();
        }

        public bool IsBusy {
            get { return _worker != null && _worker.IsBusy; }
        }

        public void Start() {
            _worker.RunWorkerAsync();
        }

        public void Stop() {
            _worker.CancelAsync();
        }

        private BackgroundWorker CreateWorker() {
            var worker = new BackgroundWorker() {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true,
            };
            worker.DoWork += (sender, e) => {
                _listener.Start();
                while (true) {
                    if (e.Cancel) {
                        break;
                    }
                    if (_listener.Pending()) {
                        HandleClient();
                    }
                    Thread.Sleep(100);
                }
            };
            worker.ProgressChanged += (sender, e) => {
                var message = e.UserState as string;
                if (String.IsNullOrEmpty(message)) {
                    return;
                }
                _log.Text += message + "\n";
            };
            worker.RunWorkerCompleted += (sender, e) => {
                _listener.Stop();
            };
            return worker;
        }

        private void HandleClient() {
            using (var client = _listener.AcceptTcpClient())
            using (var ns = client.GetStream())
            using (var ms = new System.IO.MemoryStream()) {
                if (ns.CanTimeout) {
                    ns.ReadTimeout = _settings.NetworkTimeout * 1000;
                    ns.WriteTimeout = _settings.NetworkTimeout * 1000;
                }
                var receiveBuffer = new byte[BufferSize];
                var receiveSize = 0;
                var message = "";
                try {
                    do {
                        receiveSize = ns.Read(receiveBuffer, 0, receiveBuffer.Length);
                        if (receiveSize == 0) {
                            break;
                        }
                        ms.Write(receiveBuffer, 0, receiveSize);
                    } while (ns.DataAvailable);
                    message = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length).Trim(new[] { ' ', '\n', '\r', '\t', '\0' });
                }
                catch (Exception ex) {
                    message = ex.GetAllMessages();
                }
                var endPoint = client.Client.RemoteEndPoint as IPEndPoint;
                if (endPoint != null) {
                    message += " (" + endPoint.Address.ToString() + ", " + endPoint.Port.ToString() + ")";
                }
                _worker.ReportProgress(0, message);
                var response = "Received: " + message.Length.ToString() + "\n";
                var sendBuffer = Encoding.UTF8.GetBytes(response);
                ns.Write(sendBuffer, 0, sendBuffer.Length);
            }
        }
    }
}
