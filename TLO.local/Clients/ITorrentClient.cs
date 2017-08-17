using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TLO.local
{
    interface ITorrentClient
    {
        List<TopicInfo> GetAllTorrentHash();

        IEnumerable<string> GetFiles(TopicInfo topic);

        void DistributionStop(IEnumerable<string> data);
        void DistributionPause(IEnumerable<string> data);

        void DistributionStart(IEnumerable<string> data);

        bool Ping();

        bool SetDefaultFolder(string dir);
        bool SetDefaultLabel(string label);

        string GetDefaultFolder();

        void SendTorrentFile(string path, string file);
        void SendTorrentFile(string path, string filename, byte[] fdata);

        string[] GetTrackers(string hash);
        bool SetTrackers(string hash, string[] trackers);

        bool SetLabel(string hash, string label);
        bool SetLabel(IEnumerable<string> hash, string label);
    }
}
