using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace p2pChat {

    public class ChatClients :
        Dictionary<string, ChatClient> {

        private static Properties.Settings _settings = Properties.Settings.Default;

        public string Register(ChatMessage message) {
            var client = new ChatClient(message.Sender, message.Body);
            var path = Path.Combine(_settings.IconsFolder, client.Name + client.IconExtension);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                stream.Write(message.Icon, 0, message.Icon.Length);
            }
            this[client.Name] = client;
            return "OK";
        }
    }

    public class ChatClient {

        private static Properties.Settings _settings = Properties.Settings.Default;

        public ChatClient(string name, string iconPath) {
            Name = name;
            IconExtension = Path.GetExtension(iconPath);
        }

        public string Name { get; set; }
        public string IconExtension { get; set; }
        public BitmapImage Icon {
            get {
                var path = Path.Combine(_settings.IconsFolder, Name + IconExtension);
                var image = new BitmapImage();
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                }
                return image;
            }
        }

        public override string ToString() {
            return "Name:{" + Name + "}, IconExtension:{" + IconExtension + "}";
        }

        public ChatMessage ToRegisterMessage() {
            byte[] bytes;
            using (var stream = new FileStream(_settings.MyIcon, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
            }
            return new ChatMessage(
                ChatMessage.Commands.Register,
                Name,
                IconExtension,
                bytes
            );
        }
    }
}
