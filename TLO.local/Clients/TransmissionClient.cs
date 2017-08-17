using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
//using System.Web.Script.Serialization;

namespace TLO.local
{
    class TransmissionClient : ITorrentClient
    {
        class Querty
        {
            public Argument arguments { get; set; }
        }

        class Argument
        {
            public Torrent[] torrents { get; set; }
        }

        class Torrent
        {
            public int id { get; set; }
            public string hashString { get; set; }
            public long totalSize { get; set; }

            public decimal percentDone { get; set; }
            public int error { get; set; }
            public int status { get; set; }
        }

        static NLog.Logger _logger = null;
        private TLOWebClient _webClient;
        private string _URL;
        private string svcCredentials;

        public TransmissionClient(string serverName, int port, string userName, string userPass)
        {
            if (_logger == null)
                _logger = NLog.LogManager.GetLogger("TransmissionClient");
            _webClient = new TLOWebClient(Encoding.UTF8, "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:36.0) Gecko/20100101 Firefox/36.0", "application/json, text/javascript, */*; q=0.01", true);
            _webClient.Encoding = System.Text.Encoding.UTF8;
            svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName + ":" + userPass));
            _webClient.Headers.Add("Authorization", "Basic " + svcCredentials);

            _URL = string.Format("http://{0}:{1}/transmission/rpc", serverName, port);
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
            try
            {
                var str = "{\"method\":\"torrent-get\",\"arguments\":{\"fields\":[\"hashString\", \"totalSize\", \"percentDone\", \"error\", \"status\"]}}";
                var data = _webClient.UploadData(_URL, "POST", Encoding.UTF8.GetBytes(str));

                //var objJason = new JavaScriptSerializer();
                //objJason.MaxJsonLength = int.MaxValue;
                //var obj = objJason.Deserialize<Querty>(Encoding.UTF8.GetString(data));
                var obj = JsonConvert.DeserializeObject<Querty>(Encoding.UTF8.GetString(data));

                if (obj == null || obj.arguments == null || obj.arguments.torrents == null || obj.arguments.torrents.Length == 0)
                    return new List<TopicInfo>();
                return (from t in obj.arguments.torrents
                        select new TopicInfo()
                        {
                            Hash = t.hashString.ToUpper(),
                            IsKeep = t.percentDone == 1 && t.error == 0,
                            IsDownload = true,
                            IsRun = (t.percentDone == 1 && t.error == 0) ? (bool?)(t.status == 6) : null
                        }).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Debug(ex.StackTrace);
                throw ex;
            }
        }

        public IEnumerable<string> GetFiles(TopicInfo topic)
        {
            yield break;
        }

        public void DistributionStop(IEnumerable<string> data) 
        {
            var str = "{\"method\":\"torrent-get\",\"arguments\":{\"fields\":[\"hashString\", \"id\"]}}";
            var dt = _webClient.UploadData(_URL, "POST", Encoding.UTF8.GetBytes(str));

            //var objJason = new JavaScriptSerializer();
            //objJason.MaxJsonLength = int.MaxValue;
            //var obj = objJason.Deserialize<Querty>(Encoding.UTF8.GetString(dt));
            var obj = JsonConvert.DeserializeObject<Querty>(Encoding.UTF8.GetString(dt));
            if (obj == null || obj.arguments == null || obj.arguments.torrents == null || obj.arguments.torrents.Length == 0)
                return;

            var ids = (from t in obj.arguments.torrents
                       join d in data on t.hashString.ToUpper() equals d
                       select t.id).ToArray();
            if (ids.Length == 0) return;
            var obj2 = new
            {
                method = "torrent-stop",
                arguments = new { ids = ids }
            };
            
            var message = JsonConvert.SerializeObject(obj2);
            _webClient.UploadData(_URL, "POST", Encoding.UTF8.GetBytes(message));
        }
        public void DistributionPause(IEnumerable<string> data)
        {
            var str = "{\"method\":\"torrent-get\",\"arguments\":{\"fields\":[\"hashString\", \"id\"]}}";
            var dt = _webClient.UploadData(_URL, "POST", Encoding.UTF8.GetBytes(str));

            //var objJason = new JavaScriptSerializer();
            //objJason.MaxJsonLength = int.MaxValue;
            //var obj = objJason.Deserialize<Querty>(Encoding.UTF8.GetString(dt));
            var obj = JsonConvert.DeserializeObject<Querty>(Encoding.UTF8.GetString(dt));
            if (obj == null || obj.arguments == null || obj.arguments.torrents == null || obj.arguments.torrents.Length == 0)
                return;

            var ids = (from t in obj.arguments.torrents
                       join d in data on t.hashString.ToUpper() equals d
                       select t.id).ToArray();
            if (ids.Length == 0) return;
            var obj2 = new
            {
                method = "torrent-stop",
                arguments = new { ids = ids }
            };
            var message = JsonConvert.SerializeObject(obj2);
            _webClient.UploadData(_URL, "POST", Encoding.UTF8.GetBytes(message));
        }
        public void DistributionStart(IEnumerable<string> data)
        {
            var str = "{\"method\":\"torrent-get\",\"arguments\":{\"fields\":[\"hashString\", \"id\"]}}";
            var dt = _webClient.UploadData(_URL, "POST", Encoding.UTF8.GetBytes(str));

            //var objJason = new JavaScriptSerializer();
            //objJason.MaxJsonLength = int.MaxValue;
            //var obj = objJason.Deserialize<Querty>(Encoding.UTF8.GetString(dt));
            var obj = JsonConvert.DeserializeObject<Querty>(Encoding.UTF8.GetString(dt));
            if (obj == null || obj.arguments == null || obj.arguments.torrents == null || obj.arguments.torrents.Length == 0)
                return;

            var ids = (from t in obj.arguments.torrents
                       join d in data on t.hashString.ToUpper() equals d
                       select t.id).ToArray();
            if (ids.Length == 0) return;
            var obj2 = new
            {
                method = "torrent-start",
                arguments = new { ids = ids }
            };
            //var message = objJason.Serialize(obj2);
            var message = JsonConvert.SerializeObject(obj2);
            _webClient.UploadData(_URL, "POST", Encoding.UTF8.GetBytes(message));
        }

        public bool Ping()
        {
            while (true)
            {
                try
                {
                    _webClient.UploadData(_URL, "POST", Encoding.UTF8.GetBytes("{\"method\":\"session-get\"}"));
                    return true;
                }
                catch (WebException ex)
                {
                    _webClient.Headers.Add("X-Transmission-Session-Id", ex.Response.Headers["X-Transmission-Session-Id"]);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    _logger.Debug(ex.StackTrace);
                    throw ex;
                }
            }
        }

        public bool SetDefaultFolder(string dir) { return true; }
        public bool SetDefaultLabel(string label) { return true; }

        public string GetDefaultFolder() { return string.Empty; }

        public void SendTorrentFile(string path, string file)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                //  Читаем torrent-файл (косячно но вроде работает)
                using (var fstream = System.IO.File.OpenRead(file))
                {
                    fstream.CopyTo(stream);
                    SendTorrentFile(path, System.IO.Path.GetFileName(file), stream.ToArray());
                }
            }
        }
        public void SendTorrentFile(string path, string filename, byte[] fdata) 
        {
            var torrent = Convert.ToBase64String(fdata);
            var obj = new
            {
                method = "torrent-add",
                arguments = new
                {
                    paused = false,
                    downloadDir = path,
                    metainfo = torrent
                }
            };
            //var objJason = new JavaScriptSerializer();
            //objJason.MaxJsonLength = int.MaxValue;
            //var message = objJason.Serialize(obj).Replace("downloadDir", "download-dir");
            var message = JsonConvert.SerializeObject(obj).Replace("downloadDir", "download-dir");
            var data = _webClient.UploadData(_URL, "POST", Encoding.UTF8.GetBytes(message));
        }

        public string[] GetTrackers(string hash) { return null; }
        public bool SetTrackers(string hash, string[] trackers) { return true; }

        public bool SetLabel(string hash, string label) { return true; }
        public bool SetLabel(IEnumerable<string> hash, string label) { return true; }
    }
}
