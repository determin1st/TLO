using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TLO.local
{
    class TorrentClientInfo
    {
        public TorrentClientInfo()
        {
            UID = Guid.NewGuid();
            Name = string.Empty;
            Type = "uTorrent";
            ServerName = string.Empty;
            ServerPort = 999;
            UserName = string.Empty;
            UserPassword = string.Empty;
            LastReadHash = new DateTime(2000, 1, 1);
        }
        public Guid UID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ServerName { get; set; }
        public int ServerPort { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public DateTime LastReadHash { get; set; }

        public override string ToString()
        {
            return Name;
        }
        public ITorrentClient Create()
        {
            ITorrentClient tc = null;
            if (this.Type == "uTorrent")
                tc = new uTorrentClient(ServerName, ServerPort,UserName, UserPassword);
            else if(this.Type == "Transmission")
                tc = new TransmissionClient(ServerName, ServerPort, UserName, UserPassword);
            else if(Type == "Vuze (Vuze Web Remote)")
                tc = new TransmissionClient(ServerName, ServerPort, UserName, UserPassword);
            return tc;
        }
    }
}
