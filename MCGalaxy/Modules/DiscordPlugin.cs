﻿/*
    Copyright 2015 MCGalaxy
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MCGalaxy.Config;
using MCGalaxy.Network;

namespace MCGalaxy.Modules.Discord {

    public static class DiscordConfig {
        [ConfigString("bot-token", null, "", true)]
        public static string BotToken = "";
        [ConfigString("read-channel-ids", null, "", true)]
        public static string ReadChannels = "";
        [ConfigString("send-channel-ids", null, "", true)]
        public static string SendChannels = "";
        
        [ConfigBool("enabled", null, false)]
        public static bool Enabled;
        
        const string file = "properties/discordbot.properties";
        static ConfigElement[] cfg;
        
        public static void Load() {
            // create default config file
            if (!File.Exists(file)) Save();

            if (cfg == null) cfg = ConfigElement.GetAll(typeof(DiscordConfig));
            ConfigElement.ParseFile(cfg, "properties/discordbot.properties", null);
        }
        
        public static void Save() {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(DiscordConfig));
            ConfigElement.SerialiseSimple(cfg, "properties/discordbot.properties", null);
        }
    }
    
    public sealed class DiscordWebsocket : ClientWebSocket {
        TcpClient client;
        SslStream stream;
        string _token;
        
        public DiscordWebsocket(string token) {
            _token = token;
        }
        
        const string host = "gateway.discord.gg";
        // stubs
        public override bool LowLatency { set { } }
        public override string IP { get { return ""; } }
        
        public void Connect() {
            client = new TcpClient();
            client.Connect(host, 443);

            stream   = HttpUtil.WrapSSLStream(client.GetStream(), host);
            protocol = this;
            Init();
        }
        
        public void ReadLoop() {
            byte[] data = new byte[4096];
            for (;;) {
                int len = stream.Read(data, 0, 4096);
                if (len == 0) break; // disconnected
                HandleReceived(data, len);
            }
        }
        
        protected override void HandleData(byte[] data, int len) {
            Console.WriteLine("DATA: " + Encoding.UTF8.GetString(data, 0, len));
        }
        
        protected override void SendRaw(byte[] data, SendFlags flags) {
            stream.Write(data);
        }
        
        public override void Close() {
            client.Close();
        }
        
        protected override void Disconnect(int reason) {
            base.Disconnect(reason);
            Close();
        }
        
        protected override string Path { get { return "/?v=6&encoding=json"; } }
        
        protected override void WriteCustomHeaders() {
            WriteHeader("Authorization: Bot " + _token);
            WriteHeader("Host: " + host);
        }
    }
    
    public sealed class DiscordPlugin : Plugin {
        public override string creator { get { return Server.SoftwareName + " team"; } }
        public override string MCGalaxy_Version { get { return Server.Version; } }
        public override string name { get { return "DiscordRelayPlugin"; } }
        Thread thread;
        DiscordWebsocket socket;
        
        public override void Load(bool startup) {
            DiscordConfig.Load();
            if (!DiscordConfig.Enabled) return;
            
            thread      = new Thread(IOThread);
            thread.Name = "MCG-DiscordRelay";
            thread.IsBackground = true;
            thread.Start();
        }
        
        void IOThread() {
            try {
                string token = DiscordConfig.BotToken;
                socket = new DiscordWebsocket(token);
                socket.Connect();
                socket.ReadLoop();
            } catch (Exception ex) {
                Logger.LogError("Discord relay error", ex);
            }
        }
        
        public override void Unload(bool shutdown) {

        }
    }
}