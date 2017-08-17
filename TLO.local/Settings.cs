using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using NLog.Config;

namespace TLO.local
{
    public class Settings
    {
        private DateTime _LastWriteTime;
        private static Settings _data;

        public string FileSettings
        {
            get { return Path.Combine(Folder, "TLO.local.Settings.xml"); }
        }
        public string Folder
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
        }

        public void Save()
        {
            lock (this)
            {
                try
                {
                    if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(FileSettings)))
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FileSettings));
                    using (System.IO.Stream fs = System.IO.File.Open(FileSettings, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
                    {
                        this.LogLevel = this.LogLevel.HasValue ? this.LogLevel.Value : 0;
                        System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
                        serializer.Serialize(fs, this);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
                _LastWriteTime = System.IO.File.GetLastWriteTime(FileSettings);
            }
        }
        public void Read()
        {
            try
            {
                lock (this)
                {
                    using (System.IO.Stream fs = System.IO.File.Open(FileSettings, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                    {
                        System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
                        var result = (Settings)serializer.Deserialize(fs);

                        this.IsUpdateStatistics = result.IsUpdateStatistics;
                        this.CountDaysKeepHistory = result.CountDaysKeepHistory;
                        this.PeriodRunAndStopTorrents = result.PeriodRunAndStopTorrents;
                        this.CountSeedersReport = result.CountSeedersReport;
                        this.IsAvgCountSeeders = result.IsAvgCountSeeders;
                        this.KeeperName = result.KeeperName;
                        this.KeeperPass = result.KeeperPass;
                        this.IsSelectLessOrEqual = result.IsSelectLessOrEqual;
                        this.IsNotSaveStatistics = result.IsNotSaveStatistics;
                        this.LastUpdateTopics = result.LastUpdateTopics;
                        this.ReportTop1 = result.ReportTop1.Replace("\n", "\r\n").Replace("\r\r", "\r");
                        this.ReportTop2 = result.ReportTop2.Replace("\n", "\r\n").Replace("\r\r", "\r");
                        this.ReportLine = result.ReportLine.Replace("\n", "\r\n").Replace("\r\r", "\r");
                        this.ReportBottom = result.ReportBottom;
                        //this.ReportsCountSeeders = result.ReportsCountSeeders;

                        //this.TorrentClients = result.TorrentClients;
                        //this.Folders = result.Folders;
                        //this.Categories = result.Categories;
                        //this.ReportForumPages = result.ReportForumPages;
                        SetLogger(result.LogLevel.HasValue ? result.LogLevel.Value : 0);

                        this._LastWriteTime = System.IO.File.GetLastWriteTime(FileSettings);
                    }
                }
            }
            catch
            {
                Save();
            }
        }
        public void Checking()
        {
            DateTime lastWriteTime = System.IO.File.GetLastWriteTime(FileSettings);
            if (lastWriteTime != _LastWriteTime)
                Read();
        }
        public static Settings Current
        {
            get
            {
                if (_data == null)
                {
                    _data = new Settings();
                }
                _data.Checking();
                return _data;
            }
        }

        public Settings()
        {
            KeeperName = string.Empty;
            KeeperPass = string.Empty;
            CountDaysKeepHistory = 7;
            PeriodRunAndStopTorrents = 60;
            CountSeedersReport = 10;
            IsSelectLessOrEqual = true;
            IsNotSaveStatistics = true;
            ReportLine = "[*] %%Status%% [url=http://rutracker.org/forum/viewtopic.php?t=%%ID%%]%%Name%%[/url] %%Size%%";
            ReportTop1 = @"[b]Актуально на:[/b] %%CreateDate%%

Общее количество хранимых раздач подраздела: %%CountTopics%% шт. (%%SizeTopics%%)";
            ReportTop2 = @"%%Top1%%[spoiler=""Раздачи, взятые на хранение, №№ %%NumberTopicsFirst%% - %%NumberTopicsLast%%""]
[list=1]
%%ReportLines%%
[/list]
[/spoiler]";
            ReportBottom = "";
        }
        #region Логирование
        private static NLog.Logger _logger = NLog.LogManager.GetLogger("Settings");
        [XmlElement]
        public int? LogLevel { get; set; }
        private void SetLogger(int logLevel)
        {
            if (LogLevel.HasValue && LogLevel.Value == logLevel)
                return;
            string exeFileName = "BI.Analytics.Expert.Other";
            if (Assembly.GetEntryAssembly() != null)
                exeFileName = Assembly.GetEntryAssembly().ManifestModule.Name;

            LoggingConfiguration config = new LoggingConfiguration();
            var fileTarget = new NLog.Targets.FileTarget();
            fileTarget.Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss}\t${level}\t${message}";
            config.AddTarget("logfile", fileTarget);

            fileTarget.FileName = System.IO.Path.Combine(this.Folder, exeFileName + ".log");
            fileTarget.Encoding = Encoding.UTF8;
            fileTarget.ArchiveAboveSize = 1024 * 1024 * 20;
            LoggingRule rule = null;
            if (System.Environment.UserInteractive)
            {
                var consoleTarget = new NLog.Targets.ColoredConsoleTarget();
                config.AddTarget("console", consoleTarget);
                consoleTarget.Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss}\t${level}\t${message}";

                rule = new LoggingRule("*", NLog.LogLevel.Debug, consoleTarget);
                config.LoggingRules.Add(rule);
            }
            if (logLevel <= 0)
            //    rule = new LoggingRule("*", NLog.LogLevel.Error, fileTarget);
            //else if (logLevel <= 0)
                rule = new LoggingRule("*", NLog.LogLevel.Warn, fileTarget);
            else if (logLevel == 1)
                rule = new LoggingRule("*", NLog.LogLevel.Info, fileTarget);
            else if (logLevel == 2)
                rule = new LoggingRule("*", NLog.LogLevel.Debug, fileTarget);
            else
                rule = new LoggingRule("*", NLog.LogLevel.Trace, fileTarget);
            config.LoggingRules.Add(rule);

            NLog.LogManager.Configuration = config;


            _logger.Info(String.Format("OS: {0} (Is64BitOperatingSystem: {1}, Version {2})", System.Environment.OSVersion.VersionString, System.Environment.Is64BitOperatingSystem, System.Environment.OSVersion.Version.ToString()));
            LogLevel = logLevel;
        }
        #endregion

        [XmlAttribute]
        public string KeeperName { get; set; }
        [XmlAttribute]
        public string KeeperPass { get; set; }
        [XmlAttribute]
        public bool IsUpdateStatistics { get; set; }
        [XmlAttribute]
        public int CountDaysKeepHistory { get; set; }
        [XmlAttribute]
        public int PeriodRunAndStopTorrents { get; set; }
        [XmlAttribute]
        public int CountSeedersReport { get; set; }
        [XmlAttribute]
        public bool IsAvgCountSeeders { get; set; }
        [XmlAttribute]
        public bool IsSelectLessOrEqual { get; set; }
        [XmlAttribute]
        public bool IsNotSaveStatistics { get; set; }
        [XmlAttribute]
        public DateTime LastUpdateTopics { get; set; }
        [XmlElement]
        public string ReportTop1 { get; set; }
        [XmlElement]
        public string ReportTop2 { get; set; }
        [XmlElement]
        public string ReportLine { get; set; }
        [XmlElement]
        public string ReportBottom { get; set; }
    }
}
