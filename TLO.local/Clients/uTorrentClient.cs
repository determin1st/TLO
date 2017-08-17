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
    class uTorrentClient : ITorrentClient
    {
        static NLog.Logger _logger = null;
        private TLOWebClient _webClient;
        private string _ServerName;
        private int _ServerPort;
        private string svcCredentials;
        private string token;

        public uTorrentClient(string serverName, int port, string userName, string userPass)
        {
            if (_logger == null)
                _logger = NLog.LogManager.GetLogger("uTorrentClient");
            _webClient = new TLOWebClient();
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
                _logger.Debug(string.Format("Имя сервера: {0}; Порт сервера: {1}", _ServerName, _ServerPort));
                throw;
            }
        }


        public bool Ping()
        {
            try
            {
                var str = _webClient.GetString(string.Format("http://{0}:{1}/gui/", _ServerName, _ServerPort));
                var strarr = str.Split(new string[] { "div" }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains("token")).ToArray();
                if (strarr.Length > 0)
                {
                    strarr = strarr[0].Split(new char[] { '>', '<' }, StringSplitOptions.RemoveEmptyEntries);
                    token = strarr[1];
                }
                else
                    token = null;
                return true;
            }
            catch
            {
                throw;
            }
        }
        class tt
        {
            public int build { get; set;}
            public List<object> files { get; set; }
        }

        public List<TopicInfo> GetAllTorrentHash()
        {
            
            var param = new List<string[]>();
            if (!string.IsNullOrWhiteSpace(token))
            {
                param.Add(new string[] { "token", token });
            }
            param.Add(new string[] { "list", "1" });
            var data = _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));
            var obj = JsonConvert.DeserializeObject<JObject>(data)["torrents"].ToObject<object[][]>();
            
            if (obj == null)
                return new List<TopicInfo>();

            //_logger.Debug("Получение хешей из TORRENT-клиента: всего " + tmpObbj.Count().ToString());
            var tr = obj
                .Select(x =>
                {
                    return new
                    {
                        Hash = (x[0] as string).ToUpper(),
                        Name = x[2] as string,
                        Size = x[3].GetType() == typeof(int) ?  (long)((int)x[3]) :(x[3].GetType() == typeof(long) ? (long)x[3] : 0),
                        Status = IntToArrayBool(((long)x[1])),
                        PercentComplite = ((long)x[4] * 0.1M)
                    };
                })
                .Select(x => { return new TopicInfo() 
                {
                    Hash = x.Hash,
                    TorrentName = x.Name,
                    Size = x.Size,
                    IsKeep = x.Status != null && x.PercentComplite == 100 && x.Status[3] && !x.Status[4] && x.Status[7],
                    IsDownload = true,
                    IsPause = x.Status[5],
                    //PercentComplite = x.PercentComplite,
                    IsRun = (x.PercentComplite == 100 && x.Status[3] && !x.Status[4] && x.Status[7]) ? (bool?)(x.Status[0] ? true : false) : null
                }; })
                .ToList();
            //_logger.Debug("Получение хешей из TORRENT-клиента: получено " + tr.Count().ToString());
            return tr;
        }

        public IEnumerable<string> GetFiles(TopicInfo topic)
        {
            if (topic == null || string.IsNullOrEmpty(topic.Hash))
                yield break;

            var param = new List<string[]>();
            if (!string.IsNullOrWhiteSpace(token))
            {
                param.Add(new string[] { "token", token });
            }
            param.Add(new string[] { "action", "getfiles" });
            param.Add(new string[] { "hash", topic.Hash });

            var data = _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));
            var obj = JsonConvert.DeserializeObject<JObject>(data)["files"];

            if (obj == null)
                yield break;
            obj = obj.ToObject<JArray>()[1].ToObject<JArray>();
            
            foreach(var t in obj)
            {
                var d = t.ToObject<object[]>();
                yield return d[0].ToString();
            }
        }

        private bool[] IntToArrayBool(long value)
        {
            System.Collections.BitArray b = new System.Collections.BitArray(new int[] { (int)value });

            bool[] bits = new bool[b.Count];
            b.CopyTo(bits, 0);
            return bits;
        }

        public void DistributionStop(IEnumerable<string> data)
        {
            var param = new List<string[]>();
            if (!string.IsNullOrWhiteSpace(token))
            {
                param.Add(new string[] { "token", token });
            }
            param.Add( new string[] { "action", "stop" });
            param.AddRange(data.Select(x => new string[] { "hash", x }));

            _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));
        }

        public void DistributionPause(IEnumerable<string> data)
        {
            var param = new List<string[]>();
            if (!string.IsNullOrWhiteSpace(token))
            {
                param.Add(new string[] { "token", token });
            }
            param.Add(new string[] { "action", "pause" });
            param.AddRange(data.Select(x => new string[] { "hash", x }));

            _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));
        }

        public void DistributionStart(IEnumerable<string> data)
        {
            var param = new List<string[]>();
            if (!string.IsNullOrWhiteSpace(token))
            {
                param.Add(new string[] { "token", token });
            }
            param.Add(new string[] { "action", "start" });
            param.AddRange(data.Select(x => new string[] { "hash", x }));

            _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));
        }

        public bool SetDefaultFolder(string dir)
        {
            try
            {
                var param = new List<string[]>();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    param.Add(new string[] { "token", token });
                }
                param.Add(new string[] { "action", "setsetting" });
                param.Add(new string[] { "s", "dir_active_download" });
                param.Add(new string[] { "v", System.Web.HttpUtility.UrlEncode(dir) });

                _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));

                //_webClient.DownloadString(string.Format("http://{0}:{1}/gui/?action=setsetting&s=dir_active_download&v={2}", _ServerName, _ServerPort, System.Web.HttpUtility.UrlEncode(dir)));
                //  Устанавливаем слип на всякий случай, что бы Torrent-клиент успел обработать и отдохнуть
                System.Threading.Thread.Sleep(100);
                //  Получаем текущее значение каталога по умолчанию и сравниваем с устанавливаемым значением
                //  Что бы навернека быть увереными что значение применилось
                return GetDefaultFolder() == dir;
            }
            catch
            {
                return false;
            }
        }
        public string GetDefaultFolder()
        {
            try
            {
                var param = new List<string[]>();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    param.Add(new string[] { "token", token });
                }
                param.Add(new string[] { "action", "getsettings" });

                var data = _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));

                //var data = _webClient.DownloadString(string.Format("http://{0}:{1}/gui/?action=getsettings", _ServerName, _ServerPort));

                var obj = JsonConvert.DeserializeObject<JObject>(data)["settings"].ToObject<object[][]>();

                if (obj == null)
                    return string.Empty;
                var tmp = obj
                    .Where(x => (x[0] as string) == "dir_active_download")
                    .FirstOrDefault();

                if (tmp == null)
                    return string.Empty;

                return tmp[2] as string;
            }
            catch
            {
                return null;
            }
        }

        public bool SetDefaultLabel(string label)
        {
            try
            {
                var param = new List<string[]>();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    param.Add(new string[] { "token", token });
                }
                param.Add(new string[] { "action", "setsetting" });
                param.Add(new string[] { "s", "dir_add_label" });
                param.Add(new string[] { "v", System.Web.HttpUtility.UrlEncode(label) });

                _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));
                //_webClient.DownloadString(string.Format("http://{0}:{1}/gui/?action=setsetting&s=dir_add_label&v={2}", _ServerName, _ServerPort, System.Web.HttpUtility.UrlEncode(label)));
                //  Устанавливаем слип на всякий случай, что бы Torrent-клиент успел обработать и отдохнуть
                System.Threading.Thread.Sleep(200);
                //  Получаем текущее значение каталога по умолчанию и сравниваем с устанавливаемым значением
                //  Что бы навернека быть увереными что значение применилось
                return GetDefaultLabel() == label;
            }
            catch
            {
                return false;
            }
        }
        public string GetDefaultLabel()
        {
            try
            {
                var param = new List<string[]>();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    param.Add(new string[] { "token", token });
                }
                param.Add(new string[] { "action", "getsettings" });
                var data = _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));
                //var data = _webClient.DownloadString(string.Format("http://{0}:{1}/gui/?action=getsettings", _ServerName, _ServerPort));

                var obj = JsonConvert.DeserializeObject<JObject>(data)["settings"].ToObject<object[][]>();
                if (obj == null)
                    return string.Empty;
                var tmp = obj
                    .Where(x => (x[0] as string) == "dir_add_label")
                    .FirstOrDefault();

                if (tmp == null)
                    return string.Empty;

                return tmp[2] as string;
            }
            catch
            {
                return null;
            }
        }

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
        public void SendTorrentFile(string path, string filename,byte[] fdata)
        {
            //  Используем не через web-клиент запрос (не нашел иного способа)

            var boundary = "----WebKitFormBoundary1vZaMilolI9TchBt";
            using (var stream = new System.IO.MemoryStream())
            {
                var data = Encoding.ASCII.GetBytes(string.Format(@"--{0}
Content-Disposition: form-data; name=""torrent_file""; filename=""{1}""
Content-Type: application/x-bittorrent

", boundary, filename));

                stream.Write(data, 0, data.Length);
                //  Читаем torrent-файл (косячно но вроде работает)
                stream.Write(fdata, 0, fdata.Length);

                data = Encoding.ASCII.GetBytes(string.Format("\r\n--{0}--\r\n", boundary));
                stream.Write(data, 0, data.Length);

                var param = new List<string[]>();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    param.Add(new string[] { "token", token });
                }
                param.Add(new string[] { "action", "add-file" });

                var webRequest = (HttpWebRequest)HttpWebRequest.Create(
                    string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x))))
                    );
                webRequest.Method = "POST";
                webRequest.KeepAlive = true;
                webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                //  Устанавливаем параметр границы данных
                webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                //  Устанавливаем параметры авторизации
                webRequest.Headers.Add("Authorization", _webClient.Headers.Get("Authorization"));
                webRequest.Headers.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");

                data = stream.ToArray();

                webRequest.ContentLength = data.Length;
                using (var reqStream = webRequest.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                }
            }
        }

        public string[] GetTrackers(string hash)
        {
            //try
            //{
            //    var param = new List<string[]>();
            //    if (!string.IsNullOrWhiteSpace(token))
            //    {
            //        param.Add(new string[] { "token", token });
            //    }
            //    param.Add(new string[] { "action", "getprops" });
            //    param.Add(new string[] { "hash", hash });

            //    var data = _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));

            //    //var data = _webClient.DownloadString(string.Format("http://{0}:{1}/gui/?action=getprops&hash={2}", _ServerName, _ServerPort, hash));

            //    var obj = new JavaScriptSerializer().DeserializeObject(data);
            //    var dobj = obj as Dictionary<string, object>;

            //    if (dobj == null)
            //        return null;

            //    var trackers = (dobj["props"] as object[])
            //        .Select(x=>x as Dictionary<string, object>)
            //        .Where(x => x.Keys.Contains("trackers"))
            //        .FirstOrDefault()["trackers"] as string;

            //    return trackers.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            //}
            //catch
            //{
                return null;
            //}
        }
        public bool SetTrackers(string hash, string[] trackers)
        {
            try
            {
                var trs = string.Join("\r\n\r\n", trackers) + "\r\n";
                var param = new List<string[]>();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    param.Add(new string[] { "token", token });
                }
                param.Add(new string[] { "setprops", "setprops" });
                param.Add(new string[] { "hash", hash });
                param.Add(new string[] { "s", "trackers" });
                param.Add(new string[] { "v", System.Web.HttpUtility.UrlEncode(trs) });

                var data = _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));
                //  Устанавливаем слип на всякий случай, что бы Torrent-клиент успел обработать и отдохнуть
                System.Threading.Thread.Sleep(100);
                //  Получаем текущее значение каталога по умолчанию и сравниваем с устанавливаемым значением
                //  Что бы навернека быть увереными что значение применилось
                var trct = GetTrackers(hash);
                if(trct == null)
                    return false;
                return (string.Join("\r\n\r\n", trct) + "\r\n") == trs;
            }
            catch
            {
                return false;
            }
        }

        public bool SetLabel(string hash, string label)
        {
            System.Threading.Thread.Sleep(100);
            var param = new List<string[]>();
            if (!string.IsNullOrWhiteSpace(token))
            {
                param.Add(new string[] { "token", token });
            }
            param.Add(new string[] { "action", "setprops" });
            param.Add(new string[] { "hash", hash });
            param.Add(new string[] { "s", "label" });
            param.Add(new string[] { "v", label });
            _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));

            System.Threading.Thread.Sleep(100);
            return true;
        }
        public bool SetLabel(IEnumerable<string> hashs, string label)
        {
            if (hashs == null || hashs.Count() == 0) return true;

            var param = new List<string[]>();
            
            foreach (var hash in hashs)
            {
                if (param.Count() == 0)
                {
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        param.Add(new string[] { "token", token });
                    }
                    param.Add(new string[] { "action", "setprops" });
                }
                param.Add(new string[] { "s", "label" });
                param.Add(new string[] { "hash", hash });
                param.Add(new string[] { "v", label });
                if (param.Count() > 150)
                {
                    //param.Add(new string[] { "t", ((int)(DateTime.Now - new DateTime(1970,1,1)).TotalSeconds).ToString()});
                    _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));
                    param.Clear();
                }
                System.Threading.Thread.Sleep(100);
            }
            if (param.Count() != 0)
            {
                //param.Add(new string[] { "t", ((int)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds).ToString() });
                _webClient.GetJson(string.Format("http://{0}:{1}/gui/?{2}", _ServerName, _ServerPort, string.Join("&", param.Select(x => string.Join("=", x)))));
            }

            return true;
        }
    }
}
