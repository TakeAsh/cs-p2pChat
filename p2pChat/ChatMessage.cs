using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TakeAshUtility;

namespace p2pChat {

    public struct ChatMessage {

        const int StringBufferSize = 128;

        public enum Commands : short {
            Undefined,
            Acknowledge,
            Register,
            Say,
        }

        public ChatMessage(Commands command, string sender, string body, byte[] icon) :
            this() {

            Command = command;
            Sender = sender;
            Body = body;
            Icon = icon;
        }

        public Commands Command;

        public string Sender;

        public string Body;

        public byte[] Icon;

        public override string ToString() {
            return "Command:" + Command + ", Sender:{" + Sender + "}, Message:{" + Body + "}";
        }

        public byte[] ToBytes() {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8)) {
                writer.Write((short)Command);
                writer.Write(Sender);
                writer.Write(Body);
                if (Icon == null) {
                    writer.Write((int)0);
                } else {
                    writer.Write(Icon.Length);
                    writer.Write(Icon, 0, Icon.Length);
                }
                return stream.ToArray();
            }
        }

        public static ChatMessage FromBytes(byte[] bytes) {
            var ret = new ChatMessage();
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream, Encoding.UTF8)) {
                ret.Command = (Commands)reader.ReadInt16();
                ret.Sender = reader.ReadString();
                ret.Body = reader.ReadString();
                var iconLength = reader.ReadInt32();
                if (iconLength > 0) {
                    ret.Icon = reader.ReadBytes(iconLength);
                }
            }
            return ret;
        }
    }
}
