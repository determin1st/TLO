using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace TLO.local
{
    class TLOWebClient : WebClient
    {
        private static NLog.Logger _logger;
        private string _UserAgent = string.Empty;
        private string _Accept = string.Empty;
        private bool _IsJson;
        public TLOWebClient() : this(null, null, null) { }
        public TLOWebClient(Encoding encoding) : this(encoding, null, null) { }
        public TLOWebClient(Encoding encoding, string userAgent, string accept, bool isJson = false)
        {
            if (_logger == null)
                _logger = NLog.LogManager.GetCurrentClassLogger();

            this.Encoding = encoding == null ? encoding = Encoding.UTF8 : encoding;
            _UserAgent = string.IsNullOrWhiteSpace(userAgent) ? "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:35.0) Gecko/20100101 Firefox/35.0": userAgent;
            _Accept = string.IsNullOrWhiteSpace(accept) ? "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" : accept;

            CookieContainer = new CookieContainer();
            _IsJson = isJson;
        }
        public TLOWebClient(string userAgent)
        {
            _UserAgent = userAgent;
        }
        public CookieContainer CookieContainer { get; private set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);

            request.Accept = _IsJson ? "application/json" : _Accept;
            request.UserAgent = _UserAgent;
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
            if (_IsJson)
            {
                request.Headers.Add("X-Request", "JSON");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            }
            request.ContentType = "application/x-www-form-urlencoded";
            request.KeepAlive = true;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.Headers.Add("Pragma", "no-cache");
            request.Timeout = 1000 * 60 * 60;

            if (address.Host == "dl.rutracker.org" && address.AbsoluteUri.Contains("="))
            {
                var t = address.AbsoluteUri.Split(new char[]{'='}, StringSplitOptions.RemoveEmptyEntries);
                CookieContainer.Add(address, new Cookie("bb_dl", t[1]));
                request.Referer = string.Format(@"http://rutracker.org/forum/viewtopic.php?t={0}", t[1]);
            }
        //        Referer:        http://rutracker.org/forum/viewtopic.php?t=4764779
            //request.TransferEncoding = "windows-1251";

            //request.Method = "POST";
            request.CookieContainer = CookieContainer;
            //using (Stream postStream = request.GetRequestStream())
            //{
            //    using (var zipStream = new GZipStream(postStream, CompressionMode.Compress))
            //    {
            //        zipStream.Write(byteData, 0, byteData.Length);
            //    }
            //}
            return request;
        }

        public string GetString(string url)
        {
            _IsJson = false;

            return DownloadString(url);
        }
        public string GetJson(string url)
        {
            _IsJson = true;

            return DownloadString(url);
        }
    }
}
