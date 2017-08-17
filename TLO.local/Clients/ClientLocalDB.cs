using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.IO;

namespace TLO.local
{
    class ClientLocalDB
    {
        private static ClientLocalDB _current;
        public static ClientLocalDB Current
        {
            get
            {
                if (_current == null)
                    _current = new ClientLocalDB();
                return _current;
            }
        }



        public string FileDatabase { get { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Database.db"); } }
        private SQLiteConnection _conn;

        private static NLog.Logger _logger;

        private ClientLocalDB()
        {
            if (_logger == null)
                _logger = NLog.LogManager.GetLogger("ClientServer");
            var cretateDatabase = false;
            if (!System.IO.File.Exists(FileDatabase))
                cretateDatabase = true;
            try
            {
                _logger.Info("Загрзка базы в память...");
                //_conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FileDatabase));
                using (var conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FileDatabase)))
                {
                    conn.Open();
                    _conn = new SQLiteConnection(string.Format("Data Source=:memory:;Version=3;", FileDatabase));
                    _conn.Open();
                    conn.BackupDatabase(_conn, "main", "main", -1, null, -1);

                    UpdateDataBase();
                    conn.Close();
                }
                _logger.Info("Загрзка базы в память завершена.");
            }
            catch
            {
                cretateDatabase = true;
            }

            if (cretateDatabase)
                CreateDatabase();
            SaveToDatabase();
        }

        public void SaveToDatabase()
        {
            try
            {
                if (File.Exists(FileDatabase + ".tmp"))
                    File.Delete(FileDatabase + ".tmp");
                //_conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FileDatabase));
                using (var conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FileDatabase + ".tmp")))
                {
                    conn.Open();

                    //_conn = new SQLiteConnection("Data Source=:memory:;Version=3;");
                    //_conn.Open();
                    _conn.BackupDatabase(conn, "main", "main", -1, null, -1);

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message + "\r\n" + ex.StackTrace);
            }
            if (File.Exists(FileDatabase + ".tmp"))
            {
                if (File.Exists(FileDatabase))
                    File.Delete(FileDatabase);
                File.Move(FileDatabase + ".tmp", FileDatabase);
            }
        }

        private void CreateDatabase()
        {
            if (_conn != null)
                _conn.Close();
            if (System.IO.File.Exists(FileDatabase)) System.IO.File.Delete(FileDatabase);
            //_conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FileDatabase));
            _conn = new SQLiteConnection(string.Format("Data Source=:memory:;Version=3;", FileDatabase));
            _conn.Open();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE Category(CategoryID INTEGER PRIMARY KEY ASC, ParentID INTEGER, OrderID INT, Name TEXT NOT NULL, FullName TEXT NOT NULL, IsEnable BIT, CountSeeders int, 
    TorrentClientUID TEXT, Folder TEXT, AutoDownloads INT, LastUpdateTopics DATETIME, LastUpdateStatus DATETIME, Label TEXT, ReportTopicID INT);
CREATE TABLE Topic (TopicID INT PRIMARY KEY ASC, CategoryID INT, Name TEXT, Hash TEXT, Size INTEGER, Seeders INT, AvgSeeders DECIMAL(18,4), Status INT, IsActive BIT, IsDeleted BIT, IsKeep BIT, IsKeepers BIT, IsBlackList BIT, IsDownload BIT, RegTime DATETIME, PosterID INT);
CREATE INDEX IX_Topic__Hash ON Topic (Hash);
CREATE TABLE TopicStatusHystory (TopicID INT NOT NULL, Date DateTime NOT NULL, Seeders INT, PRIMARY KEY(TopicID ASC, Date ASC));
CREATE TABLE TorrentClient(UID NVARCHAR(50) PRIMARY KEY ASC NOT NULL, Name NVARCHAR(100) NOT NULL, Type VARCHAR(50) NOT NULL, ServerName NVARCHAR(50) NOT NULL, ServerPort INT NOT NULL, UserName NVARCHAR(50), UserPassword NVARCHAR(50), LastReadHash DATETIME);
CREATE TABLE Report(CategoryID INT NOT NULL, ReportNo INT NOT NULL, URL TEXT, Report TEXT, PRIMARY KEY(CategoryID ASC, ReportNo ASC));
CREATE TABLE Keeper (KeeperName nvarchar(100) not null, CategoryID int not null, Count INT NOT NULL, Size DECIMAL(18,4) NOT NULL, PRIMARY KEY(KeeperName ASC, CategoryID ASC));
CREATE TABLE KeeperToTopic(KeeperName NVARCHAR(50) NOT NULL, CategoryID INT NULL, TopicID INT NOT NULL, PRIMARY KEY(KeeperName ASC, TopicID ASC));
CREATE TABLE User (UserID INT PRIMARY KEY ASC NOT NULL, Name NVARCHAR(100) NOT NULL);
";
                cmd.ExecuteNonQuery();
            }
        }
        public void ClearDatabase()
        {
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    var hash = new HashSet<int>();
                    cmd.Transaction = tran;
                    cmd.CommandText = "DELETE FROM Topic";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "DELETE FROM TopicStatusHystory";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "UPDATE Report SET Report = ''";
                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.CommandText = "vacuum;";
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public void UpdateDataBase()
        {
            using (var cmd = _conn.CreateCommand())
            {
                //try
                //{
                //    cmd.CommandText = "SELECT Label FROM Category LIMIT 1;";
                //    using (var rdr = cmd.ExecuteReader()) { while (rdr.Read()) { } }
                //}
                //catch
                //{
                //    cmd.CommandText = "ALTER TABLE Category ADD Label TEXT";
                //    cmd.ExecuteNonQuery();
                //}
                //try
                //{
                //    cmd.CommandText = "SELECT CategoryID FROM KeeperToTopic LIMIT 1;";
                //    using (var rdr = cmd.ExecuteReader()) { while (rdr.Read()) { } }
                //}
                //catch
                //{
                //    cmd.CommandText = "DELETE FROM KeeperToTopic";
                //    cmd.ExecuteNonQuery();
                //    cmd.CommandText = "ALTER TABLE KeeperToTopic ADD CategoryID INT NOT NULL";
                //    cmd.ExecuteNonQuery();
                //}
                //try
                //{
                //    cmd.CommandText = "SELECT ReportTopicID FROM Category LIMIT 1;";
                //    using (var rdr = cmd.ExecuteReader()) { while (rdr.Read()) { } }
                //}
                //catch
                //{
                //    cmd.CommandText = "ALTER TABLE Category ADD ReportTopicID INT";
                //    cmd.ExecuteNonQuery();
                //}
                //try
                //{
                //    cmd.CommandText = "SELECT PosterID FROM Topic LIMIT 1;";
                //    using (var rdr = cmd.ExecuteReader()) { while (rdr.Read()) { } }
                //}
                //catch
                //{
                //    cmd.CommandText = "ALTER TABLE Topic ADD PosterID INT";
                //    cmd.ExecuteNonQuery();
                //}
                //try
                //{
                //    cmd.CommandText = "SELECT * FROM User LIMIT 1;";
                //    using (var rdr = cmd.ExecuteReader()) { while (rdr.Read()) { } }
                //}
                //catch
                //{
                //    cmd.CommandText = "CREATE TABLE User (UserID INT PRIMARY KEY ASC NOT NULL, Name NVARCHAR(100) NOT NULL);";
                //    cmd.ExecuteNonQuery();
                //}
            }
        }

        public IEnumerable<UserInfo> GetUsers()
        {
            var result = new List<UserInfo>();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM User";
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        result.Add(new UserInfo()
                        {
                            UserID = rdr.GetInt32(0),
                            Name = rdr.GetString(1),
                        });
                    }
                }
            }
            return result;
        }
        public void SaveUsers(IEnumerable<UserInfo> data)
        {
            if (data == null || data.Count() == 0) return;
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"INSERT OR REPLACE INTO User(UserID, Name) VALUES(@UserID, @Name);";
                    cmd.Parameters.Add("@UserID", System.Data.DbType.Int32);
                    cmd.Parameters.Add("@Name", System.Data.DbType.String);
                    cmd.Prepare();
                    foreach (var u in data)
                    {
                        cmd.Parameters[0].Value = u.UserID;
                        cmd.Parameters[1].Value = u.Name ?? "<Удален>";
                        cmd.ExecuteNonQuery();
                    }
                }
                tran.Commit();
            }
        }
        public int[] GetNoUsers()
        {
            var result = new List<int>();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT DISTINCT t.PosterID
FROM 
     Topic AS t     
     LEFT JOIN User AS u ON (t.PosterID = u.UserID)     
WHERE
     t.PosterID IS NOT NULL AND u.Name IS NULL";
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        result.Add(rdr.GetInt32(0));
                }
            }
            return result.ToArray();
        }

        public void CategoriesSave(IEnumerable<Category> data, bool isLoad = false)
        {
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    var hash = new HashSet<int>();
                    cmd.Transaction = tran;
                    cmd.CommandText = string.Format(@"select CategoryID FROM Category WHERE CategoryID IN ({0})", string.Join(",", data.Select(x => x.CategoryID)));
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read()) hash.Add(rdr.GetInt32(0));
                    }
                    if (isLoad)
                    {
                        cmd.CommandText = @"UPDATE Category SET ParentID = @ParentID, OrderID = @OrderID, Name = @Name, FullName = @FullName WHERE CategoryID = @ID";
                        foreach (var c in data.Where(x => hash.Contains(x.CategoryID)))
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@ID", c.CategoryID);
                            cmd.Parameters.AddWithValue("@ParentID", c.ParentID);
                            cmd.Parameters.AddWithValue("@OrderID", c.OrderID);
                            cmd.Parameters.AddWithValue("@Name", c.Name);
                            cmd.Parameters.AddWithValue("@FullName", string.IsNullOrWhiteSpace(c.FullName) ? c.Name : c.FullName);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        cmd.CommandText = @"UPDATE Category SET IsEnable = 0 WHERE IsEnable = 1";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = @"UPDATE Category SET IsEnable = @IsEnable, Folder = @Folder, LastUpdateTopics = @LastUpdateTopics, LastUpdateStatus = @LastUpdateStatus, CountSeeders = @CountSeeders, TorrentClientUID = @TorrentClientUID, Label = @Label WHERE CategoryID = @ID";
                        foreach (var c in data.Where(x => hash.Contains(x.CategoryID)))
                        {
                            var folder = string.Format("{0}|{1}|{2}|{3}",
                                c.Folder,
                                c.CreateSubFolder,
                                c.IsSaveTorrentFiles ? "1" : "0",
                                c.IsSaveWebPage ? "1" : "0"
                                );

                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@ID", c.CategoryID);
                            cmd.Parameters.AddWithValue("@IsEnable", c.IsEnable);
                            cmd.Parameters.AddWithValue("@CountSeeders", c.CountSeeders);
                            cmd.Parameters.AddWithValue("@TorrentClientUID", c.TorrentClientUID.ToString());
                            cmd.Parameters.AddWithValue("@Folder", folder);
                            cmd.Parameters.AddWithValue("@LastUpdateTopics", c.LastUpdateTopics);
                            cmd.Parameters.AddWithValue("@LastUpdateStatus", c.LastUpdateStatus);
                            cmd.Parameters.AddWithValue("@Label", c.Label);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    cmd.CommandText = @"INSERT OR REPLACE INTO Category (CategoryID, ParentID, OrderID, Name, FullName, IsEnable, Folder, LastUpdateTopics, LastUpdateStatus, Label) 
VALUES(@ID, @ParentID, @OrderID, @Name, @FullName, @IsEnable, @Folder, @LastUpdateTopics, @LastUpdateStatus, @Label)";
                    foreach (var c in data.Where(x => !hash.Contains(x.CategoryID)))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@ID", c.CategoryID);
                        cmd.Parameters.AddWithValue("@ParentID", c.ParentID);
                        cmd.Parameters.AddWithValue("@OrderID", c.OrderID);
                        cmd.Parameters.AddWithValue("@Name", c.Name);
                        cmd.Parameters.AddWithValue("@FullName", string.IsNullOrWhiteSpace(c.FullName) ? c.Name : c.FullName);

                        cmd.Parameters.AddWithValue("@IsEnable", c.IsEnable);
                        cmd.Parameters.AddWithValue("@CountSeeders", c.CountSeeders);
                        cmd.Parameters.AddWithValue("@Folder", c.Folder);
                        cmd.Parameters.AddWithValue("@LastUpdateTopics", c.LastUpdateTopics);
                        cmd.Parameters.AddWithValue("@LastUpdateStatus", c.LastUpdateStatus);
                        cmd.Parameters.AddWithValue("@Label", c.Label);
                        cmd.ExecuteNonQuery();
                    }
                }
                tran.Commit();
            }
        }
        public List<Category> GetCategories()
        {
            var result = new List<Category>();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT CategoryID, ParentID, OrderID, Name, FullName, IsEnable, Folder, LastUpdateTopics, LastUpdateStatus, CountSeeders, TorrentClientUID, ReportTopicID, Label FROM Category";
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var obj = new Category()
                        {
                            CategoryID = rdr.GetInt32(0),
                            ParentID = rdr.GetInt32(1),
                            OrderID = rdr.GetInt32(2),
                            Name = rdr.GetString(3),
                            FullName = rdr.GetString(4),
                            IsEnable = rdr.GetBoolean(5),
                            CountSeeders = rdr.IsDBNull(9) ? 2 : rdr.GetInt32(9),
                            TorrentClientUID = rdr.IsDBNull(10) ? Guid.Empty : Guid.Parse(rdr.GetString(10)),
                            LastUpdateTopics = rdr.GetDateTime(7),
                            LastUpdateStatus = rdr.GetDateTime(8),
                            ReportList = rdr.IsDBNull(11) ? string.Empty : rdr.GetString(11),
                            Label = rdr.IsDBNull(12) ? string.Empty : rdr.GetString(12),
                        };
                        var folder = rdr.IsDBNull(6) ? null : rdr.GetString(6);
                        if (!string.IsNullOrWhiteSpace(folder))
                        {
                            var param = folder.Split(new char[] { '|' });
                            if (param.Length >= 1)
                                obj.Folder = param[0];
                            if (param.Length >= 2)
                                obj.CreateSubFolder = int.Parse(param[1]);
                            if (param.Length >= 3)
                            {
                                obj.IsSaveTorrentFiles = param[2] == "1" ? true : false;
                                obj.FolderTorrentFile = System.IO.Path.Combine(obj.Folder, "!!!Torrent-files!!!");
                            }
                            if (param.Length >= 4)
                            {
                                obj.IsSaveWebPage = param[3] == "1" ? true : false;
                                obj.FolderSavePageForum = System.IO.Path.Combine(obj.Folder, "!!!Web-pages!!!");
                            }
                        }
                        result.Add(obj);
                    }
                }
            }
            return result;
        }
        public List<Category> GetCategoriesEnable()
        {
            var result = new List<Category>();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT CategoryID, ParentID, OrderID, Name, FullName, IsEnable, Folder, LastUpdateTopics, LastUpdateStatus, CountSeeders, TorrentClientUID, Label FROM Category WHERE IsEnable = 1 ORDER BY FullName";
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var obj = new Category()
                            {
                                CategoryID = rdr.GetInt32(0),
                                ParentID = rdr.GetInt32(1),
                                OrderID = rdr.GetInt32(2),
                                Name = rdr.GetString(3),
                                FullName = rdr.GetString(4),
                                IsEnable = rdr.GetBoolean(5),
                                CountSeeders = rdr.IsDBNull(9) ? 2 : rdr.GetInt32(9),
                                TorrentClientUID = rdr.IsDBNull(10) ? Guid.Empty : Guid.Parse(rdr.GetString(10)),
                                LastUpdateTopics = rdr.GetDateTime(7),
                                LastUpdateStatus = rdr.GetDateTime(8),
                                Label = rdr.IsDBNull(11) ? string.Empty : rdr.GetString(11),
                            };
                        var folder = rdr.IsDBNull(6) ? null : rdr.GetString(6);
                        if (!string.IsNullOrWhiteSpace(folder))
                        {
                            var param = folder.Split(new char[] { '|' });
                            if (param.Length >= 1)
                                obj.Folder = param[0];
                            if (param.Length >= 2)
                                obj.CreateSubFolder = int.Parse(param[1]);
                            if (param.Length >= 3)
                            {
                                obj.IsSaveTorrentFiles = param[2] == "1" ? true : false;
                                obj.FolderTorrentFile = System.IO.Path.Combine(obj.Folder, "!!!Torrent-files!!!");
                            }
                            if (param.Length >= 4)
                            {
                                obj.IsSaveWebPage = param[3] == "1" ? true : false;
                                obj.FolderSavePageForum = System.IO.Path.Combine(obj.Folder, "!!!Web-pages!!!");
                            }
                        }
                        result.Add(obj);
                    }
                }
            }
            return result;
        }


        public void ResetFlagsTopicDownloads()
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"UPDATE Topic SET IsKeep = 0, IsDownload = 0";
                cmd.ExecuteNonQuery();
            }
        }
        public void SaveTopicInfo(List<TopicInfo> data, bool isUpdateTopic = false)
        {
            var minRegTime = new DateTime(2000, 1, 1);
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;

                    if (isUpdateTopic)
                    {
                        cmd.CommandText = @"UPDATE Category SET LastUpdateTopics = @LastUpdateTopics WHERE CategoryID = @CategoryID";
                        foreach (var c in data.Select(x => x.CategoryID).Distinct())
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@CategoryID", c);
                            cmd.Parameters.AddWithValue("@LastUpdateTopics", DateTime.Now);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    cmd.CommandText = string.Format(@"SELECT TopicID FROM Topic WHERE TopicID IN ({0})", string.Join(",", data.Select(x => x.TopicID)));
                    var list = new List<int>();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                            list.Add(rdr.GetInt32(0));
                    }

                    if (isUpdateTopic)
                    {
                        cmd.CommandText = @"UPDATE Topic SET CategoryID = @CategoryID, Name = @Name, Hash = @Hash, Size = @Size, Seeders = @Seeders, Status = @Status, IsDeleted = @IsDeleted, RegTime = @RegTime, PosterID = @PosterID WHERE TopicID = @TopicID;";
                        foreach (var t in data.Where(x => list.Contains(x.TopicID)))
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@TopicID", t.TopicID);
                            cmd.Parameters.AddWithValue("@CategoryID", t.CategoryID);
                            cmd.Parameters.AddWithValue("@Name", t.Name2);
                            cmd.Parameters.AddWithValue("@Hash", t.Hash);
                            cmd.Parameters.AddWithValue("@Size", t.Size);
                            cmd.Parameters.AddWithValue("@Seeders", t.Seeders);
                            cmd.Parameters.AddWithValue("@Status", t.Status);
                            cmd.Parameters.AddWithValue("@IsDeleted", 0);
                            cmd.Parameters.AddWithValue("@RegTime", t.RegTime < minRegTime ? minRegTime : t.RegTime);
                            cmd.Parameters.AddWithValue("@PosterID", t.PosterID);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        cmd.CommandText = @"UPDATE Topic SET CategoryID = @CategoryID, Name = @Name, Hash = @Hash, Size = @Size, Seeders = @Seeders, Status = @Status, IsDeleted = @IsDeleted, IsKeep = @IsKeep, IsKeepers = @IsKeepers, IsBlackList = @IsBlackList, IsDownload = @IsDownload WHERE TopicID = @TopicID;";
                        foreach (var t in data.Where(x => list.Contains(x.TopicID)))
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@TopicID", t.TopicID);
                            cmd.Parameters.AddWithValue("@CategoryID", t.CategoryID);
                            cmd.Parameters.AddWithValue("@Name", t.Name2);
                            cmd.Parameters.AddWithValue("@Hash", t.Hash);
                            cmd.Parameters.AddWithValue("@Size", t.Size);
                            cmd.Parameters.AddWithValue("@Seeders", t.Seeders);
                            cmd.Parameters.AddWithValue("@Status", t.Status);
                            cmd.Parameters.AddWithValue("@IsDeleted", 0);
                            cmd.Parameters.AddWithValue("@IsKeep", t.IsKeep);
                            cmd.Parameters.AddWithValue("@IsKeepers", t.IsKeeper);
                            cmd.Parameters.AddWithValue("@IsBlackList", t.IsBlackList);
                            cmd.Parameters.AddWithValue("@IsDownload", t.IsDownload);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    cmd.CommandText = @"
INSERT OR REPLACE INTO Topic (TopicID, CategoryID, Name, Hash, Size, Seeders, Status, IsActive, IsDeleted, IsKeep, IsKeepers, IsBlackList, IsDownload, RegTime, PosterID)
VALUES(@TopicID, @CategoryID, @Name, @Hash, @Size, @Seeders, @Status, @IsActive, @IsDeleted, @IsKeep, @IsKeepers, @IsBlackList, @IsDownload, @RegTime, @PosterID);";
                    foreach (var t in data.Where(x => !list.Contains(x.TopicID)))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@TopicID", t.TopicID);
                        cmd.Parameters.AddWithValue("@CategoryID", t.CategoryID);
                        cmd.Parameters.AddWithValue("@Name", t.Name2);
                        cmd.Parameters.AddWithValue("@Hash", t.Hash);
                        cmd.Parameters.AddWithValue("@Size", t.Size);
                        cmd.Parameters.AddWithValue("@Seeders", t.Seeders);
                        cmd.Parameters.AddWithValue("@Status", t.Status);
                        cmd.Parameters.AddWithValue("@IsActive", 1);
                        cmd.Parameters.AddWithValue("@IsDeleted", 0);
                        cmd.Parameters.AddWithValue("@IsKeep", t.IsKeep);
                        cmd.Parameters.AddWithValue("@IsKeepers", t.IsKeeper);
                        cmd.Parameters.AddWithValue("@IsBlackList", t.IsBlackList);
                        cmd.Parameters.AddWithValue("@IsDownload", t.IsDownload);
                        cmd.Parameters.AddWithValue("@RegTime", t.RegTime < minRegTime ? minRegTime : t.RegTime);
                        cmd.Parameters.AddWithValue("@PosterID", t.PosterID);
                        cmd.ExecuteNonQuery();
                    }
                }
                tran.Commit();
            }
            SaveStatus(data.Select(x => new int[] { x.TopicID, x.Seeders }).ToArray(), true);
        }
        internal void DeleteTopicsByCategoryId(int categoryID)
        {
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.Parameters.AddWithValue("@categoryID", categoryID);
                    cmd.CommandText = @"UPDATE Topic SET IsDeleted = 1 WHERE CategoryID = @categoryID;";
                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
            }
        }
        public void ClearHistoryStatus()
        {
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("@Date", System.Data.DbType.DateTime);
                    cmd.Parameters[0].Value = DateTime.Now.Date.AddDays(-Settings.Current.CountDaysKeepHistory);
                    cmd.CommandText = @"DELETE FROM TopicStatusHystory WHERE Date <= @Date;";
                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
            }
        }
        public void SaveStatus(int[][] data, bool isUpdateStatus = false)
        {
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE Topic SET Seeders = @Seeders WHERE TopicID = @TopicID;
INSERT OR REPLACE INTO TopicStatusHystory VALUES(@TopicID, @Date, @Seeders);
";

                    if (Settings.Current.IsNotSaveStatistics)
                        cmd.CommandText = @"UPDATE Topic SET Seeders = @Seeders WHERE TopicID = @TopicID;";
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add("@Date", System.Data.DbType.DateTime);
                    cmd.Parameters.Add("@TopicID", System.Data.DbType.Int32);
                    cmd.Parameters.Add("@Seeders", System.Data.DbType.Int32);
                    cmd.Parameters[0].Value = DateTime.Now;
                    foreach (var t in data)
                    {
                        cmd.Parameters[1].Value = t[0];
                        cmd.Parameters[2].Value = t[1];
                        cmd.ExecuteNonQuery();
                    }
                    //if (isUpdateStatus)
                    //{
                    //    var list = new List<int>();
                    //    cmd.CommandText = string.Format(@"SELECT DISTINCT CategoryID FROM Topic WHERE TopicID IN ({0})", string.Join(",", data.Select(x => x[0])));
                    //    using (var rdr = cmd.ExecuteReader())
                    //    {
                    //        while (rdr.Read()) list.Add(rdr.GetInt32(0));
                    //    }
                    //    cmd.CommandText = @"UPDATE Category SET LastUpdateStatus = @LastUpdateStatus WHERE CategoryID = @CategoryID";
                    //    foreach (var c in list)
                    //    {
                    //        cmd.Parameters.Clear();
                    //        cmd.Parameters.AddWithValue("@CategoryID", c);
                    //        cmd.Parameters.AddWithValue("@LastUpdateStatus", DateTime.Now);
                    //        cmd.ExecuteNonQuery();
                    //    }
                    //}
                }
                tran.Commit();
            }
        }

        public List<TopicInfo> GetTopicsByCategory(int categoyid)
        {
            var minRegTime = new DateTime(2000, 1, 1);

            var result = new List<TopicInfo>();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT TopicID, CategoryID, Name, Hash, Size, Seeders, Status, IsActive, IsDeleted, IsKeep, IsKeepers, IsBlackList, IsDownload, AvgSeeders, RegTime, PosterID
FROM Topic WHERE (CategoryID = @CategoryID OR @CategoryID = -1) AND IsDeleted = 0 AND Status NOT IN (7,4,11,5) and Hash IS NOT NULL";
                cmd.Parameters.AddWithValue("@CategoryID", categoyid);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        result.Add(new TopicInfo()
                        {
                            TopicID = rdr.GetInt32(0),
                            CategoryID = rdr.GetInt32(1),
                            Name2 = rdr.GetString(2),
                            Hash = rdr.GetString(3),
                            Size = rdr.GetInt64(4),
                            Seeders = rdr.GetInt32(5),
                            Status = rdr.GetInt32(6),
                            IsKeep = rdr.GetBoolean(9),
                            IsKeeper = rdr.GetBoolean(10),
                            IsBlackList = rdr.GetBoolean(11),
                            IsDownload = rdr.GetBoolean(12),
                            AvgSeeders = rdr.IsDBNull(13) ? null : (decimal?)rdr.GetDecimal(13),
                            RegTime = rdr.IsDBNull(14) ? minRegTime : rdr.GetDateTime(14),
                            PosterID = rdr.IsDBNull(15) ? 0 : rdr.GetInt32(15),
                        });
                }
            }
            return result;
        }
        public List<TopicInfo> GetTopicsAllByCategory(int categoyid)
        {
            var minRegTime = new DateTime(2000, 1, 1);

            var result = new List<TopicInfo>();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT TopicID, CategoryID, Name, Hash, Size, Seeders, Status, IsActive, IsDeleted, IsKeep, IsKeepers, IsBlackList, IsDownload, AvgSeeders, RegTime, PosterID
FROM Topic WHERE (CategoryID = @CategoryID OR @CategoryID = -1) and Hash is null";
                cmd.Parameters.AddWithValue("@CategoryID", categoyid);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        result.Add(new TopicInfo()
                        {
                            TopicID = rdr.GetInt32(0),
                            CategoryID = rdr.GetInt32(1),
                            Name2 = rdr.GetString(2),
                            Hash = rdr.GetString(3),
                            Size = rdr.GetInt64(4),
                            Seeders = rdr.GetInt32(5),
                            Status = rdr.GetInt32(6),
                            IsKeep = rdr.GetBoolean(9),
                            IsKeeper = rdr.GetBoolean(10),
                            IsBlackList = rdr.GetBoolean(11),
                            IsDownload = rdr.GetBoolean(12),
                            AvgSeeders = rdr.IsDBNull(13) ? null : (decimal?)rdr.GetDecimal(13),
                            RegTime = rdr.IsDBNull(14) ? minRegTime : rdr.GetDateTime(14),
                            PosterID = rdr.IsDBNull(15) ? 0 : rdr.GetInt32(15),
                        });
                }
            }
            return result;
        }
        public List<TopicInfo> GetTopics(DateTime regTime, int categoyid, int? countSeeders, int? avgCountSeeders, bool? isKeep, bool? isKeepers, bool? isDownload, bool? isBlack, bool? isPoster)
        {
            var minRegTime = new DateTime(2000, 1, 1);

            var result = new List<TopicInfo>();

            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT DISTINCT t.TopicID, t.CategoryID, t.Name, Hash, Size, Seeders, Status, IsActive, IsDeleted, IsKeep, IsKeepers, IsBlackList, IsDownload, AvgSeeders, RegTime, CAST(CASE WHEN @UserName = u.Name THEN 1 ELSE 0 END AS BIT),
    CAST(CASE WHEN kt.TopicID IS NOT NULL THEN 1 ELSE 0 END AS BIT)
FROM 
    Topic AS t    
    LEFT JOIN User AS u ON (t.PosterID = u.UserID)
    LEFT JOIN KeeperToTopic AS kt ON (kt.TopicID = t.TopicID AND kt.KeeperName <> @UserName)
WHERE 
    t.CategoryID = @CategoryID 
    AND t.RegTime < @RegTime
    AND Status NOT IN (7, 4,11,5)
    " + (countSeeders.HasValue ? string.Format("AND Seeders {1} {0}", countSeeders.Value, (Settings.Current.IsSelectLessOrEqual ? " <= " : " = ")) : "") + @"
    " + (avgCountSeeders.HasValue ? string.Format("AND AvgSeeders {1} {0}", avgCountSeeders.Value, (Settings.Current.IsSelectLessOrEqual ? " <= " : " = ")) : "") + @"
    " + (isKeep.HasValue ? string.Format("AND IsKeep = {0}", (isKeep.Value ? 1 : 0)) : "") + @"
    " + (isKeepers.HasValue ? string.Format("AND CAST(CASE WHEN kt.TopicID IS NOT NULL THEN 1 ELSE 0 END AS BIT) = {0}", (isKeepers.Value ? 1 : 0)) : "") + @"
    " + (isDownload.HasValue ? string.Format("AND IsDownload = {0}", (isDownload.Value ? 1 : 0)) : "") + @"
    " + (isPoster.HasValue ? string.Format("AND @UserName = u.Name", (isPoster.Value ? 1 : 0)) : "") + @"
    " + string.Format("AND IsBlackList = {0}", (isBlack.HasValue && isBlack.Value ? 1 : 0)) + @"
    AND IsDeleted = 0
ORDER BY
    t.Seeders, t.Name";
                cmd.Parameters.AddWithValue("@CategoryID", categoyid);
                cmd.Parameters.AddWithValue("@RegTime", regTime);
                cmd.Parameters.AddWithValue("@UserName", string.IsNullOrWhiteSpace(Settings.Current.KeeperName) ? "-" : Settings.Current.KeeperName);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        result.Add(new TopicInfo()
                        {
                            TopicID = rdr.GetInt32(0),
                            CategoryID = rdr.GetInt32(1),
                            Name2 = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2),
                            Hash = rdr.IsDBNull(3) ? string.Empty : rdr.GetString(3),
                            Size = rdr.GetInt64(4),
                            Seeders = rdr.GetInt32(5),
                            Status = rdr.GetInt32(6),
                            IsKeep = rdr.GetBoolean(9),
                            IsKeeper = rdr.GetBoolean(16),
                            IsBlackList = rdr.GetBoolean(11),
                            IsDownload = rdr.GetBoolean(12),
                            AvgSeeders = (rdr.IsDBNull(13) ? null : (decimal?)Math.Round(rdr.GetDecimal(13), 3)),
                            RegTime = rdr.IsDBNull(14) ? minRegTime : rdr.GetDateTime(14),
                            IsPoster = rdr.GetBoolean(15),

                        });
                }
            }
            return result;
        }
        public void SetTorrentClientHash(List<TopicInfo> data)
        {
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"UPDATE Topic SET IsDownload = @IsDownload, IsKeep = @IsKeep WHERE Hash = @Hash;";
                    foreach (var t in data)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@Hash", t.Hash);
                        cmd.Parameters.AddWithValue("@IsDownload", t.IsDownload);
                        cmd.Parameters.AddWithValue("@IsKeep", t.IsKeep);
                        cmd.ExecuteNonQuery();
                    }
                }
                tran.Commit();
            }
        }
        

        public void SaveTorrentClients(IEnumerable<TorrentClientInfo> data, bool isUpdateList = false)
        {
            var tc = GetTorrentClients();
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    if (isUpdateList)
                    {
                        cmd.CommandText = @"DELETE FROM TorrentClient WHERE UID = @UID";
                        foreach (var t in tc.Where(x => !data.Select(y => y.UID).Contains(x.UID)))
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@UID", t.UID.ToString());
                            cmd.ExecuteNonQuery();
                        }
                    }

                    cmd.CommandText = @"UPDATE TorrentClient SET Name = @Name, Type = @Type, ServerName = @ServerName, ServerPort = @ServerPort, UserName = @UserName, UserPassword = @UserPassword, LastReadHash = @LastReadHash WHERE UID = @UID";
                    foreach (var t in data.Where(x => tc.Select(y => y.UID).Contains(x.UID)))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@UID", t.UID.ToString());
                        cmd.Parameters.AddWithValue("@Name", t.Name);
                        cmd.Parameters.AddWithValue("@Type", t.Type);
                        cmd.Parameters.AddWithValue("@ServerName", t.ServerName);
                        cmd.Parameters.AddWithValue("@ServerPort", t.ServerPort);
                        cmd.Parameters.AddWithValue("@UserName", t.UserName);
                        cmd.Parameters.AddWithValue("@UserPassword", t.UserPassword);
                        cmd.Parameters.AddWithValue("@LastReadHash", t.LastReadHash);
                        cmd.ExecuteNonQuery();
                    }
                    cmd.CommandText = @"INSERT INTO TorrentClient (UID, Name, Type, ServerName, ServerPort, UserName, UserPassword, LastReadHash) VALUES(@UID, @Name, @Type, @ServerName, @ServerPort, @UserName, @UserPassword, @LastReadHash)";
                    foreach (var t in data.Where(x => !tc.Select(y => y.UID).Contains(x.UID)))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@UID", t.UID.ToString());
                        cmd.Parameters.AddWithValue("@Name", t.Name);
                        cmd.Parameters.AddWithValue("@Type", t.Type);
                        cmd.Parameters.AddWithValue("@ServerName", t.ServerName);
                        cmd.Parameters.AddWithValue("@ServerPort", t.ServerPort);
                        cmd.Parameters.AddWithValue("@UserName", t.UserName);
                        cmd.Parameters.AddWithValue("@UserPassword", t.UserPassword);
                        cmd.Parameters.AddWithValue("@LastReadHash", t.LastReadHash);
                        cmd.ExecuteNonQuery();
                    }
                }
                tran.Commit();
            }
        }
        public List<TorrentClientInfo> GetTorrentClients()
        {
            var result = new List<TorrentClientInfo>();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM TorrentClient";
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        result.Add(new TorrentClientInfo()
                            {
                                UID = Guid.Parse(rdr.GetString(0)),
                                Name = rdr.GetString(1),
                                Type = rdr.GetString(2),
                                ServerName = rdr.GetString(3),
                                ServerPort = rdr.GetInt32(4),
                                UserName = rdr.GetString(5),
                                UserPassword = rdr.GetString(6),
                                LastReadHash = rdr.GetDateTime(7),
                            });
                    }
                }
            }
            return result;
        }

        public void SaveKeepOtherKeepers(Dictionary<string, Tuple<int, List<int>>> data)
        {
            if (data == null) return;
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandTimeout = 60 * 1000;
                    foreach (var dt in data)
                    {
                        //if (dt.Key == Settings.Current.KeeperName) continue;
                        var vl = dt.Value.Item2.Distinct().ToArray();

                        var top = new List<int>[vl.Length % 500 == 0 ? vl.Length / 500 : vl.Length / 500 + 1];
                        for (int i = 0; i < vl.Length; ++i)
                        {
                            var index = i / 500;
                            if (top[index] == null) top[index] = new List<int>();
                            top[index].Add(vl[i]);
                        }
                        foreach (var v in top)
                        {
                            cmd.Parameters.Clear();
                            cmd.CommandText = "INSERT OR REPLACE INTO KeeperToTopic(KeeperName, CategoryID, TopicID)\r\n" + string.Join("UNION ", v.Select(x => string.Format("SELECT @KeeperName, {2}, {1}\r\n", dt.Key, x, dt.Value.Item1)));
                            cmd.Parameters.AddWithValue("@KeeperName", dt.Key);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                tran.Commit();
            }
        }
        private void SaveKeepStatus(string keepName, List<Tuple<int, int, decimal>> data)
        {
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"INSERT OR REPLACE INTO Keeper VALUES(@KeeperName, @CategoryID, @Count, @Size)";
                    //cmd.Parameters.Add("@KeeperID", System.Data.DbType.Int32);
                    cmd.Parameters.Add("@KeeperName", System.Data.DbType.String);
                    cmd.Parameters.Add("@CategoryID", System.Data.DbType.Int32);
                    cmd.Parameters.Add("@Count", System.Data.DbType.Int64);
                    cmd.Parameters.Add("@Size", System.Data.DbType.Decimal);
                    if (string.IsNullOrWhiteSpace(keepName))
                        keepName = Settings.Current.KeeperName;
                    if (string.IsNullOrWhiteSpace(keepName))
                        return;
                    foreach (var s in data)
                    {
                        cmd.Parameters[0].Value = keepName;
                        cmd.Parameters[1].Value = s.Item1;
                        cmd.Parameters[2].Value = s.Item2;
                        cmd.Parameters[3].Value = Math.Round(s.Item3, 2);
                        cmd.ExecuteNonQuery();
                    }
                }
                tran.Commit();
            }
        }

        public void ClearReports()
        {
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    //  Пишем везде сообщение что оно удалено
                    cmd.CommandText = @"UPDATE Report SET Report = @Report WHERE ReportNo <> 0";
                    cmd.Parameters.AddWithValue("@Report", "Удалено");
                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
            }
        }
        public List<Tuple<int, string, int, decimal>> GetStatisticsByAllUsers()
        {
            var result = new List<Tuple<int, string, int, decimal>>();
            var isCurrent = GetTorrentClients().Any();
            using (var cmd = _conn.CreateCommand())
            {
                #region Обновляем статистику
                cmd.CommandText = @"INSERT OR REPLACE INTO Keeper SELECT 'All', CategoryID, COUNT(*) Cnt, SUM(Size) / 1073741824.0 Size FROM Topic WHERE IsDeleted = 0 AND CategoryID <> 0 GROUP BY CategoryID;
INSERT OR REPLACE INTO Keeper SELECT kt.KeeperName, kt.CategoryID, COUNT(*),  CAST(SUM(t.Size) / 1073741824.0  AS NUMERIC(18,4)) Size 
    FROM KeeperToTopic AS kt JOIN Topic AS t ON (kt.TopicID = t.TopicID AND kt.KeeperName <> @KeeperName) group by kt.KeeperName, kt.CategoryID;
INSERT OR REPLACE INTO Keeper SELECT @KeeperName, CategoryID,  COUNT(*) Cnt, CAST(SUM(Size) / 1073741824.0 AS NUMERIC(18,4)) Size FROM Topic 
        WHERE IsDeleted = 0 AND IsKeep = 1 AND (Seeders <= @Seeders OR @Seeders = -1) AND Status NOT IN (7, 4,11,5) AND IsBlackList = 0 GROUP BY CategoryID;
";
                cmd.Parameters.AddWithValue("@KeeperName", isCurrent ? Settings.Current.KeeperName : "<no>");
                cmd.Parameters.AddWithValue("@Seeders", Settings.Current.CountSeedersReport);
                cmd.ExecuteNonQuery();
                #endregion

                cmd.CommandText = @"SELECT KeeperName, CategoryID, Count, Size FROM Keeper";
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        result.Add(new Tuple<int, string, int, decimal>(rdr.GetInt32(1), rdr.GetString(0), rdr.GetInt32(2), Math.Round(rdr.GetDecimal(3), 3)));
                }
            }
            return result;
        }
        public void SaveReports(Dictionary<int, Dictionary<int, string>> reports)
        {
            foreach (var r1 in reports)
            {
                var maxKey = r1.Value.Keys.Max(x => x);
                r1.Value.Add(maxKey + 1, "Резерв");
                r1.Value.Add(maxKey + 2, "Резерв");
            }
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;

                    var reps = GetReports();
                    //  
                    if (reports.Any(x => !x.Value.ContainsKey(0)))
                    {
                        //  В категориях, которые существуют, на всех отчетах изменяем сообщения с "Удалено" на "Резерв"
                        cmd.CommandText = @"UPDATE Report SET Report = @Report WHERE CategoryID = @CategoryID AND ReportNo <> 0";
                        foreach (var r1 in reports)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@CategoryID", r1.Key);
                            cmd.Parameters.AddWithValue("@Report", "Резерв");
                            cmd.ExecuteNonQuery();
                        }
                    }

                    //  Заменяем сообщения "Резерв" на сами сообщения
                    cmd.CommandText = @"UPDATE Report SET Report = @Report WHERE CategoryID = @CategoryID AND ReportNo = @ReportNo";
                    foreach (var r1 in reports)
                    {
                        foreach (var r2 in r1.Value.Where(x => reps.ContainsKey(new Tuple<int, int>(r1.Key, x.Key))))
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@CategoryID", r1.Key);
                            cmd.Parameters.AddWithValue("@ReportNo", r2.Key);
                            cmd.Parameters.AddWithValue("@Report", r2.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    //  Добавляем новые сообщения
                    cmd.CommandText = @"INSERT OR REPLACE INTO Report VALUES(@CategoryID, @ReportNo, @URL, @Report)";
                    foreach (var r1 in reports)
                    {
                        foreach (var r2 in r1.Value.Where(x => !reps.ContainsKey(new Tuple<int, int>(r1.Key, x.Key))))
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@CategoryID", r1.Key);
                            cmd.Parameters.AddWithValue("@ReportNo", r2.Key);
                            cmd.Parameters.AddWithValue("@URL", string.Empty);
                            cmd.Parameters.AddWithValue("@Report", r2.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                tran.Commit();
            }
        }
        public Dictionary<Tuple<int, int>, Tuple<string, string>> GetReports(int? categoryID = null)
        {
            var result = new Dictionary<Tuple<int, int>, Tuple<string, string>>();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM Report";
                if (categoryID.HasValue)
                {
                    cmd.CommandText += " WHERE CategoryID = @CategoryID";
                    cmd.Parameters.AddWithValue("@CategoryID", categoryID.Value);
                }
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        if (result.ContainsKey(new Tuple<int, int>(rdr.GetInt32(0), rdr.GetInt32(1))))
                            continue;

                        result.Add(new Tuple<int, int>(rdr.GetInt32(0), rdr.GetInt32(1)), new Tuple<string, string>(rdr.GetString(2), rdr.GetString(3)));
                    }
                }
            }
            return result;
        }

        public void SaveSettingsReport(List<Tuple<int, int, string>> result)
        {
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = "SELECT DISTINCT CategoryID FROM Report WHERE ReportNo = 0";
                    var filter = new HashSet<int>();
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) filter.Add(rdr.GetInt32(0));

                    cmd.CommandText = @"UPDATE Report SET URL = @url WHERE CategoryID = @CategoryID AND ReportNo = @ReportNo";
                    cmd.Parameters.Add("@CategoryID", System.Data.DbType.Int32);
                    cmd.Parameters.Add("@ReportNo", System.Data.DbType.Int32);
                    cmd.Parameters.Add("@url", System.Data.DbType.String);
                    cmd.Prepare();
                    foreach (var r in result)
                    {
                        cmd.Parameters["@CategoryID"].Value = r.Item1;
                        cmd.Parameters["@ReportNo"].Value = r.Item2;
                        cmd.Parameters["@url"].Value = r.Item3;
                        cmd.ExecuteNonQuery();
                    }
                    cmd.CommandText = @"INSERT OR REPLACE INTO Report VALUES(@CategoryID, @ReportNo, @URL, '')";
                    cmd.Prepare();
                    foreach (var r in result.Where(x => !filter.Contains(x.Item1)))
                    {
                        cmd.Parameters["@CategoryID"].Value = r.Item1;
                        cmd.Parameters["@ReportNo"].Value = r.Item2;
                        cmd.Parameters["@url"].Value = r.Item3;
                        cmd.ExecuteNonQuery();
                    }
                }
                tran.Commit();
            }
        }

        public void UpdateStatistics()
        {
            using (var tran = _conn.BeginTransaction())
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    var data = new List<decimal[]>();
                    cmd.CommandText = @"update Topic SET AvgSeeders = (SELECT AVG(Seeders) FROM TopicStatusHystory AS st WHERE st.TopicID = Topic.TopicID)";
                    cmd.ExecuteNonQuery();
                    //using (var rdr = cmd.ExecuteReader())
                    //{
                    //    while (rdr.Read())
                    //        data.Add(new decimal[2] { rdr.GetDecimal(0), rdr.GetDecimal(1) });
                    //}
                    //var dataRdr = data.GroupBy(x => x[0]).Select(x => new Tuple<int, decimal>(x.Key, Math.Round(x.Average(y => (decimal)y[1]), 3))).ToList();

                    cmd.CommandText = @"UPDATE Topic SET AvgSeeders = @Seeders WHERE TopicID = @TopicID";
                    foreach (var s in data)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@TopicID", s[0]);
                        cmd.Parameters.AddWithValue("@Seeders", s[1]);
                        cmd.ExecuteNonQuery();
                    }
                }
                tran.Commit();
            }
        }

        public void ClearKeepers()
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"DELETE FROM Keeper;
DELETE FROM KeeperToTopic;
UPDATE Report SET Report = '' WHERE ReportNo = 0";
                cmd.ExecuteNonQuery();
            }
        }

        public void CreateReportByRootCategories()
        {
            try
            {
                using (var cmd = _conn.CreateCommand())
                {
                    GetStatisticsByAllUsers();
                    var reports = new Dictionary<int, Dictionary<int, string>>();
                    var dictByRootCategory = new Dictionary<int, Tuple<string, decimal, decimal>>();
                    var dictByKeeper = new Dictionary<Tuple<int, string>, Tuple<string, decimal, decimal>>();
                    var dictByCategories = new Dictionary<Tuple<int, string, int>, Tuple<string, decimal, decimal>>();
                    var lsCat = new List<Tuple<int, int, string, decimal, decimal>>();
                    cmd.CommandText = @"
SELECT c.CategoryID, c.FullName, SUM(Count)Count, SUM(Size)Size
FROM
    (
       SELECT CategoryID, ParentID FROM Category WHERE ParentID > 1000000 UNION
       SELECT c1.CategoryID, c2.ParentID FROM Category AS c1 JOIN Category AS c2 ON (c1.ParentID = c2.CategoryID) WHERE c2.ParentID > 1000000       
    ) AS t    
    JOIN Category AS c ON (t.ParentID = c.CategoryID)    
    JOIN Keeper AS k ON (k.CategoryID = t.CategoryID AND k.KeeperName <> 'All')
GROUP BY
      c.CategoryID, c.FullName
ORDER BY c.FullName";
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                            dictByRootCategory.Add(rdr.GetInt32(0), new Tuple<string, decimal, decimal>(rdr.GetString(1), rdr.GetDecimal(2), rdr.GetDecimal(3)));
                    }

                    cmd.CommandText = @"
SELECT c.CategoryID, c.FullName, k.KeeperName, SUM(Count)Count, SUM(Size)Size
FROM
    (
       SELECT CategoryID, ParentID FROM Category WHERE ParentID > 1000000 UNION
       SELECT c1.CategoryID, c2.ParentID FROM Category AS c1 JOIN Category AS c2 ON (c1.ParentID = c2.CategoryID) WHERE c2.ParentID > 1000000       
    ) AS t    
    JOIN Category AS c ON (t.ParentID = c.CategoryID)    
    JOIN Keeper AS k ON (k.CategoryID = t.CategoryID AND k.KeeperName <> 'All')
GROUP BY
      c.CategoryID, c.FullName, k.KeeperName
ORDER BY c.FullName, k.KeeperName";

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                            dictByKeeper.Add(new Tuple<int, string>(rdr.GetInt32(0), rdr.GetString(2)), new Tuple<string, decimal, decimal>(rdr.GetString(1), rdr.GetDecimal(3), rdr.GetDecimal(4)));
                    }
                    cmd.CommandText = @"
SELECT t.ParentID, c.CategoryID, c.FullName, k.KeeperName, SUM(Count)Count, SUM(Size)Size
FROM
    (
       SELECT CategoryID, ParentID FROM Category WHERE ParentID > 1000000 UNION
       SELECT c1.CategoryID, c2.ParentID FROM Category AS c1 JOIN Category AS c2 ON (c1.ParentID = c2.CategoryID) WHERE c2.ParentID > 1000000       
    ) AS t    
    JOIN Category AS c ON (t.CategoryID = c.CategoryID)    
    JOIN Keeper AS k ON (k.CategoryID = t.CategoryID AND k.KeeperName <> 'All')
GROUP BY
      t.ParentID, c.FullName, k.KeeperName, c.CategoryID
ORDER BY c.FullName, k.KeeperName";

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                            dictByCategories.Add(new Tuple<int, string, int>(rdr.GetInt32(0), rdr.GetString(3), rdr.GetInt32(1)), new Tuple<string, decimal, decimal>(rdr.GetString(2), rdr.GetDecimal(4), rdr.GetDecimal(5)));
                    }
                    cmd.CommandText = @"
SELECT t.ParentID, c.CategoryID, c.FullName,SUM(Count)Count, SUM(Size)Size
FROM
    (
       SELECT CategoryID, ParentID FROM Category WHERE ParentID > 1000000 UNION
       SELECT c1.CategoryID, c2.ParentID FROM Category AS c1 JOIN Category AS c2 ON (c1.ParentID = c2.CategoryID) WHERE c2.ParentID > 1000000       
    ) AS t    
    JOIN Category AS c ON (t.CategoryID = c.CategoryID)    
    JOIN Keeper AS k ON (k.CategoryID = t.CategoryID AND k.KeeperName <> 'All')
GROUP BY
      c.CategoryID, c.FullName
ORDER BY c.FullName";
                    using (var rdr = cmd.ExecuteReader())
                    {

                        while (rdr.Read())
                            lsCat.Add(new Tuple<int, int, string, decimal, decimal>
                                (
                                    rdr.GetInt32(0),
                                    rdr.GetInt32(1),
                                    rdr.GetString(2),
                                    rdr.GetDecimal(3),
                                    rdr.GetDecimal(4)
                                ));
                    }

                    foreach (var c in dictByRootCategory.Select(x => x.Key))
                    {
                        var sb = new StringBuilder();
                        //sb.AppendFormat("[b]{0}[/b]\r\n", dictByRootCategory[c].Item1);
                        sb.AppendFormat("[hr]\r\n[hr]\r\n[b][color=darkgreen][align=center][size=16]Статистика раздела: {0}[/size][/align][/color][/b][hr]\r\n[hr]\r\n\r\n", DateTime.Now.Date.ToString("dd.MM.yyyy"));
                        sb.AppendFormat("Всего: {0} шт. ({1:0.00} Гб.)\r\n\r\n", dictByRootCategory[c].Item2, dictByRootCategory[c].Item3);
                        sb.AppendLine("[hr]");
                        sb.AppendLine("[size=12][b]По хранителям:[/b][/size]");
                        int counter = 1;
                        foreach (var k in dictByKeeper.Where(x => x.Key.Item1 == c))
                        {
                            sb.AppendFormat("[spoiler=\"{0}. {1} - {2} шт. ({3:0.00} Гб.)\"]\r\n", counter, k.Key.Item2, k.Value.Item2, k.Value.Item3);
                            foreach (var t in dictByCategories.Where(x => x.Key.Item2 == k.Key.Item2 && x.Key.Item1 == c))
                            {
                                sb.AppendFormat("{0} - {1} шт. ({2:0.00} Гб.)\r\n", t.Value.Item1, t.Value.Item2, t.Value.Item3);
                            }
                            sb.AppendLine("[/spoiler]");
                            ++counter;
                        }
                        sb.AppendLine("[hr]");
                        sb.AppendLine("[size=12][b]По форумам:[/b][/size]");

                        foreach (var k in lsCat.Where(x => x.Item1 == c).OrderBy(x => x.Item3))
                        {
                            sb.AppendFormat("[spoiler=\"{0} - {1} шт. ({2:0.00} Гб.)\"]\r\n", k.Item3, k.Item4, k.Item5);
                            foreach (var t in dictByCategories.Where(x => x.Key.Item3 == k.Item2).OrderBy(x => x.Key.Item2))
                            {
                                sb.AppendFormat("{0} - {1} шт. ({2:0.00} Гб.)\r\n", t.Key.Item2, t.Value.Item2, t.Value.Item3);
                            }
                            sb.AppendLine("[/spoiler]");
                        }

                        reports.Add(c, new Dictionary<int, string>());
                        reports[c].Add(0, sb.ToString().Replace("<wbr>", ""));
                    }
                    SaveReports(reports);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
