using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TLO.local
{
    class KTorrentClient : ITorrentClient
    {
        static NLog.Logger _logger = null;
        private TLOWebClient _webClient;
        private string _ServerName;
        private int _ServerPort;
        private string svcCredentials;

        public KTorrentClient(string serverName, int port, string userName, string userPass)
        {
            if (_logger == null)
                _logger = NLog.LogManager.GetLogger("TransmissionClient");
            _webClient = new TLOWebClient(Encoding.UTF8, "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:36.0) Gecko/20100101 Firefox/36.0", "application/json, text/javascript, */*; q=0.01", true);
            _webClient.Encoding = System.Text.Encoding.UTF8;
            svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName + ":" + userPass));
            _webClient.Headers.Add("Authorization", "Basic " + svcCredentials);

            _ServerName = serverName;
            _ServerPort = port;
            try
            {
                Ping();
            }
            catch
            {
                _logger.Debug(string.Format("Имя сервера: {0}; Порт сервера: {1}", serverName, port));
                throw;
            }
        }

        public List<TopicInfo> GetAllTorrentHash()
        {
            return new List<TopicInfo>();
        }

        public IEnumerable<string> GetFiles(TopicInfo topic)
        {
            return new List<string>();
        }

        public void DistributionStop(IEnumerable<string> data){}
        public void DistributionPause(IEnumerable<string> data) { }
        public void DistributionStart(IEnumerable<string> data) { }

        public bool Ping() { return true; }

        public bool SetDefaultFolder(string dir) { return true; }
        public bool SetDefaultLabel(string label) { return true; }

        public string GetDefaultFolder() { return string.Empty; }

        public void SendTorrentFile(string path, string file) { }
        public void SendTorrentFile(string path, string filename, byte[] fdata) { }

        public string[] GetTrackers(string hash) { return null; }
        public bool SetTrackers(string hash, string[] trackers) { return true; }

        public bool SetLabel(string hash, string label) { return true; }
        public bool SetLabel(IEnumerable<string> hash, string label) { return true; }
    }
}
