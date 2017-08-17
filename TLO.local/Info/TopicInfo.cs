using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TLO.local
{
    class TopicInfo
    {
        protected static System.Globalization.CultureInfo _cultureUsInfo = new System.Globalization.CultureInfo("en-US");

        public int TopicID { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public string Hash { get; set; }

        public int CategoryID { get; set; }
        public string Name { get { return System.Web.HttpUtility.HtmlDecode(Name2); } }
        public string Name2 { get; set; }

        public string TorrentName { get; set; }
        public List<string> Files { get; set; }
        public int Status { get; set; }
        public long Size { get; set; }
        public DateTime RegTime { get; set; }
        public decimal? AvgSeeders { get; set; }
        public bool IsKeeper { get; set; }
        public bool IsKeep { get; set; }
        public bool IsDownload { get; set; }
        public bool IsBlackList { get; set; }
        public bool IsSelected { get; set; }
        public string Alternative { get { return ">>>>"; } }

        public bool? IsRun { get; set; }
        public bool IsPause { get; set; }
        public bool[] TorrentClientStatus { get; set; }
        public decimal PercentComplite { get; set; }
        public bool Checked { get; set; }


        public static string sizeToString(long size)
        {
            if (size >= 1073741824.0M)
                return (Math.Round((decimal)(size / 1073741824.0M), 2).ToString() + " GB");
            if (size >= 1048576.0M)
                return (Math.Round((decimal)(size / 1048576.0M), 2).ToString() + " MB");
            if (size >= 1024.0M)
                return (Math.Round((decimal)(size / 1024.0M), 2).ToString() + " KB");
            return (Math.Round((decimal)size, 2).ToString() + " B");
        }
        public string SizeToString
        {
            get
            {
                return TopicInfo.sizeToString(Size);
            }
            //set
            //{
            //    if (!string.IsNullOrWhiteSpace(value))
            //    {
            //        var str = value.Split(new string[] { "&nbsp;" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            //        if (str.Length != 2)
            //            return;
            //        var size = decimal.Parse(str[0].Replace(",", "."), _cultureUsInfo);
            //        switch (str[1])
            //        {
            //            case "TB":
            //                size = size * 1024 * 1024 * 1024 * 1024;
            //                break;
            //            case "GB":
            //                size = size * 1024 * 1024 * 1024;
            //                break;
            //            case "MB":
            //                size = size * 1024 * 1024;
            //                break;
            //            case "KB":
            //                size = size * 1024;
            //                break;

            //        }
            //        Size = Convert.ToInt64(Math.Round(size, 0));
            //    }
            //}
        }
        public string StatusToString
        {
            get
            {
                switch (Status)
                {
                    case 0: return "*";
                    case 1: return "x";
                    case 2: return "√";
                    case 3: return "?";
                    case 4: return "!";
                    case 5: return "D";
                    case 6: return "©";
                    case 7: return "∑";
                    case 8: return "#";
                    case 9: return "%";
                    case 10: return "T";
                    case 11: return "∏";
                    default: return "-";
                }
            }
        }
        public string RegTimeToString { get { return RegTime.ToString("dd.MM.yyyy"); } }

        public int PosterID { get; set; }
        public bool IsPoster { get; set; }
    }
}
