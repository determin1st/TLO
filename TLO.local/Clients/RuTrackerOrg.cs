using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
//using System.Web.Script.Serialization;
using System.Xml;

namespace TLO.local
{
    class RuTrackerOrg
    {
        //class 

        NLog.Logger _logger = null;
        private TLOWebClient _webClient;
        private string _userName;
        private string _userPass;
        private int _keeperid;
        private string _apiid;
        private JsonSerializer jSerializer;


        private static RuTrackerOrg _current;
        public static RuTrackerOrg Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new RuTrackerOrg();
                }
                return _current;
            }
        }
        private RuTrackerOrg() : this(null, null) { }
        public RuTrackerOrg(string userName, string password)
        {
            jSerializer = new JsonSerializer();
            //jSerializer.MaxJsonLength = int.MaxValue;
            _userName = userName;
            _userPass = password;
            if (_logger == null) _logger = NLog.LogManager.GetLogger("RuTrackerOrg");
            if(!string.IsNullOrWhiteSpace(_userName) && !string.IsNullOrWhiteSpace(_userPass))
                ReadKeeperInfo();
        }

        public IEnumerable<Category> GetCategories()
        {
            var result = new List<Category>();
            //var str = DownloadArchivePage("http://api.rutracker.org/api/static/cat_forum_tree.json.gz");
            var str = DownloadArchivePage("http://api.rutracker.org/v1/static/cat_forum_tree");

            var obj = JsonConvert.DeserializeObject(str);
            var jobj = (obj as JObject)["result"].ToObject<JObject>();
            jobj["c"].ToObject<JObject>();
            result.AddRange(jobj["c"].ToObject<Dictionary<string, object>>()
                .Select(x =>
                {
                    return new Category()
                    {
                        CategoryID = 1000000 + int.Parse(x.Key),
                        Name = x.Value as string,
                    };
                }
                ).ToArray());
            result.AddRange((jobj["f"].ToObject<Dictionary<string, object>>())
                .Select(x =>
                {
                    return new Category()
                    {
                        CategoryID = int.Parse(x.Key),
                        Name = x.Value as string
                    };
                }
                ));
            var dict = result.ToDictionary(x => x.CategoryID, x => x);
            var tobj = (jobj["tree"].ToObject<JObject>());
            var counter = 0;
            foreach (var t1 in tobj)
            {
                var key1 = int.Parse(t1.Key) + 1000000;
                ++counter;
                dict[key1].OrderID = counter;
                dict[key1].FullName = dict[key1].Name;
                foreach (var t2 in t1.Value.ToObject<JObject>())
                {
                    var key2 = int.Parse(t2.Key);
                    ++counter;
                    if (dict.ContainsKey(key2))
                    {
                        var objKey2 = dict[key2];
                        objKey2.ParentID = key1;
                        objKey2.OrderID = counter;
                        objKey2.FullName = string.Format("{0} » {1}", (dict.ContainsKey(key1) ? dict[key1].Name : ""), objKey2.Name);
                    }
                    foreach (var t3 in t2.Value.ToObject<JArray>())
                    {
                        var key3 = (int)t3;
                        ++counter;
                        if (dict.ContainsKey(key3))
                        {
                            var objKey3 = dict[key3];
                            objKey3.ParentID = key2;
                            objKey3.OrderID = counter;
                            objKey3.FullName = string.Format("{0} » {1}", (dict.ContainsKey(key2) ? dict[key2].FullName : ""), objKey3.Name);
                        }
                    }
                }
            }


            return result;
        }
        public IEnumerable<Tuple<int, string>> GetCategoriesFromPost(string postUrl)
        {
            var result = new List<Tuple<int, string>>();

            var html = DownloadWebPage(postUrl);
            var t1 = html.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Contains("http://rutracker.org/forum/viewforum.php?f=") && x.Contains("class=\"postLink\"")
                ).ToArray();
            int? categoryId = null;
            string list = null;
            var categories = new HashSet<int>();
            foreach (var t in t1)
            {
                var tt01 = t.Split(new char[] { '"', '<', '>', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => x.Contains("http://rutracker.org/forum/viewforum.php?f=") || x.Contains("http://rutracker.org/forum/viewtopic.php?t=") || x.Contains("http://rutracker.org/forum/viewtopic.php?p="))
                    .ToArray();
                foreach (var tt in tt01)
                {
                    if (tt.Contains("http://rutracker.org/forum/viewforum.php?f="))
                        categoryId = int.Parse(tt.Replace("http://rutracker.org/forum/viewforum.php?f=", ""));
                    if (categoryId == 2020)
                        Console.Write("");
                    if (tt.Contains("http://rutracker.org/forum/viewtopic.php?t=") && tt != "http://rutracker.org/forum/viewtopic.php?t=")
                        list = tt;
                    if (tt.Contains("http://rutracker.org/forum/viewtopic.php?p="))
                        list = GetTopicUrlByPostUrl(tt);
                }
                if (categoryId.HasValue && !string.IsNullOrWhiteSpace(list))
                    result.Add(new Tuple<int, string>(categoryId.Value, list));
                categoryId = null;
                list = null;
            }

            return result;

        }
        public string GetTopicUrlByPostUrl(string postUrl)
        {
            var html = DownloadWebPage(postUrl);
            if (html.Contains("<div class=\"mrg_16\">Тема не найдена</div>")) return null;
            return html.Split(new char[] { '"' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => x.Contains("http://rutracker.org/forum/viewtopic.php?t="))
                    .Select(x => x).FirstOrDefault();
        }
        //public int? GetTopicIdByPostUrl(string postUrl)
        //{
        //    var url = GetTopicUrlByPostUrl(postUrl);
        //    if (!string.IsNullOrWhiteSpace(url.Replace("http://rutracker.org/forum/viewtopic.php?t=", "")))
        //        return int.Parse(url.Replace("http://rutracker.org/forum/viewtopic.php?t=", ""));
        //    return null;
        //}

        public int[][] GetTopicsStatus(int forumID)
        {
            //var str = DownloadArchivePage(string.Format("http://api.rutracker.org/api/static/pvc/f/{0}.json.gz", forumID));

            var str = DownloadArchivePage(string.Format("http://api.rutracker.org/v1/static/pvc/f/{0}", forumID));

            var obj = JsonConvert.DeserializeObject<JObject>(str)["result"].ToObject<Dictionary<int,int[]>>();
            //var status = (obj as Dictionary<string, object>)["result"] as Dictionary<string, object>;

            var result = new int[obj.Count][];
            int i = 0;
            foreach (var t in obj)
            {
                var v = t.Value;
                result[i] = new int[] { t.Key, v.Length > 1 ? v[1] : -1 };
                i++;
            }
            return result;
        }

        public List<TopicInfo> GetTopicsInfo(int[] topics)
        {
            if (topics == null || topics.Length == 0 || topics.Length > 100) return null;

            var result = new List<TopicInfo>();
            var str = DownloadArchivePage(string.Format("http://api.rutracker.org/v1/get_tor_topic_data?by=topic_id&val={0}", System.Web.HttpUtility.UrlEncode(string.Join(",", topics))));
            //var obj = JsonConvert.DeserializeObject(str);

            var obj = JsonConvert.DeserializeObject<JObject>(str)["result"].ToObject<Dictionary<int, Dictionary<string, object>>>();
            //var info = (obj as Dictionary<string, object>)["result"].ToObject<Dictionary<int, JObject>>();

            foreach (var inf in obj)
            {
                var topicinfo = new TopicInfo();
                topicinfo.TopicID = inf.Key;
                var v = inf.Value;
                if (v != null)
                {
                    topicinfo.Hash = v["info_hash"] as string;
                    topicinfo.CategoryID = int.Parse(v["forum_id"].ToString());
                    topicinfo.RegTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(int.Parse(v["reg_time"].ToString()));
                    topicinfo.Size = long.Parse(v["size"].ToString());//(long)v["size"];
                    topicinfo.Status = int.Parse(v["tor_status"].ToString());
                    topicinfo.Seeders = int.Parse(v["seeders"].ToString());
                    topicinfo.Name2 = v["topic_title"] as string;
                    topicinfo.PosterID = int.Parse(v["poster_id"].ToString());
                }
                result.Add(topicinfo);
            }
            Thread.Sleep(500);

            return result;
        }
        public IEnumerable<UserInfo> GetUsers(int[] id)
        {
            if (id == null || id.Count() == 0) return null;
            var result = new List<UserInfo>();

            var packets = new List<int>[id.Count() % 100 == 0 ? id.Count() / 100 : id.Count() / 100 + 1];
            for (int i = 0; i < id.Count(); ++i)
            {
                var index = i / 100;
                if (packets[index] == null) packets[index] = new List<int>();
                packets[index].Add(id[i]);
            }
            foreach (var packet in packets)
            {
                var str = DownloadArchivePage(string.Format("http://api.rutracker.org/v1/get_user_name?by=user_id&val={0}", System.Web.HttpUtility.UrlEncode(string.Join(",", packet))));
                var obj = JsonConvert.DeserializeObject<JObject>(str)["result"].ToObject<Dictionary<int, string>>();
                //var info = (obj as Dictionary<string, object>)["result"] as Dictionary<string, object>;

                foreach (var inf in obj)
                {
                    var user = new UserInfo();
                    user.UserID = inf.Key;
                    user.Name = inf.Value as string;
                    result.Add(user);
                }
                Thread.Sleep(500);
            }

            return result;
        }
        public List<int> GetPostsFromTopicId(int topicid)
        {
            //http://rutracker.org/forum/viewtopic.php?t=3707810&start=30
            var html=string.Empty;
            var posts = 0;
            var result = new List<int>();
            do
            {
                html = string.Empty;
                html = DownloadWebPage(string.Format("http://rutracker.org/forum/viewtopic.php?t={0}{1}",
                    topicid,
                    posts == 0 ? "" : "&start=" + posts.ToString()
                    ));
                if (html.Contains("<div class=\"mrg_16\">Тема не найдена</div>"))
                {
                    Thread.Sleep(500);
                    html = DownloadWebPage(string.Format("http://rutracker.org/forum/viewtopic.php?p={0}",
                    topicid
                    ));
                    if (html.Contains("<div class=\"mrg_16\">Тема не найдена</div>")) return new List<int>();
                    var topicidTemp = html.Split(new char[] { '"' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => x.Contains("http://rutracker.org/forum/viewtopic.php?t="))
                        .Select(x => x.Replace("http://rutracker.org/forum/viewtopic.php?t=", "")).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(topicidTemp))
                    {
                        topicid = int.Parse(topicidTemp);
                        continue;
                    }
                }

                var t1 = html.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains("\">[Цитировать]</a>")).ToArray();
                foreach (var tt1 in t1)
                {
                    var tt2 = tt1.Split(new char[] { '"' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains("http://")).FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(tt2)) continue;
                    var t2 = tt2.Split('=');
                    if (t2.Length < 3) continue;
                    result.Add(int.Parse(t2[2]));
                }

                posts += 30;
            } while (html.Contains("\">След.</a></b></p>") || posts == 0);
            return result;
        }

        internal Tuple<string, int, List<int>> GetTopicsFromReport(int postId, int categoryId)
        {
            Tuple<string, int, List<int>> result = null;
            var html = DownloadWebPage(string.Format("http://post.rutracker.org/forum/posting.php?mode=quote&p={0}", postId));
            var t1 = html.Split(new string[] { "<textarea", "</textarea>" }, StringSplitOptions.RemoveEmptyEntries);
            if (t1.Length < 2) return result;
            t1 = t1[1].Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains("http://rutracker.org/forum/viewtopic.php?t=") || x.Contains("quote=")).ToArray();
            foreach (var tt1 in t1)
            {
                try
                {
                    if (tt1.Contains("quote="))
                    {
                        var tt2 = tt1.Replace("quote=", "").Replace("\"", "");
                        result = new Tuple<string, int, List<int>>(tt2, categoryId, new List<int>());
                        continue;
                    }
                    else if (result == null) continue;
                    var t3 = tt1.Split('=');
                    if (t3.Length < 3) continue;
                    result.Item3.Add(int.Parse(t3[2]));
                }
                catch (Exception ex)
                {
                    _logger.Error("Ошибка получения информации о раздаче по адресу \"" + tt1 + "\": " + ex.Message);
                }
            }

            return result;
        }
        public Dictionary<string, Tuple<int, List<int>>> GetKeeps(int topicid, int categoryId)
        {
            var data = new Dictionary<string, Tuple<int, List<int>>>();

            var html = string.Empty;
            var posts = 0;
            var result = new List<int>();
            do
            {
                html = string.Empty;
                html = DownloadWebPage(string.Format("http://rutracker.org/forum/viewtopic.php?t={0}{1}",
                    topicid,
                    posts == 0 ? "" : "&start=" + posts.ToString()
                    ));
                if (html.Contains("<div class=\"mrg_16\">Тема не найдена</div>"))
                {
                    Thread.Sleep(500);
                    html = DownloadWebPage(string.Format("http://rutracker.org/forum/viewtopic.php?p={0}",
                    topicid
                    ));
                    if (html.Contains("<div class=\"mrg_16\">Тема не найдена</div>")) return data;
                    var topicidTemp = string.Join("\r\n", html.Split(new char[] { '\r', '\n'}).Where(x=>x.Contains("id=\"topic - title\"")))
                        .Split(new char[] { '"', '<', '>', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => x.Contains("http://rutracker.org/forum/viewtopic.php?t="))
                        .Select(x => x.Replace("http://rutracker.org/forum/viewtopic.php?t=", "")).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(topicidTemp))
                    {
                        _logger.Trace(topicidTemp);
                        topicid = int.Parse(topicidTemp);
                        continue;
                    }
                }

                var t1 = html.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Where(x =>
                    x.Contains("\t\t<a href=\"#\" onclick=\"return false;\">")
                    || (x.Contains("http://rutracker.org/forum/viewtopic.php?t=") && x.Contains("class=\"postLink\"") && !x.Contains("<div"))
                    ).ToArray();
                var keeperName = string.Empty;
                foreach (var tt1 in t1)
                {
                    if (tt1.Contains("\t\t<a href=\"#\" onclick=\"return false;\">"))
                    {
                        keeperName = tt1.Replace("\t\t<a href=\"#\" onclick=\"return false;\">", "").Replace("</a>", "");
                        //keeperName = keeperName.Replace("<wbr>", "");
                        continue;
                    }
                    if (!data.ContainsKey(keeperName))
                        data.Add(keeperName, new Tuple<int, List<int>>(categoryId, new List<int>()));

                    var tt2 = tt1.Split(new char[] { '"', '<', '>', ' ', '#', '&' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains("http://rutracker.org/forum/viewtopic.php?t=")).FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(tt2)) continue;
                    _logger.Trace(tt2);
                    var t2 = tt2.Split('=');
                    if (t2.Length < 2) continue;
                    try { data[keeperName].Item2.Add(int.Parse(t2[1])); }
                    catch (Exception ex) { _logger.Warn(topicid.ToString() + "\t" + t2[1] + "\t" + ex.Message); }
                }

                posts += 30;
            } while (html.Contains("\">След.</a></b></p>") || posts == 0);

            return data;
        }
        public Dictionary<string, Tuple<int, List<int>>> GetKeeps2(int topicid, int categoryId)
        {
            var data = new Dictionary<string, Tuple<int, List<int>>>();

            var reports = GetPostsFromTopicId(topicid);
            foreach (var r in reports)
            {
                var dt = GetTopicsFromReport(r, categoryId);
                if (dt == null || dt.Item3.Count == 0) continue;
                if (!data.ContainsKey(dt.Item1))
                    data.Add(dt.Item1, new Tuple<int, List<int>>(dt.Item2, new List<int>()));
                data[dt.Item1].Item2.AddRange(dt.Item3);
            }

            return data;
        }


        private string DownloadArchivePage(string page)
        {
            Exception e = null;
            for (int i = 0; i < 20; i++)
            {
                string htmlPage = string.Empty;

                var webClient = new TLOWebClient();

                try
                {
                    htmlPage = webClient.DownloadString(page);
                }
                catch(Exception ex)
                {
                    e = ex;
                    if (ex.Message.Contains("404")) throw ex;

                    Thread.Sleep(i * 1000);
                    continue;
                }

                return htmlPage;
            }

            throw new Exception("Не удалось скачать WEB-страницу за 20 попыток: " + e.Message, e);
        }
        public string DownloadWebPage(string page, params object[] param)
        {
            return Encoding.GetEncoding("windows-1251").GetString(DownloadWebPages(string.Format(page, param)));
        }
        public byte[] DownloadTorrentFile(int id)
        {
            for (int i = 0; i < 100; i++)
            {
                byte[] data = new byte[0];
                var htmlPage = string.Empty;
                TLOWebClient webClient = null;
                try
                {
                    if (_webClient == null)
                    {
                        webClient = new TLOWebClient();
                        //  Авторизовываемся
                        var str = string.Format("login_username={0}&login_password={1}&login={2}",
                            System.Web.HttpUtility.UrlEncode(_userName, Encoding.GetEncoding(1251)),
                            System.Web.HttpUtility.UrlEncode(_userPass, Encoding.GetEncoding(1251)),
                            "Вход");
                        htmlPage = Encoding.GetEncoding("windows-1251").GetString(webClient.UploadData("http://login.rutracker.org/forum/login.php", "POST", Encoding.GetEncoding(1251).GetBytes(str)));

                        Thread.Sleep(500);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex.Message);
                    _logger.Warn(ex);
                }

                if (!string.IsNullOrWhiteSpace(htmlPage))
                {
                    if (htmlPage.Contains("http://static.rutracker.org/captcha"))
                        throw new Exception("При авторизации требуется ввести текст с картинки. Авторизуйтесь на WEB-сайте, а потом повторите попытку");
                    if (htmlPage.Contains("<a href=\"profile.php?mode=register\"><b>Регистрация</b></a>"))
                        throw new Exception("Не удалось авторизоваться, проверьте логин и пароль");
                    _webClient = webClient;
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(_apiid))
                    {

                        var html = DownloadWebPage(string.Format(@"http://rutracker.org/forum/viewtopic.php?t={0}", id)).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var token = html.Where(x => x.Contains("form_token : '")).FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            token = token.Split(new char[] { '\'' }, StringSplitOptions.RemoveEmptyEntries)[1];
                        }

                        var str = string.Format("form_token={0}", token);

                        data = _webClient.UploadData(string.Format("http://dl.rutracker.org/forum/dl.php?t={0}", id), "POST", Encoding.GetEncoding(1251).GetBytes(str));
                    }
                    else
                    {
                        var str = string.Format("keeper_user_id={0}&keeper_api_key={1}&t={2}&add_retracker_url=0", _keeperid, _apiid, id);

                        data = _webClient.UploadData("http://dl.rutracker.org/forum/dl.php", "POST", Encoding.GetEncoding(1251).GetBytes(str));
                    }
                    

                }
                catch(Exception ex)
                {
                    if(i >=20)
                        throw new Exception("Не удалось скачать WEB-страницу за 20 попыток:" + ex.Message, ex);
                    Thread.Sleep(i * 1000);
                    continue;
                }

                htmlPage = Encoding.GetEncoding(1251).GetString(data).ToLower();

                if (htmlPage.ToLower().Contains("форум временно отключен") || htmlPage.Contains("форум временно отключен"))
                    throw new Exception("Форум временно отключен");

                if (htmlPage.Contains("http://static.rutracker.org/captcha") || htmlPage.Contains("<a href=\"profile.php?mode=register\"><b>регистрация</b></a>"))
                {
                    if (_webClient != null)
                        _webClient.Dispose();
                    _webClient = null;
                    continue;
                }

                if (htmlPage[0] != 'd')
                {
                    var name = Path.Combine(Settings.Current.Folder, "error_" + id.ToString() + ".html");
                    if (File.Exists(name)) File.Delete(name);
                    using (var file = File.Create(name))
                    {
                        file.Write(data, 0, data.Length);
                    }
                    return null;
                }

                return data;
            }
            return null;
        }
        public byte[] DownloadWebPages(string page)
        {
            for (int i = 0; i < 20; i++)
            {
                byte[] data = new byte[0];
                var htmlPage = string.Empty;
                TLOWebClient webClient = null;
                try
                {
                    if (_webClient == null)
                    {
                        webClient = new TLOWebClient(Encoding.GetEncoding(1251));
                        //  Авторизовываемся
                        if (!string.IsNullOrWhiteSpace(_userName) && !string.IsNullOrWhiteSpace(_userPass))
                        {
                            var str = string.Format("login_username={0}&login_password={1}&login={2}",
                                System.Web.HttpUtility.UrlEncode(_userName, Encoding.GetEncoding(1251)),
                                System.Web.HttpUtility.UrlEncode(_userPass, Encoding.GetEncoding(1251)),
                                "Вход");
                            htmlPage = Encoding.GetEncoding("windows-1251").GetString(webClient.UploadData("http://login.rutracker.org/forum/login.php", "POST", Encoding.GetEncoding(1251).GetBytes(str)));
                        }

                        Thread.Sleep(500);
                    }
                }
                catch { }

                if (!string.IsNullOrWhiteSpace(htmlPage) && !string.IsNullOrWhiteSpace(_userName) && !string.IsNullOrWhiteSpace(_userPass))
                {
                    if (htmlPage.Contains("http://static.rutracker.org/captcha"))
                        throw new Exception("При авторизации требуется ввести текст с картинки. Авторизуйтесь на WEB-сайте, а потом повторите попытку");
                    if (htmlPage.Contains("<a href=\"profile.php?mode=register\"><b>Регистрация</b></a>"))
                        throw new Exception("Не удалось авторизоваться, проверьте логин и пароль");
                }
                _webClient = webClient;

                try { data = _webClient.DownloadData(page); }
                catch
                {
                    Thread.Sleep(i * 1000);
                    continue;
                }

                htmlPage = Encoding.GetEncoding("windows-1251").GetString(data);
                if (htmlPage.ToLower().Contains("форум временно отключен") || htmlPage.ToLower().Contains("форум временно отключен"))
                    throw new Exception("Форум временно отключен");


                if (htmlPage.Contains("http://static.rutracker.org/captcha") || htmlPage.Contains("<a href=\"profile.php?mode=register\"><b>Регистрация</b></a>"))
                {
                    if (_webClient != null)
                    {
                        _webClient.Dispose();
                    }
                    _webClient = null;
                    continue;
                }

                return data;
            }

            throw new Exception("Не удалось скачать WEB-страницу за 20 попыток");
        }
        public byte[] DownloadArchiveData(string page)
        {
            for (int i = 0; i < 20; i++)
            {
                byte[] data = new byte[0];
                var htmlPage = string.Empty;

                if (_webClient == null)
                    _webClient = new TLOWebClient();

                try { data = _webClient.DownloadData(page); }
                catch
                {
                    Thread.Sleep(i * 1000);
                    continue;
                }

                htmlPage = Encoding.GetEncoding(1251).GetString(data).ToLower();

                if (htmlPage.Contains("введите ваше имя и пароль"))
                    return new byte[0];
                if (htmlPage.ToLower().Contains("форум временно отключен") || htmlPage.Contains("введите ваше имя и пароль"))
                    throw new Exception("Форум временно отключен");

                //  Если в начале нет буквы d, то это не торрент файл
                if(htmlPage[0] != 'd')
                    return new byte[0];

                return data;
            }

            throw new Exception("Не удалось скачать WEB-страницу за 20 попыток");
        }
        public void SavePage(string topicID, string folder)
        {
            var webClient = new TLOWebClient();
            var data = webClient.DownloadString(string.Format("http://rutracker.org/forum/viewtopic.php?t={0}", topicID));
            if (data.Contains("Тема не найдена"))
                return;
            using (var stream = File.Create(Path.Combine(folder, string.Format("{0}.html", topicID))))
            {
                using (var strWrt = new StreamWriter(stream, Encoding.GetEncoding(1251)))
                {
                    strWrt.Write(data);
                }
            }
        }

        public void SendReport(string url, string message)
        {
            if (url.Split('#').FirstOrDefault().Split('=').Length < 3)
                throw new ArgumentException("Не корректно указан адрес отправки отчета: " + url);

            var g_post = url.Split('#').FirstOrDefault().Split('=')[2];
            var html = DownloadWebPage(string.Format("http://post.rutracker.org/forum/posting.php?mode=editpost&p={0}", g_post));
            var pageEdit = html.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            //var fl = Path.Combine(Settings.Current.Folder, "post.html");
            //if (File.Exists(fl)) File.Delete(fl);
            //using (var strean = File.Create(fl))
            //{
            //    using (var strWrt = new StreamWriter(strean, Encoding.GetEncoding(1251)))
            //    {
            //        strWrt.Write(html);
            //    }
            //}

            Thread.Sleep(1000);
            //var values = new NameValueCollection();

            var str = string.Format("align=-1&codeColor=black&codeSize=12&codeUrl2=&decflag=2&f=1584&fontFace=-1&form_token=c2a9bace5d7f3900e2bddbf5f0f0f94a&message=&mode=editpost&p=59972538&submit_mode=submit&t=3985106");

            //values.Add("align", "-1");
            //values.Add("codeColor", "black");
            //values.Add("codeSize", "12");
            //values.Add("codeUrl2", "");
            //values.Add("decflag", "2");
            //values.Add("fontFace", "-1");
            //values.Add("message", System.Web.HttpUtility.UrlEncode(message, Encoding.GetEncoding(1251)));
            //values.Add("mode", "editpost");
            //values.Add("p", g_post);
            //values.Add("submit_mode", "submit");

            //subject

            var tmp = pageEdit.Where(x => x.Contains("name=\"f\" value=\"")).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(tmp))
                throw new ArgumentException("Параметр 'f' не найден на странице");
            //values.Add("f", tmp.Split('"')[5]);

            var tmp2 = pageEdit.Where(x => x.Contains("form_token : '")).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(tmp2))
                throw new ArgumentException("Параметр 'form_token' не найден на странице");
            //values.Add("form_token", tmp2.Split('"')[5]);

            var tmp3 = pageEdit.Where(x => x.Contains("name=\"t\" value=\"")).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(tmp3))
                throw new ArgumentException("Параметр 't' не найден на странице");
            //values.Add("t", tmp3.Split('"')[5]);

            if(tmp3.Split('"').Length < 6)
                throw new ArgumentException("Массив с параметром 't' меньше предполагаемого: " + tmp3);
            if (tmp2.Split('\'').Length < 2)
                throw new ArgumentException("Массив с параметром 'form_token' меньше предполагаемого: " + tmp2);
            if (tmp.Split('"').Length < 6)
                throw new ArgumentException("Массив с параметром 'f' меньше предполагаемого: " + tmp);

            var tmp4 = pageEdit.Where(x => x.Contains("name=\"subject\" ")).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tmp4) && tmp4.Split('"').Length < 12)
                throw new ArgumentException("Массив с параметром 'subject' меньше предполагаемого: " + tmp4);
            //str = string.Format("align=-1&codeColor=black&codeSize=12&codeUrl2=&decflag=2&f={4}&fontFace=-1&form_token={3}{5}&message={2}&mode=editpost&p={1}&submit_mode=submit&t={0}",
            str = string.Format(@"mode=editpost&f={4}&t={0}&p={1}&fontFace=-1&codeColor=black&codeSize=12&align=-1&codeUrl2&submit_mode=submit&decflag=2&form_token={3}{5}&message={2}",
                tmp3.Split('"')[5],
                g_post,
                System.Web.HttpUtility.UrlEncode(message, Encoding.GetEncoding(1251)),
                tmp2.Split('\'')[1],
                tmp.Split('"')[5],
                string.IsNullOrWhiteSpace(tmp4) ? string.Empty : string.Format("&subject={0}", System.Web.HttpUtility.UrlEncode(tmp4.Split('"')[11], Encoding.GetEncoding(1251)))
                );
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    //  Если вдруг проблема с авторизацией
                    if (_webClient == null) DownloadWebPage(string.Format("http://post.rutracker.org/forum/posting.php?mode=editpost&p={0}", g_post));
                    //  
                    _webClient.UploadData(string.Format("http://post.rutracker.org/forum/posting.php?mode=editpost&p={0}", g_post), "POST", Encoding.GetEncoding(1251).GetBytes(str));
                    break;
                }
                catch (Exception ex)
                {
                    if (i == 20)
                        throw new Exception("Не удалось отправить отчет за 10 попыток. Ошибка " + ex.Message);
                    Thread.Sleep(i * 1000);
                }
            }
            Thread.Sleep(1000);
        }

        public int GetUserIdByName(string name)
        {
            try
            {
                var html = DownloadWebPage(@"http://rutracker.org/forum/profile.php?mode=viewprofile&u={0}", name).Split(new char[] { '\r', '\n' }).Where(x => x.Contains("user_id")).ToArray();
                foreach (var id in html.Where(x => x.Contains(":")))
                {
                    try
                    {
                        var id2 = id.Split(new char[] { ' ', ':', '}', '\t' }, StringSplitOptions.RemoveEmptyEntries)[1];
                        return int.Parse(id2);
                    }
                    catch(Exception ex)
                    {
                        _logger.Trace(id + "\t" + ex.Message);
                    }
                }
                return 0;
            }
            catch(Exception ex)
            {
                _logger.Error("Не удалось скачать страницу профиля: " + ex.Message);
                return -1;
            }
        }
        public void ReadKeeperInfo()
        {
            try
            {
                //< td class="med">bt: <b>xTBxByqSek</b> api: <b>7EQHOWGX81</b></td>

                var html = DownloadWebPage(@"http://rutracker.org/forum/profile.php?mode=viewprofile&u={0}", _userName).Split(new char[] { '\r', '\n' }).Where(x => x.Contains("bt:") && x.Contains("api:")).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(html)) return;
                html = html.Split(new string[] { "<b>", "</b>" }, StringSplitOptions.RemoveEmptyEntries)[3];
                _apiid = html;
                _keeperid = GetUserIdByName(_userName);
            }
            catch { }
            _logger.Info("Результат авторизации: KeeperID: {0}; KeeperApiKey: {1}", _keeperid, _apiid);
        }
    }
}
