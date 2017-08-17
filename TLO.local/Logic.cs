using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TLO.local
{
    class Logic
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static RuTrackerOrg _Current;
        public static RuTrackerOrg Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new RuTrackerOrg(Settings.Current.KeeperName, Settings.Current.KeeperPass);
                }
                return _Current;
            }
        }

        //public static void SaveTorrentFile(string url, string fileName)
        //{
        //    var data = Current.DownloadTorrentFile(url);
        //    if (data == null) return;
        //    var file = Path.Combine(@"e:\Книги и журналы » Гуманитарные науки » Литературоведение", fileName);
        //    if (File.Exists(file)) File.Delete(file);
        //    using (var stream = File.Create(file))
        //    {
        //        stream.Write(data, 0, data.Length);
        //    }
        //}

        public static void SendTorrentFileToTorrentClient(List<TopicInfo> topics, Category category)
        {
            var tci = ClientLocalDB.Current.GetTorrentClients().Where(x => x.UID == category.TorrentClientUID).FirstOrDefault();
            if (tci == null)
                return;
            var tc = tci.Create();
            if (tc == null)
                return;
            if (string.IsNullOrWhiteSpace(category.Folder))
                throw new Exception("В разделе не указан каталог для загрузки");

            foreach (var t in topics)
            {
                //  Раздача поглащена
                if (t.Status == 7 || t.Status == 4) continue;
                //if (tc.GetTrackers(t.Hash) != null) continue;

                if(category.CreateSubFolder == 0);
                else if(category.CreateSubFolder == 1)
                    tc.SetDefaultFolder(Path.Combine(category.Folder, t.TopicID.ToString()));
                else
                    throw new Exception("Не поддарживается указаный метод создания подкаталога");

                var data = new byte[0];
                if(data.Length == 0)
                    data = Current.DownloadTorrentFile(t.TopicID);
                if (data == null) return;
                tc.SendTorrentFile(category.CreateSubFolder == 1 ? Path.Combine(category.Folder, t.TopicID.ToString()) : category.Folder, string.Format("[rutracker.org].t{0}.torrent", t.TopicID), data);
                if (category.IsSaveTorrentFiles)
                {
                    if (!Directory.Exists(category.FolderTorrentFile)) Directory.CreateDirectory(category.FolderTorrentFile);
                    using (var stream = File.Create(Path.Combine(category.FolderTorrentFile, string.Format("[rutracker.org].t{0}.torrent", t.TopicID))))
                    {
                        stream.Write(data, 0, data.Count());
                    }
                }
                if (category.IsSaveWebPage)
                {
                    System.Threading.Thread.Sleep(500);
                    data = Current.DownloadWebPages(string.Format("http://rutracker.org/forum/viewtopic.php?t={0}", t.TopicID));
                    if (!Directory.Exists(category.FolderSavePageForum)) Directory.CreateDirectory(category.FolderSavePageForum);
                    using (var stream = File.Create(Path.Combine(category.FolderSavePageForum, string.Format("[rutracker.org].t{0}.html", t.TopicID))))
                    {
                        stream.Write(data, 0, data.Count());
                    }
                }
                System.Threading.Thread.Sleep(500);
            }
        }
        public static void SendTorrentFileToTorrentClient(TopicInfo topic, Category category)
        {
            if (topic == null) return;
            if (category == null) return;
            SendTorrentFileToTorrentClient(new List<TopicInfo>() { topic }, category);
        }

        public static void SendReportToForum()
        {
            foreach (var r in ClientLocalDB.Current.GetReports())
            {
                if (string.IsNullOrWhiteSpace(r.Value.Item1))
                    continue;

                Current.SendReport(r.Value.Item1, r.Value.Item2);
            }
        }
        public static void SendReportToForum(System.Windows.Forms.ProgressBar pBar)
        {
            var reports = ClientLocalDB.Current.GetReports();
            
            pBar.Visible = true;
            // Set Minimum to 1 to represent the first file being copied.
            pBar.Minimum = 1;
            // Set Maximum to the total number of files to copy.
            pBar.Maximum = reports.Count;
            // Set the initial value of the ProgressBar.
            pBar.Value = 1;
            // Set the Step property to a value of 1 to represent each file being copied.
            pBar.Step = 1;

            foreach (var r in reports)
            {
                if (!string.IsNullOrWhiteSpace(r.Value.Item1))
                    Current.SendReport(r.Value.Item1, r.Value.Item2);
                pBar.PerformStep();
            }
        }
        

        public static void LoadHashFromClients(List<TorrentClientInfo> clients = null)
        {
            if (clients == null)
                clients = ClientLocalDB.Current.GetTorrentClients();
            if (clients == null)
                return;
            foreach (var tc in clients)
            {
                try
                {
                    ITorrentClient tClient = tc.Create();
                    if (tClient == null) continue;
                    ClientLocalDB.Current.SetTorrentClientHash(tClient.GetAllTorrentHash());
                }
                catch { }
            }
        }
        public static void LoadHashFromClients(TorrentClientInfo client)
        {
            if (client == null)
                return;
            LoadHashFromClients(new List<TorrentClientInfo>() { client });
        }
        internal static void LoadHashFromClients(System.Windows.Forms.ProgressBar pBar)
        {
            var clients = ClientLocalDB.Current.GetTorrentClients();
            pBar.Visible = true;
            pBar.Minimum = 1;
            pBar.Maximum = clients.Count;
            pBar.Value = 1;
            pBar.Step = 1;
            foreach (var tc in clients)
            {
                try
                {
                    ITorrentClient tClient = tc.Create();
                    if (tClient != null)
                        ClientLocalDB.Current.SetTorrentClientHash(tClient.GetAllTorrentHash());
                }
                catch { }
                pBar.PerformStep();
            }
        }
        public static void LoadHashFromClients(Guid uid)
        {
            var client = ClientLocalDB.Current.GetTorrentClients().Where(x => x.UID == uid).FirstOrDefault();
            if (client == null)
                return;
            LoadHashFromClients(client);
            ClientLocalDB.Current.CreateReportByRootCategories();
        }

        public static void UpdateSeedersByCategories(List<Category> categories = null)
        {
            if (categories == null)
                categories = ClientLocalDB.Current.GetCategoriesEnable();
            if (categories == null) return;
            foreach (var c in categories)
            {
                ClientLocalDB.Current.SaveStatus(RuTrackerOrg.Current.GetTopicsStatus(c.CategoryID), true);
            }
        }
        public static void UpdateSeedersByCategory(Category category)
        {
            if (category == null) return;
            UpdateSeedersByCategories(new List<Category>() { category });
        }

        public static void UpdateTopicsByCategories(List<Category> categories = null)
        {
            if (categories == null)
                categories = ClientLocalDB.Current.GetCategoriesEnable();
            if (categories == null) return;
            foreach (var c in categories)
            {
                var topics = RuTrackerOrg.Current.GetTopicsStatus(c.CategoryID);
                ClientLocalDB.Current.SaveTopicInfo(RuTrackerOrg.Current.GetTopicsInfo(topics.Select(x => x[0]).Distinct().ToArray()), true);
            }
        }
        public static void UpdateTopicsByCategories(System.Windows.Forms.ProgressBar pBar)
        {
            var categories = ClientLocalDB.Current.GetCategoriesEnable();

            pBar.Visible = true;
            pBar.Minimum = 1;
            pBar.Maximum = categories.Count;
            pBar.Value = 1;
            pBar.Step = 1;

            foreach (var c in categories)
            {
                var topics = RuTrackerOrg.Current.GetTopicsStatus(c.CategoryID);
                ClientLocalDB.Current.SaveTopicInfo(RuTrackerOrg.Current.GetTopicsInfo(topics.Select(x => x[0]).Distinct().ToArray()), true);
                pBar.PerformStep();
            }
        }
        public static void UpdateTopicsByCategory(Category category)
        {
            if (category == null) return;
            UpdateTopicsByCategories(new List<Category>() { category });
        }

        public static void bwDownloadTorrentFiles(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            try
            {
                var arguments = e.Argument as Tuple<List<TopicInfo>, MainForm>;
                var topics = arguments.Item1;
                var folder = string.Empty;
                //  Если нечего загружать, то выходим
                if (topics == null || topics.Count == 0) return;
                //  Запрашиваем каталог для сохранения файлов
                arguments.Item2.Invoke((MethodInvoker)delegate
                {
                    var dlg = new FolderBrowserDialog();
                    if (dlg.ShowDialog() != DialogResult.OK)
                        return;
                    folder = dlg.SelectedPath;
                });
                //  Если каталог не найден - выдаем сообщение и выходим
                if (string.IsNullOrWhiteSpace(folder))
                {
                    arguments.Item2.Invoke((MethodInvoker)delegate { System.Windows.Forms.MessageBox.Show("Не указан каталог для сохранения торрент-файлов", "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1); });
                    return;
                }
                foreach (var t in topics)
                {
                    var data = Current.DownloadTorrentFile(t.TopicID);

                    if (data == null) continue;

                    using (var stream = File.Create(Path.Combine(folder, string.Format("[rutracker.org].t{0}.torrent", t.TopicID))))
                    {
                        stream.Write(data, 0, data.Count());
                    }

                    percentComplete += 100.0M / topics.Count;
                    worker.ReportProgress((int)percentComplete);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.Debug(ex);
                System.Windows.Forms.MessageBox.Show("Поизошла ошибка скачивании торрент-файлов:\r\n" + ex.Message, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
        }
        public static void bwSendTorrentFileToTorrentClient(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            try
            {
                var arguments = e.Argument as Tuple<MainForm, List<TopicInfo>, Category>;
                var topics = arguments.Item2;
                var category = arguments.Item3;
                logger.Info("Запущена задача на скачивание и добавление торрент-файлов в торрент-клиент...");
                logger.Trace(string.Format("\tКол-во раздач для скачивания торрент-файлов: {0}", topics.Count));

                var tci = ClientLocalDB.Current.GetTorrentClients().Where(x => x.UID == category.TorrentClientUID).FirstOrDefault();
                var torData = tci.Create().GetAllTorrentHash().Where(x => !string.IsNullOrWhiteSpace(x.Hash));
                foreach (var t in topics)
                {
                    var d = torData.Where(x => x.Hash == t.Hash).FirstOrDefault();
                    if (d == null) continue;
                    t.TorrentName = d.TorrentName;
                }
                var hashsFromTorrent = torData.Select(x => x.Hash).ToList();


                if (tci == null)
                {
                    logger.Warn("Не указан торрент-клиент в категории/подфоруме");
                    return;
                }

                var folder = category.Folder;
                if (string.IsNullOrWhiteSpace(folder))
                {
                    arguments.Item1.Invoke((MethodInvoker)delegate
                    {
                        var dlg = new FolderBrowserDialog();
                        if (dlg.ShowDialog() != DialogResult.OK)
                            return;
                        folder = dlg.SelectedPath;
                    });
                }

                if (string.IsNullOrWhiteSpace(folder))
                    throw new Exception("Не указан каталог для загрузки");

                foreach (var t in topics)
                {
                    try
                    {
                        //  Раздача поглащена
                        if (t.Status == 7 || t.Status == 4) continue;

                        var folder2 = string.Empty;
                        if (category.CreateSubFolder == 0)
                            folder2 = folder;
                        else if (category.CreateSubFolder == 1)
                            folder2 = Path.Combine(folder, t.TopicID.ToString());
                        else if (category.CreateSubFolder == 2)
                        {
                            DialogResult result = DialogResult.None;
                            arguments.Item1.Invoke((MethodInvoker)delegate
                                {
                                    var dlg = new TLO.local.Forms.FolderNameDialog();
                                    dlg.SelectedPath = t.Name;
                                    result = dlg.ShowDialog();
                                    folder2 = Path.Combine(folder, dlg.SelectedPath);
                                });
                            if (result == DialogResult.Abort)
                                return;
                            else if (result == DialogResult.Cancel)
                                continue;
                            else if (result != DialogResult.OK)
                                throw new Exception("result != DialogResult.OK");

                        }
                        else
                            throw new Exception("Не поддарживается указаный метод создания подкаталога");

                        byte[] data;

                        if (!hashsFromTorrent.Contains(t.Hash))
                        {
                            var tc = tci.Create();
                            if (tc == null)
                                throw new ArgumentException("Не удалось создать подключение к торрент-клиенту \"" + tci.Name + "\"");

                            tc.SetDefaultFolder(folder2);

                            data = Current.DownloadTorrentFile(t.TopicID);
                            if (data == null)
                            {
                                logger.Warn("Не удалось скачать торрент-файл для раздачи \"" + t.Name + "\". Статус раздачи: " + t.Status.ToString());
                                continue;
                            }
                            tc.SendTorrentFile(folder2, string.Format("[rutracker.org].t{0}.torrent", t.TopicID), data);
                            tc.SetLabel(t.Hash, string.IsNullOrWhiteSpace(category.Label) ? category.FullName : category.Label);
                            if (category.IsSaveTorrentFiles)
                            {
                                if (!Directory.Exists(category.FolderTorrentFile)) Directory.CreateDirectory(category.FolderTorrentFile);
                                using (var stream = File.Create(Path.Combine(category.FolderTorrentFile, string.Format("[rutracker.org].t{0}.torrent", t.TopicID))))
                                {
                                    stream.Write(data, 0, data.Count());
                                }
                            }
                        }
                        if (category.IsSaveWebPage)
                        {
                            System.Threading.Thread.Sleep(500);
                            data = Current.DownloadWebPages(string.Format("http://rutracker.org/forum/viewtopic.php?t={0}", t.TopicID));
                            if (!Directory.Exists(category.FolderSavePageForum)) Directory.CreateDirectory(category.FolderSavePageForum);
                            using (var stream = File.Create(Path.Combine(category.FolderSavePageForum, string.Format("[rutracker.org].t{0}.html", t.TopicID))))
                            {
                                stream.Write(data, 0, data.Count());
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(t.TorrentName))
                        {

                            try
                            {
                                if (Directory.Exists(Path.Combine(category.Folder, t.TorrentName)))
                                {
                                    if (!Directory.Exists(Path.Combine(category.Folder, t.TopicID.ToString())))
                                        Directory.CreateDirectory(Path.Combine(category.Folder, t.TopicID.ToString()));
                                    Directory.Move(Path.Combine(category.Folder, t.TorrentName), Path.Combine(category.Folder, t.TopicID.ToString(), t.TorrentName));
                                    continue;
                                }

                                if (File.Exists(Path.Combine(category.Folder, t.TorrentName)))
                                {
                                    if (!Directory.Exists(Path.Combine(category.Folder, t.TopicID.ToString())))
                                        Directory.CreateDirectory(Path.Combine(category.Folder, t.TopicID.ToString()));
                                    File.Move(Path.Combine(category.Folder, t.TorrentName), Path.Combine(category.Folder, t.TopicID.ToString(), t.TorrentName));
                                    continue;
                                }
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Не удалось скачать или добавить в торрент-клиент торрент-файл для раздачи \"" + t.Name + "\". Статус раздачи: " + t.Status.ToString() + "\t\t" + ex.Message);
                    }

                    percentComplete += 100.0M / topics.Count;
                    worker.ReportProgress((int)percentComplete);
                }
                logger.Info("Завершена задача на скачивание и добавление торрент-файлов в торрент-клиент.");
            }
            catch (Exception ex)
            {
                logger.Error("Произошла ошибка при скачивании и добавлении торрент-файлов в торрент-клиент: " + ex.Message);
                logger.Debug(ex);
                System.Windows.Forms.MessageBox.Show("Поизошла ошибка скачивании торрент-файлов:\r\n" + ex.Message, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
        }

        public static void bwSetLabels(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            try
            {
                var arguments = e.Argument as Tuple<MainForm, List<TopicInfo>, string>;
                var topics = arguments.Item2;
                var label = arguments.Item3;
                logger.Info("Запущена задача на установку пользовательских меток в торрент-клиент...");

                var clients = ClientLocalDB.Current.GetTorrentClients();
                foreach (var t in clients)
                {
                    try
                    {
                        var connect = t.Create();

                        var tcHash = connect.GetAllTorrentHash();
                        var tHash = (from tc in tcHash
                                     join tp in topics on tc.Hash equals tp.Hash
                                     select tp.Hash).ToArray();

                        connect.SetLabel(tHash, label);

                        percentComplete += 100.0M / clients.Count();
                        if (percentComplete <= 100) worker.ReportProgress((int)percentComplete);
                    }
                    catch { }
                }
                worker.ReportProgress(100);
            }
            catch (Exception ex)
            {
                logger.Error("Произошла ошибка при установке пользовательских меток в торрент-клиент: " + ex.Message);
                logger.Debug(ex);
                System.Windows.Forms.MessageBox.Show("Поизошла ошибка:\r\n" + ex.Message, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
        }
        public static void bwUpdateCountSeedersByAllCategories(object sender, DoWorkEventArgs e)
        {
            logger.Info("Запущена задача на обновление информации о кол-ве сидов на раздачах...");
            BackgroundWorker worker = sender as BackgroundWorker;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            //var iserror = false;
            try
            {
                logger.Trace("\t Очищаем историю о кол-ве сидов на раздаче...");
                ClientLocalDB.Current.ClearHistoryStatus();

                var categories = ClientLocalDB.Current.GetCategoriesEnable();
                foreach (var c in categories)
                {
                    logger.Trace("\t " + c.Name + "...");
                    try
                    {
                        ClientLocalDB.Current.SaveStatus(RuTrackerOrg.Current.GetTopicsStatus(c.CategoryID), true);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Не удалось обновить кол-во сидов по разделу \"" + c.Name + "\"");
                        logger.Debug(ex);
                        //iserror = true;
                    }
                    percentComplete += 100.0M / categories.Count;
                    worker.ReportProgress((int)percentComplete);
                }
                if (Settings.Current.IsUpdateStatistics)
                {
                    logger.Trace("\t Обновление статистики...");
                    ClientLocalDB.Current.UpdateStatistics();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.Debug(ex);
                //System.Windows.Forms.MessageBox.Show("Поизошла ошибка при отправке отчетов:\r\n" + ex.Message, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
            logger.Info("Завершена задача по обновлению информации о кол-ве сидов на раздачах.");
            //if (iserror)
            //    System.Windows.Forms.MessageBox.Show("Были ошибки при обновлении кол-ва сидов. Будут не актуальные данные.", "Внимание", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning, System.Windows.Forms.MessageBoxDefaultButton.Button1);
        }
        public static void bwUpdateHashFromAllTorrentClients(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            try
            {
                ClientLocalDB.Current.ResetFlagsTopicDownloads();
                var torrentclients = ClientLocalDB.Current.GetTorrentClients();

                foreach (var tc in torrentclients)
                {
                    try
                    {
                        ITorrentClient tClient = tc.Create();
                        if (tClient != null)
                            ClientLocalDB.Current.SetTorrentClientHash(tClient.GetAllTorrentHash());
                    }
                    catch(Exception ex)
                    {
                        logger.Warn("Не удалось загрузить список статусов раздач из torrent-клиента \"" + tc.Name + "\": \""+ ex.Message + "\". Возможно клиент не запущен или нет доступа.");
                        logger.Debug(ex);
                    }
                    percentComplete += 100.0M / torrentclients.Count;
                    worker.ReportProgress((int)percentComplete);
                }
                CreateReports();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.Debug(ex);
                System.Windows.Forms.MessageBox.Show("Поизошла ошибка при отправке отчетов:\r\n" + ex.Message, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
        }
        public static void bwUpdateHashFromTorrentClientsByCategoryUID(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            try
            {
                var category = e.Argument as Category;
                if (category == null) return;
                logger.Info("Обновление списка хранимого из торрент-клиента (по разделу)...");
                var torrentclients = ClientLocalDB.Current.GetTorrentClients().Where(x => x.UID == category.TorrentClientUID).ToList();

                foreach (var tc in torrentclients)
                {
                    try
                    {
                        ITorrentClient tClient = tc.Create();
                        if (tClient != null)
                            ClientLocalDB.Current.SetTorrentClientHash(tClient.GetAllTorrentHash());
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Не удалось загрузить список статусов раздач из torrent-клиента \"" + tc.Name + "\": \"" + ex.Message + "\". Возможно клиент не запущен или нет доступа.");
                        logger.Debug(ex);
                    }
                    percentComplete += 100.0M / torrentclients.Count;
                    worker.ReportProgress((int)percentComplete);
                }
                //logger.Info("Сборка отчетов...");
                CreateReports();
                logger.Info("Завершена задача по обновлению списка хранимого из торрент-клиента (по разделу).");
            }
            catch (Exception ex)
            {
                logger.Error("Произошла ошибка при обновлении списка хранимого из торрент-клиента: " + ex.Message);
                logger.Debug(ex);
                System.Windows.Forms.MessageBox.Show("Поизошла ошибка при отправке отчетов:\r\n" + ex.Message, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
        }
        public static void bwUpdateTopicsByCategory(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            var category = e.Argument as Category;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            try
            {
                var topics = RuTrackerOrg.Current.GetTopicsStatus(category.CategoryID).Select(x => x[0]).Distinct().ToArray();

                //  Разбиваем данные на пакеты
                var top = new List<int>[topics.Length % 100 == 0 ? topics.Length / 100 : topics.Length / 100 + 1];
                for (int i = 0; i < topics.Length; ++i)
                {
                    var index = i / 100;
                    if (top[index] == null) top[index] = new List<int>();
                    top[index].Add(topics[i]);
                }

                foreach (var t in top)
                {
                    ClientLocalDB.Current.SaveTopicInfo(RuTrackerOrg.Current.GetTopicsInfo(t.ToArray()), true);
                    percentComplete += 100.0M / top.Length;
                    worker.ReportProgress((int)percentComplete);
                }
                ClientLocalDB.Current.SaveUsers(RuTrackerOrg.Current.GetUsers(ClientLocalDB.Current.GetNoUsers()));
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.Debug(ex);
                System.Windows.Forms.MessageBox.Show("Поизошла ошибка при отправке отчетов:\r\n" + ex.Message, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
        }
        public static void bwUpdateTopicsByCategories(object sender, DoWorkEventArgs e)
        {
            logger.Info("Запущена задача по обновлению топиков...");
            BackgroundWorker worker = sender as BackgroundWorker;
            var categories = e.Argument as List<Category>;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            try
            {
                foreach (var category in categories)
                {
                    logger.Trace("\t Обрабатывается форум \"" + category.Name + "\"...");
                    try
                    {

                        var topics = RuTrackerOrg.Current.GetTopicsStatus(category.CategoryID).Select(x => x[0]).Distinct().ToArray();

                        //  Разбиваем данные на пакеты
                        var packets = new List<int>[topics.Length % 100 == 0 ? topics.Length / 100 : topics.Length / 100 + 1];
                        for (int i = 0; i < topics.Length; ++i)
                        {
                            var index = i / 100;
                            if (packets[index] == null) packets[index] = new List<int>();
                            packets[index].Add(topics[i]);
                        }

                        ClientLocalDB.Current.DeleteTopicsByCategoryId(category.CategoryID);
                        foreach (var t in packets)
                        {
                            ClientLocalDB.Current.SaveTopicInfo(RuTrackerOrg.Current.GetTopicsInfo(t.ToArray()), true);
                            percentComplete += 100.0M / (categories.Count * packets.Length);
                            worker.ReportProgress((int)percentComplete);
                        }
                    }
                    catch(Exception ex)
                    {
                        logger.Error("Ошибка при обнновлении топиков: " + ex.Message);
                        logger.Debug(ex);
                    }
                    ClientLocalDB.Current.SaveUsers(RuTrackerOrg.Current.GetUsers(ClientLocalDB.Current.GetNoUsers()));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.Debug(ex);
                System.Windows.Forms.MessageBox.Show(ex.Message, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
            logger.Info("Завершена задача по обновлению топиков.");
        }
        public static void bwUpdateKeepersByAllCategories(object sender, DoWorkEventArgs e)
        {
            logger.Info("Запущена задача по обновлению информации о хранителях...");
            BackgroundWorker worker = sender as BackgroundWorker;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            try
            {
                ClientLocalDB.Current.ClearKeepers();
                var categories = ClientLocalDB.Current.GetCategoriesEnable().Select(x=>x.CategoryID).OrderBy(x=>x).ToArray();
                var listFirstTopics = ClientLocalDB.Current.GetReports()
                    .Where(x => x.Key.Item2 == 0 && x.Key.Item1 != 0 && !string.IsNullOrWhiteSpace(x.Value.Item1) && categories.Any(z=>z == x.Key.Item1))
                    .Select(x =>
                        {
                            var p = x.Value.Item1.Split('=');
                            if (p.Length == 3)
                                return new { TopicID = int.Parse(p[2]), CategoryID = x.Key.Item1 };
                            else if (p.Length == 2)
                                return new { TopicID = int.Parse(p[1]), CategoryID = x.Key.Item1 };
                            else
                                return new { TopicID = 0, CategoryID = x.Key.Item1 };
                        })
                        .Where(x => x.TopicID != 0)
                        .OrderBy(x=>x.CategoryID)
                        .ToArray()
                    ;
                var forum = new RuTrackerOrg(Settings.Current.KeeperName, Settings.Current.KeeperPass);
                foreach (var t in listFirstTopics)
                {
                    logger.Trace("\t" + t.CategoryID);
                    ClientLocalDB.Current.SaveKeepOtherKeepers(forum.GetKeeps(t.TopicID, t.CategoryID));
                    percentComplete += 100.0M / listFirstTopics.Count();
                    worker.ReportProgress((int)percentComplete);
                }
                ClientLocalDB.Current.CreateReportByRootCategories();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.Debug(ex);
                System.Windows.Forms.MessageBox.Show(ex.Message, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
            logger.Info("Завершена задача по обновлению информации о хранителях.");
        }
        public static void bwRuningAndStopingDistributions(object sender, DoWorkEventArgs e)
        {
            logger.Info("Запущена задача по запуску/остановке раздач в торрент-клиентах...");
            BackgroundWorker worker = sender as BackgroundWorker;
            var category = e.Argument as Category;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            var countSeedersBycategories = new Dictionary<int, int>();
            try
            {
                var allTopics = ClientLocalDB.Current.GetTopicsByCategory(-1).Where(x => !x.IsBlackList);

                foreach (var c in ClientLocalDB.Current.GetCategoriesEnable())
                {
                    if (!countSeedersBycategories.ContainsKey(c.CategoryID)) countSeedersBycategories.Add(c.CategoryID, c.CountSeeders);
                }
                //  Сбрасываем флаги о загружености и хранении топиков, получим позже
                ClientLocalDB.Current.ResetFlagsTopicDownloads();

                var clients = ClientLocalDB.Current.GetTorrentClients();
                //  Обхо
                foreach (var tc in clients)
                {
                    try
                    {
                        ITorrentClient tClient = tc.Create();
                        if (tClient != null)
                        {
                            var fromTorrentClient = tClient.GetAllTorrentHash();
                            logger.Info("\t Кол-во раздач в торрент-клиенте \"" + tc.Name + "\": " + fromTorrentClient.Count);
                            ClientLocalDB.Current.SetTorrentClientHash(fromTorrentClient);
                            var status = (from c in fromTorrentClient
                                          join a in allTopics on c.Hash equals a.Hash
                                          where c.IsRun.HasValue
                                          select new
                                          {
                                              Hash = a.Hash,
                                              IsRun = c.IsRun.Value,
                                              IsPause = c.IsPause,
                                              Seeders = a.Seeders,
                                              MaxSeeders = countSeedersBycategories.ContainsKey(a.CategoryID) ? (int?)countSeedersBycategories[a.CategoryID] : null
                                          }).ToList();

                            var stopings = status.Where(x => x.IsRun && x.MaxSeeders.HasValue && x.Seeders > x.MaxSeeders.Value + 1).Select(x => x.Hash).ToArray();
                            logger.Info("\t Кол-во раздач в торрент-клиенте \"" + tc.Name + "\" которые требуется остановить: " + stopings.Length + ". Останавливаем...");
                            var packets = new List<string>[stopings.Length / 50 + (stopings.Length % 50 != 0 ? 1 : 0)];
                            for (int i = 0; i < stopings.Length; ++i)
                            {
                                var index = i / 50;
                                if (packets[index] == null) packets[index] = new List<string>();
                                packets[index].Add(stopings[i]);
                            }
                            if (packets.Length == 0) { percentComplete += 100.0M / (2 * clients.Count); worker.ReportProgress((int)percentComplete); }
                            foreach (var p in packets)
                            {
                                if (p != null) tClient.DistributionStop(p);
                                percentComplete += 100.0M / (2 * clients.Count * packets.Length);
                                worker.ReportProgress((int)percentComplete);
                            }

                            var startings = status.Where(x => (!x.IsRun || x.IsPause) && x.MaxSeeders.HasValue && x.Seeders <= x.MaxSeeders.Value).Select(x => x.Hash).ToArray();
                            packets = new List<string>[startings.Length / 50 + (startings.Length % 50 != 0 ? 1 : 0)];

                            logger.Info("\t Кол-во раздач в торрент-клиенте \"" + tc.Name + "\" которые требуется запустить: " + startings.Length + ". Запускаем...");
                            for (int i = 0; i < startings.Length; ++i)
                            {
                                var index = i / 50;
                                if (packets[index] == null) packets[index] = new List<string>();
                                packets[index].Add(startings[i]);
                            }
                            if (packets.Length == 0) { percentComplete += 100.0M / (2 * clients.Count); worker.ReportProgress((int)percentComplete); }
                            foreach (var p in packets)
                            {
                                if (p != null) tClient.DistributionStart(p);
                                percentComplete += 100.0M / (2 * clients.Count * packets.Length);
                                worker.ReportProgress((int)percentComplete);
                            }
                        }
                        else
                            percentComplete += 100.0M / (clients.Count);
                        worker.ReportProgress((int)percentComplete);


                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Не удалось запустить/остановить раздачи на клиенте \"" + tc.Name + "\": " + ex.Message);
                        logger.Debug(ex);
                        percentComplete += 100.0M / clients.Count;
                    }
                    worker.ReportProgress((int)percentComplete);
                }
                logger.Info("Строим отчеты о хранимом...");
                CreateReports();
                logger.Info("Отчеты о хранимом построены.");
            }
            catch (Exception ex)
            {
                logger.Warn("Произошла критическая ошибка при запуске/остановки раздач");
                logger.Debug(ex);
            }
            logger.Info("Завершена задача по запуску/остановке раздач в торрент-клиентах.");
            logger.Debug(string.Format("Размер ОЗУ 1: {0}", GC.GetTotalMemory(false)));
            GC.Collect(2);
            logger.Debug(string.Format("Размер ОЗУ 2: {0}", GC.GetTotalMemory(false)));
        }
        public static void bwCreateReportsTorrentClients(object sender, DoWorkEventArgs e)
        {
            var clients = ClientLocalDB.Current.GetTorrentClients();
            var allTopics = ClientLocalDB.Current.GetTopicsByCategory(-1).Where(x => !x.IsBlackList);
            logger.Info("Строим отчет о статистике в торрент-клиенте...");
            var strBuld = new StringBuilder();
            var categDict = ClientLocalDB.Current.GetCategories().ToDictionary(x => x.CategoryID, x => x);
            var starlen = Math.Max(categDict.Count == 0 ? 20 : categDict.Values.Max(x => x.FullName.Length), clients.Count == 0 ? 20 : clients.Max(x => x.Name.Length));
            var starStr = string.Empty;
            for (int i = 0; i < starlen; i++) starStr += "*";
            
            BackgroundWorker worker = sender as BackgroundWorker;
            var percentComplete = 0.0M;
            worker.ReportProgress((int)percentComplete);
            
            foreach (var tc in clients)
            {
                logger.Debug("\t" + tc.Name + "...");
                try
                {
                    strBuld.AppendLine(starStr);
                    strBuld.AppendFormat("*\t{0}\r\n", tc.Name);
                    strBuld.AppendLine(starStr);
                    ITorrentClient tClient = tc.Create();
                    if (tClient != null)
                    {
                        var fromTorrentClient = tClient.GetAllTorrentHash();
                        var ttt = (from t in fromTorrentClient
                                   join b in allTopics on t.Hash equals b.Hash into bt
                                   from b in bt.DefaultIfEmpty()
                                   select new
                                   {
                                       CategoryID = b != null ? b.CategoryID : -1,
                                       Size = t.Size,
                                       IsRun = (t.IsRun.HasValue ? (t.IsRun.Value ? 1 : 0) : -1),
                                       IsPause = t.IsPause,
                                       Seeders = b == null ? -1 : b.Seeders,
                                   }).GroupBy(x => new { x.CategoryID, x.IsRun, x.IsPause, x.Seeders }).Select(x => new
                                   {
                                       CategoryID = x.Key.CategoryID,
                                       IsRun = x.Key.IsRun,
                                       IsPause = x.Key.IsPause,
                                       Size = x.Sum(y => y.Size),
                                       Count = x.Count(),
                                       Seeders = x.Key.Seeders
                                   }).ToArray();
                        strBuld.AppendFormat("\tВсего:\t\t{0,6} шт. ({1})\r\n", ttt.Sum(x => x.Count), TopicInfo.sizeToString(ttt.Sum(x => x.Size)));
                        strBuld.AppendFormat("\tРаздаются:\t{0,6} шт. ({1})\r\n", ttt.Where(x => x.IsRun == 1).Sum(x => x.Count), TopicInfo.sizeToString(ttt.Where(x => x.IsRun == 1).Sum(x => x.Size)));
                        strBuld.AppendFormat("\tОстановлены:\t{0,6} шт. ({1})\r\n", ttt.Where(x => x.IsRun == 0).Sum(x => x.Count), TopicInfo.sizeToString(ttt.Where(x => x.IsRun == 0).Sum(x => x.Size)));
                        strBuld.AppendFormat("\tПрочие:\t\t{0,6} шт. ({1})\r\n", ttt.Where(x => x.IsRun == -1).Sum(x => x.Count), TopicInfo.sizeToString(ttt.Where(x => x.IsRun == -1).Sum(x => x.Size)));
                        strBuld.AppendFormat("\tНеизвестные:\t{0,6} шт. ({1})\r\n", ttt.Where(x => x.CategoryID == -1).Sum(x => x.Count), TopicInfo.sizeToString(ttt.Where(x => x.CategoryID == -1).Sum(x => x.Size)));
                        strBuld.AppendLine();

                        strBuld.AppendFormat("\tПо кол-ву сидов:\r\n");
                        foreach (var d in ttt.GroupBy(x => x.Seeders).Select(x => new { Seeders = x.Key, Count = x.Sum(z => z.Count), Size = x.Sum(z => z.Size) }).OrderBy(x=>x.Seeders))
                            strBuld.AppendFormat("\t{2}:\t\t{0,5} шт. ({1})\r\n", d.Count, TopicInfo.sizeToString(d.Size), d.Seeders);
                        strBuld.AppendLine();
                        foreach (var c in ttt.Select(x => x.CategoryID).Distinct().OrderBy(x => x).ToArray())
                        {
                            var filt = ttt.Where(x => x.CategoryID == c).ToArray();
                            var name = "Неизвестные";
                            if (categDict.ContainsKey(c)) name = categDict[c].FullName;
                            strBuld.AppendFormat("{0}:\r\n", name);
                            strBuld.AppendFormat("\tВсего:\t\t{0,5} шт. ({1})\r\n",
                                filt.Sum(x => x.Count),
                                TopicInfo.sizeToString(filt.Sum(x => x.Size)));
                            strBuld.AppendFormat("\tРаздаются:\t{0,5} шт. ({1})\r\n",
                                filt.Where(x => x.IsRun == 1).Sum(x => x.Count),
                                TopicInfo.sizeToString(filt.Where(x => x.IsRun == 1).Sum(x => x.Size)));
                            strBuld.AppendFormat("\tОстановлены:\t{0,5} шт. ({1})\r\n",
                                filt.Where(x => x.IsRun == 0).Sum(x => x.Count),
                                TopicInfo.sizeToString(filt.Where(x => x.IsRun == 0).Sum(x => x.Size)));
                            strBuld.AppendFormat("\tПрочие:\t\t{0,5} шт. ({1})\r\n",
                                filt.Where(x => x.IsRun == -1).Sum(x => x.Count),
                                TopicInfo.sizeToString(filt.Where(x => x.IsRun == -1).Sum(x => x.Size)));
                        }
                        strBuld.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    strBuld.AppendFormat("Ошибка: {0}\r\n\r\n\r\n", ex.Message);
                }
                strBuld.AppendLine();
                percentComplete += 100.0M / clients.Count();
                if (percentComplete <= 100) worker.ReportProgress((int)percentComplete);
            }
            var reports = new Dictionary<int, Dictionary<int, string>>();
            reports.Add(0, new Dictionary<int, string>());
            reports[0].Add(1, strBuld.ToString());
            try
            {
                ClientLocalDB.Current.SaveReports(reports);
                logger.Info("Отчет о статистике в торрент-клиенте построен.");
            }
            catch(Exception ex)
            {
                logger.Error("Произошла ошибка при сохранении отчета в базу данных: " + ex.Message);
                logger.Trace(ex.StackTrace);
            }
        }
        public static void bwSendReports(object sender, DoWorkEventArgs e)
        {
            logger.Info("Запущена задача на отправку отчетов на форум....");
            var percentComplete = 0.0M;
            BackgroundWorker worker = sender as BackgroundWorker;
            var reports = ClientLocalDB.Current.GetReports()
                .Where(x => !string.IsNullOrWhiteSpace(x.Value.Item1))
                .OrderBy(x => x.Key.Item1)
                .Select(x => x.Value)
                .Where(x=>x.Item1.Split('=').Length == 3)
                .ToArray();
            if (reports.Where(x => !string.IsNullOrWhiteSpace(x.Item1)).Count() == 0)
            {
                System.Windows.Forms.MessageBox.Show("Нет ни одного отчета c указанным URL для отправки на форум");
                return;
            }
            foreach (var r in reports.Where(x => !string.IsNullOrWhiteSpace(x.Item1)))
            {
                logger.Info(r.Item1);
                try
                {
                    Current.SendReport(r.Item1, r.Item2);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    logger.Debug(ex);
                    System.Windows.Forms.MessageBox.Show("Поизошла ошибка при отправке отчетов:\r\n" + ex.Message, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
                }
                percentComplete += 100.0M / reports.Length;
                worker.ReportProgress((int)percentComplete);
            }
            logger.Info("Завершена задача на отправку отчетов на форум.");
        }

        
        public static void CreateReports()
        {
            //  Очищаем отчеты
            ClientLocalDB.Current.ClearReports();
            var categories = ClientLocalDB.Current.GetCategoriesEnable();
            var currReports = ClientLocalDB.Current.GetReports();
            var reports = new Dictionary<int, Dictionary<int, string>>();
            var allStatistics = ClientLocalDB.Current.GetStatisticsByAllUsers().Where(x => !string.IsNullOrWhiteSpace(x.Item2)).ToArray();
            var sb = new StringBuilder();
            
            #region Формируем сводный отчет
            var statistis = allStatistics.Where(x => x.Item2 == Settings.Current.KeeperName).ToArray();
            var catId = categories.Select(x=>x.CategoryID).ToArray();
            sb.Clear();
            sb.AppendFormat("Актуально на: {0}\r\n\r\n", DateTime.Now.ToString("dd.MM.yyyy"));
            sb.AppendFormat("Общее количество хранимых раздач: {0} шт.\r\n", statistis.Where(x => catId.Contains(x.Item1)).Sum(x => x.Item3));
            sb.AppendFormat("Общий вес хранимых раздач: {0} Gb\r\n", statistis.Where(x => catId.Contains(x.Item1)).Sum(x => x.Item4).ToString("0.00"));
            sb.AppendLine("[hr]");
            foreach (var category in categories.OrderBy(x=>x.FullName))
            {
                var st = statistis.Where(x => x.Item1 == category.CategoryID).FirstOrDefault();
                if (st == null)
                    st = new Tuple<int, string, int, decimal>(category.CategoryID, "<->", 0, 0);
                var url = string.Empty;
                if (currReports.ContainsKey(new Tuple<int, int>(st.Item1, 1)))
                {
                    url = currReports[new Tuple<int, int>(st.Item1, 1)].Item1;
                    if (!string.IsNullOrWhiteSpace(url) && url.Split('=').Length > 2)
                        url = url.Split('=')[2];
                    else
                        url = string.Empty;

                    sb.AppendFormat("{0}{1}{2} - {3} шт. ({4:0.00} GB)\r\n",
                        string.IsNullOrWhiteSpace(url) ? "" : string.Format("[url=http://rutracker.org/forum/viewtopic.php?p={0}#{0}]", url),
                        category.FullName,
                        string.IsNullOrWhiteSpace(url) ? "" : "[/url]",
                        st.Item3,
                        st.Item4
                        );
                }
            }
            reports.Add(0, new Dictionary<int, string>());
            reports[0].Add(0, sb.ToString());
            ClientLocalDB.Current.SaveReports(reports);
            reports.Clear();
            #endregion

            #region Формируем первые посты
            foreach (var category in categories)
            {
                sb.Clear();
                var st = allStatistics.Where(x => x.Item1 == category.CategoryID && x.Item3 > 0 && x.Item2 != "All");
                var all = allStatistics.Where(x => x.Item1 == category.CategoryID && x.Item2 == "All").FirstOrDefault();
                if (st == null || st.Count() == 0 || all == null) continue;
                sb.AppendFormat("[url=http://rutracker.org/forum/viewforum.php?f={0}][color=darkgreen][b]{1}[/b][/color][/url] | [url=http://rutracker.org/forum/tracker.php?f={0}&tm=-1&o=10&s=1][color=darkgreen][b]Проверка сидов[/b][/color][/url]\r\n\r\n", category.CategoryID, category.Name);
                sb.AppendFormat("[b]Актуально на:[/b] {0}\r\n\r\n", DateTime.Now.ToString("dd.MM.yyyy"));
                sb.AppendFormat("[b]Общее количество раздач в подразделе:[/b] {0} шт.\r\n", all.Item3);
                sb.AppendFormat("[b]Общий размер раздач в подразделе:[/b] {0:0.00} GB.\r\n", all.Item4);
                sb.AppendFormat("[b]Количество хранителей:[/b] {0}\r\n", st.Count());
                sb.AppendFormat("[b]Общее количество хранимых раздач:[/b] {0} шт.\r\n", st.Sum(x=>x.Item3));
                sb.AppendFormat("[b]Общий вес хранимых раздач:[/b] {0:0.00} GB.\r\n", st.Sum(x => x.Item4));
                sb.AppendLine("[hr]");
                var counter = 0;
                foreach(var keepStatistic in st.OrderBy(x=>x.Item2))
                {
                    ++counter;
                    sb.AppendFormat("[b]Хранитель {0}:[/b] [url=http://rutracker.org/forum/profile.php?mode=viewprofile&u={4}][color=darkgreen][b]{1}[/b][/color][/url] - {2} шт. ({3:0.00} GB)\r\n", 
                        counter,
                        keepStatistic.Item2.Replace("<wbr>", ""),
                        keepStatistic.Item3,
                        keepStatistic.Item4,
                        System.Web.HttpUtility.UrlEncode(keepStatistic.Item2.Replace("<wbr>", "")));
                }
                reports.Add(category.CategoryID, new Dictionary<int, string>());
                reports[category.CategoryID].Add(0, sb.ToString());
            }
            ClientLocalDB.Current.SaveReports(reports);
            reports.Clear();
            #endregion

            #region  Формируем отчеты о хранимом
            {
                var top1 = Settings.Current.ReportTop1.Replace("%%CreateDate%%", "{0}")
                    .Replace("%%CountTopics%%", "{1}")
                    .Replace("%%SizeTopics%%", "{2}")
                    + "\r\n";
                var top2 = Settings.Current.ReportTop2.Replace("%%CreateDate%%", "{0}")
                    .Replace("%%CountTopics%%", "{1}")
                    .Replace("%%SizeTopics%%", "{2}")
                    .Replace("%%NumberTopicsFirst%%", "{3}")
                    .Replace("%%NumberTopicsLast%%", "{4}")
                    .Replace("%%ReportLines%%", "{5}")
                    .Replace("%%Top1%%", "{6}") + "\r\n";

                var lineFormat = Settings.Current.ReportLine.Replace("%%ID%%", "{0}")
                    .Replace("%%Name%%", "{1}")
                    .Replace("%%Size%%", "{2}")
                    .Replace("%%Status%%", "{3}")
                    .Replace("%%CountSeeders%%", "{4}")
                    .Replace("%%Date%%", "{5}");
                int len = 115000;

                var sb1 = new StringBuilder();
                var sb2 = new StringBuilder();

                foreach (var category in categories)
                {
                    int counter1 = 0;           // Кол-во топиков
                    int counter2 = 0;
                    //int counter3 = 0;           //  Кол-во в пакете (для сборки пакета кратным 10
                    int counter4 = 1;           //  Хранит предыдущее кол-во топиков  
                    int pages = 0;
                    sb1.Clear();
                    sb2.Clear();

                    var topics = ClientLocalDB.Current.GetTopicsByCategory(category.CategoryID).Where(x => x.IsKeep && (x.Seeders <= Settings.Current.CountSeedersReport || Settings.Current.CountSeedersReport == -1) && !x.IsBlackList).OrderBy(x => x.Name2).ToArray();
                    //  Если данных в отчете нет, то нет и отчета
                    if (topics.Length == 0) continue;

                    reports.Add(category.CategoryID, new Dictionary<int, string>());
                    var repCat = reports[category.CategoryID];

                    var header = string.Format(top1, DateTime.Now.ToString("dd.MM.yyyy"), topics.Length, TopicInfo.sizeToString(topics.Sum(x => x.Size)));
                        //string.Format("[b]Актуально на:[/b] {0}\r\n\r\nОбщее количество хранимых раздач подраздела: {1} шт. ({2})\r\n", DateTime.Now.ToString("dd.MM.yyyy"), topics.Length, TopicInfo.sizeToString(topics.Sum(x => x.Size)));


                    foreach (var t in topics)
                    {
                        sb2.AppendLine(string.Format(lineFormat, t.TopicID, t.Name2, t.SizeToString,
                            t.StatusToString,
                            t.Seeders,
                            t.RegTimeToString
                            ));

                        ++counter1;
                        ++counter2;

                        if (counter1 % 10 == 0 || topics.Length <= counter1)
                        {
                            if (topics.Length == counter1)
                            {
                                if (counter2 == 0)
                                    sb1.AppendFormat("[*={0}{1}", counter4, sb2.ToString().Substring(2));
                                else
                                    sb1.AppendLine(sb2.ToString());
                            }

                            if (len <= sb1.Length + sb2.Length + header.Length || topics.Length <= counter1)
                            {
                                ++pages;

                                var current = counter1 < topics.Length ? counter1 - 10 : counter1;
                                //repCat.Add(pages, string.Format("{0}[spoiler=\"Раздачи, взятые на хранение, №№ {1} - {2}\"]\r\n[list=1]\r\n{3}\r\n[/list]\r\n[/spoiler]", header, counter4, current, sb1.ToString()));
                                repCat.Add(pages, string.Format(top2, DateTime.Now.ToString("dd.MM.yyyy"), topics.Length, TopicInfo.sizeToString(topics.Sum(x => x.Size)), counter4, current, sb1.ToString(), header) + Settings.Current.ReportBottom);

                                sb1.Clear();
                                counter2 = 0;
                                counter4 = (current + 1);
                                header = string.Empty;
                            }
                            if (counter2 == 0)
                                sb1.AppendFormat("[*={0}{1}\r\n", counter4, sb2.ToString().Substring(2));
                            else
                                sb1.AppendLine(sb2.ToString());
                            sb2.Clear();
                        }
                    }
                }

                ClientLocalDB.Current.SaveReports(reports);
            }
            #endregion
            
        }

    }
}
