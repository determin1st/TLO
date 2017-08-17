using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TLO.local
{
    class Category
    {
        public Category()
        {
            LastUpdateTopics = new DateTime(2000, 1, 1);
            LastUpdateStatus = new DateTime(2000, 1, 1);
            CountSeeders = 2;
            CreateSubFolder = 2;
        }
        public int CategoryID { get; set; }
        public int ParentID { get; set; }
        public int OrderID { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }

        public bool IsEnable { get; set; }
        public int CountSeeders { get; set; }
        public Guid TorrentClientUID { get; set; }

        public int CreateSubFolder { get; set; }
        public string Folder { get; set; }
        public string Label { get; set; }

        public bool IsSaveTorrentFiles { get; set; }
        public string FolderTorrentFile { get; set; }

        public bool IsSaveWebPage { get; set; }
        public string FolderSavePageForum { get; set; }
        public string ReportList { get; set; }

        public DateTime LastUpdateTopics { get; set; }
        public DateTime LastUpdateStatus { get; set; }

        public override string ToString()
        {
            return FullName;
        }
    }
}
