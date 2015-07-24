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
        private static readonly char[] WhiteSpaces = new[] { ' ', '\n', '\r', '\t', '\0', };

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
            using (var ns = client.GetStream()) {
                if (ns.CanTimeout) {
                    ns.ReadTimeout = _settings.NetworkTimeout * 1000;
                    ns.WriteTimeout = _settings.NetworkTimeout * 1000;
                }
                var endPoint = client.Client.RemoteEndPoint as IPEndPoint;
                var clientAddress = endPoint != null ?
                    endPoint.Address.ToString() + ":" + endPoint.Port.ToString() :
                    null;
                _worker.ReportProgress(0, "Connected: " + clientAddress);
                try {
                    var isDisconnected = false;
                    while (!isDisconnected) {
                        var message = "";
                        using (var ms = new MemoryStream()) {
                            var receiveBuffer = new byte[BufferSize];
                            var receiveSize = 0;
                            do {
                                receiveSize = ns.Read(receiveBuffer, 0, receiveBuffer.Length);
                                if (receiveSize == 0) {
                                    isDisconnected = true;
                                    break;
                                }
                                ms.Write(receiveBuffer, 0, receiveSize);
                            } while (ns.DataAvailable);
                            message = isDisconnected ?
                                "Disconnected: " + clientAddress :
                                Encoding.UTF8
                                    .GetString(ms.GetBuffer(), 0, (int)ms.Length);
                        }
                        _worker.ReportProgress(0, message.Trim(WhiteSpaces));
                        if (!isDisconnected) {
                            var response = "Received: " + message.Length.ToString() + "\r\n\0";
                            var sendBuffer = Encoding.UTF8.GetBytes(response);
                            ns.Write(sendBuffer, 0, sendBuffer.Length);
                        }
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex) {
                    var socketException = ex.InnerException as SocketException;
                    var message = socketException != null && socketException.ErrorCode == (int)SocketError.TimedOut ?
                        "Timeout: " + clientAddress :
                        ex.GetAllMessages();
                    _worker.ReportProgress(0, message);
                }
            }
        }
    }
}
