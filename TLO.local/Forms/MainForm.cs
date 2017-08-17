using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TLO.local.Forms;

namespace TLO.local
{
    public partial class MainForm : Form
    {
        NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        //System.Timers.Timer tmr = null;
        string headText;
        Timer tmr = null;
        DateTime _LastRunTimer = DateTime.Now;
        private BindingSource _CategorySource = new BindingSource();
        private BindingSource _TopicsSource = new BindingSource();
        private bool IsClose { get; set; }
        private NotifyIcon notifyIcon;
        public MainForm()
        {
            InitializeComponent();
            _DateRegistration.Value = DateTime.Now.AddDays(-30);
            this.Text = headText = string.Format("TLO {0}", System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion);
            _cbCountSeeders.Value = 5;
            _cbCategoryFilters.SelectedItem = "Не скачан торрент";

            _CategorySource.Clear();
            _CategorySource.DataSource = ClientLocalDB.Current.GetCategoriesEnable();
            _CategorySource.CurrentChanged += new EventHandler(SelectionChanged);
            _cbCategory.DataSource = _CategorySource;
            if (_CategorySource.Count > 0) _CategorySource.Position = 1;

            _TopicsSource.CurrentChanged += new EventHandler(SelectionChanged);

            //_dgvReportDownloads.SelectionMode = DataGridViewSelectionMode.ColumnHeaderSelect | DataGridViewSelectionMode.RowHeaderSelect;
            _dgvReportDownloads.AutoGenerateColumns = false;
            _dgvReportDownloads.ClearSelection();
            _dgvReportDownloads.DataSource = _TopicsSource;

            this.Disposed += MainForm_Disposed;
            this.Resize += MainForm_Resize;
            //tmr = new System.Timers.Timer();
            tmr = new Timer();
            tmr.Tick += tmr_Tick;
            tmr.Interval = 1000;// Settings.Current.PeriodRunAndStopTorrents * 60 * 1000;
            tmr.Start();
            IsClose = false;

            notifyIcon = new NotifyIcon();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
            notifyIcon.Visible = true;
            WriteReports();
        }

        private void MenuClick(object sender, EventArgs e)
        {
            try
            {
                if (sender == menuSettingsToolStripMenuItem)
                {
                    var dlg = new SettingsForm();
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        _CategorySource.Clear();
                        _CategorySource.DataSource = null;
                        _CategorySource.DataSource = ClientLocalDB.Current.GetCategoriesEnable();
                        _CategorySource.Position = 0;

                        if (MessageBox.Show("Запустить загрузку/обновление информации о топиках (раздачах) по всем категориям?", "Обновление данных", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.OK)
                            dwCreateAndRun(Logic.bwUpdateTopicsByCategories, "Полное обновление информации о топиках (раздачах) по всем категориям...", ClientLocalDB.Current.GetCategoriesEnable());
                    }
                }
                else if (sender == UpdateCountSeedersToolStripMenuItem)
                {
                    dwCreateAndRun(Logic.bwUpdateCountSeedersByAllCategories, "Обновление кол-ва сидов на раздачах...", sender);
                }
                else if (sender == UpdateListTopicsToolStripMenuItem)
                {
                    dwCreateAndRun(Logic.bwUpdateTopicsByCategories, "Полное обновление информации о топиках (раздачах) по всем категориям...", ClientLocalDB.Current.GetCategoriesEnable());
                }
                //  Обновление хешей из торрент-клиентов
                else if (sender == UpdateKeepTopicsToolStripMenuItem)
                    dwCreateAndRun(Logic.bwUpdateHashFromAllTorrentClients, "Полное обновление информации из Torrent-клиентов...");
                //  Очистка БД
                else if (sender == ClearDatabaseToolStripMenuItem)
                {
                    if (MessageBox.Show("Вы пытаетесь очистить базу данны от текущих данных (статистику и информацию о топиках).\r\n Продолжить?", "Запрос подтверждения", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes) return;
                    ClientLocalDB.Current.ClearDatabase();
                    //  Отправка сформированных отчетов на форум
                }
                else if (sender == ClearKeeperListsToolStripMenuItem)
                {
                    if (MessageBox.Show("Вы пытаетесь очистить базу данны от данных других хранителей.\r\n Продолжить?", "Запрос подтверждения", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes) return;
                    ClientLocalDB.Current.ClearKeepers();
                    SelectionChanged(_CategorySource, null);
                }
                else if (sender == SendReportsToForumToolStripMenuItem)
                {
                    if (MessageBox.Show("Отправка отчетов на форум может продолжаться продолжительное время.\r\n Продолжить?", "Запрос подтверждения", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes) return;
                    dwCreateAndRun(Logic.bwSendReports, "Отправка отчетов на форум...", this);
                }
                //  Обновление отчетов
                else if (sender == CreateReportsToolStripMenuItem)
                {
                    if (MessageBox.Show("Сборка отчетов может продолжаться продолжительное время и потребуется обновить список раздач и информацию из торрент-клиентов.\r\n Продолжит?", "Запрос подтверждения", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes) return;
                    dwCreateAndRun(Logic.bwUpdateCountSeedersByAllCategories, "Обновление кол-ва сидов на раздачах...", sender);
                    dwCreateAndRun(Logic.bwUpdateHashFromAllTorrentClients, "Обновление информации из Torrent-клиентов...", sender);
                }
                //  Запуск/Остановка раздач
                else if (sender == RuningStopingDistributionToolStripMenuItem)
                {
                    dwCreateAndRun(Logic.bwUpdateCountSeedersByAllCategories, "Обновление кол-ва сидов на раздачах...", sender);
                    dwCreateAndRun(Logic.bwRuningAndStopingDistributions, "Обновление информации из Torrent-клиентов...", sender);
                    dwCreateAndRun(Logic.bwCreateReportsTorrentClients, "Построение сводного отчета по торрент-клиентам...", sender);
                }
                else if (sender == CreateConsolidatedReportByTorrentClientsToolStripMenuItem)
                    dwCreateAndRun(Logic.bwCreateReportsTorrentClients, "Построение сводного отчета по торрент-клиентам...", sender);
                else if (sender == LoadListKeepersToolStripMenuItem)
                    dwCreateAndRun(Logic.bwUpdateKeepersByAllCategories, "Обновлении данных о хранителях...", sender);
                else if (sender == ExitToolStripMenuItem)
                {
                    IsClose = true;
                    Close();

                }
                else if (sender == DevlToolStripMenuItem)
                {
                    dwCreateAndRun(Logic.bwUpdateKeepersByAllCategories, "Обновлении данных о хранителях...", sender);

                    try
                    {
                        var rt = new RuTrackerOrg(Settings.Current.KeeperName, Settings.Current.KeeperPass);
                        //var categories = ClientLocalDB.Current.GetCategories().Where(x => x.ReportTopicID > 0).ToArray();
                        //foreach (var c in categories)
                        //{
                        //    var data = rt.GetKeeps(c.ReportTopicID);
                        //    ClientLocalDB.Current.SaveKeepOtherKeepers(data);
                        //    Console.WriteLine(data.Count());
                        //}
                        //dwCreateAndRun(Logic.bwUpdateTopicsByCategories, "Полное обновление информации о топиках (раздачах) по всем категориям...", categories.ToList());

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
                    }
                }
                else if (sender == _btSaveToFile)
                    SaveSetingsToFile();
                else if (sender == _btLoadSettingsFromFile)
                    ReadSettingsFromFile();
            }
            catch(Exception ex)
            {
                Cursor.Current = Cursors.Default;
                MessageBox.Show(ex.Message);
            }
            Cursor.Current = Cursors.Default;
        }

        void tmr_Tick(object sender, EventArgs e)
        {
            if (backgroundWorkers.Count > 0)
            {
                this.Text = string.Format("{0} ({1})", headText, "Выполняются задачи...");
                notifyIcon.Text = string.Format("{0} ({1})", headText, "Выполняются задачи...");
                return;
            }
            var curr = _LastRunTimer - DateTime.Now.AddMinutes(-Settings.Current.PeriodRunAndStopTorrents);
            if (curr.TotalSeconds > 0)
            {
                this.Text = string.Format("{0} ({1:hh\\:mm\\:ss})", headText, curr);
                notifyIcon.Text = string.Format("{0} ({1:hh\\:mm\\:ss})", headText, curr);
                return;
            }
            try
            {
                if (Settings.Current.LastUpdateTopics < DateTime.Now.AddDays(-1))
                {
                    dwCreateAndRun(Logic.bwUpdateTopicsByCategories, "Полное обновление информации о топиках (раздачах) по всем категориям...", ClientLocalDB.Current.GetCategoriesEnable());
                    dwCreateAndRun(Logic.bwUpdateKeepersByAllCategories, "Обновлении данных о хранителях...", sender);
                    Settings.Current.LastUpdateTopics = DateTime.Now.Date;
                    Settings.Current.Save();
                }
                else
                    dwCreateAndRun(Logic.bwUpdateCountSeedersByAllCategories, "Обновление информации о кол-ве сидов на раздачах...", sender);
                dwCreateAndRun(Logic.bwRuningAndStopingDistributions, "Запуск/Остановка раздач в Torrent-клиентах...", sender);
                dwCreateAndRun(Logic.bwCreateReportsTorrentClients, "Построение сводного отчета по торрент-клиентам...", sender);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Debug(ex.Message, ex);
            }
            _LastRunTimer = DateTime.Now;
        }

        void MainForm_Disposed(object sender, EventArgs e)
        {
            tmr.Stop();
            tmr.Dispose();
        }

        void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void SelectionChanged(object sender, EventArgs e)
        {
            #region Фильтр
            if (sender == _CategorySource || sender == _cbCountSeeders || sender == _cbBlackList || sender == _cbCategoryFilters || sender == _DateRegistration)
            {
                _TopicsSource.Clear();

                if (_CategorySource.Current != null)
                {
                    var category = _CategorySource.Current as Category;
                    int countSeeders = (int)_cbCountSeeders.Value;
                    DateTime date = _DateRegistration.Value;
                    bool? isKeep = null;
                    bool? isKeepers = null;
                    bool? isDownload = null;
                    bool? isBlack = null;
                    bool? isPoster = null;
                    var filter = _cbCategoryFilters.SelectedItem as string;
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        switch (filter)
                        {
                            case "Не скачан торрент":
                                isDownload = false;
                                break;
                            case "Не скачан торрент и нет хранителя":
                                isDownload = false;
                                isKeepers = false;
                                break;
                            case "Не скачан торрент и есть хранитель":
                                isDownload = false;
                                isKeepers = true;
                                break;
                            case "Храню":
                                isKeep = true;
                                break;
                            case "Храню и есть хранитель":
                                isKeep = true;
                                isKeepers = true;
                                break;
                            case "Скачиваю раздачу":
                                isDownload = true;
                                isKeep = false;
                                break;
                            case "Есть хранитель":
                                isKeepers = true;
                                break;
                            case "Не скачано":
                                isDownload = false;
                                break;
                            case "Не храню":
                                isKeep = false;
                                break;
                            case "Я релизер":
                                isPoster = false;
                                break;
                        }
                    }
                    isBlack = _cbBlackList.Checked;
                    var data = new List<TopicInfo>();
                    if (Settings.Current.IsAvgCountSeeders)
                    {
                        data = ClientLocalDB.Current.GetTopics(date,
                                                    category.CategoryID,
                                                    null,
                                                    countSeeders > -1 ? (int?)countSeeders : null,
                                                    isKeep,
                                                    isKeepers,
                                                    isDownload,
                                                    isBlack,
                                                    isPoster
                                                    );
                    }
                    else
                    {
                        data = ClientLocalDB.Current.GetTopics(date,
                                                    category.CategoryID,
                                                    countSeeders > -1 ? (int?)countSeeders : null,
                                                    null,
                                                    isKeep,
                                                    isKeepers,
                                                    isDownload,
                                                    isBlack,
                                                    isPoster
                                                    );
                    }
                    _lbTotal.Text = string.Format("Кол-во: {0}; Размер: {1}", data.Count(), TopicInfo.sizeToString(data.Sum(x => x.Size)));
                    _TopicsSource.DataSource = data;
                }
            }
            #endregion
            #region Отчеты
            if (sender == _CategorySource && _CategorySource.Current != null)
            {
                tabReports.Controls.Clear();
                var reports = ClientLocalDB.Current.GetReports((_CategorySource.Current as Category).CategoryID);
                if (reports.Count() > 0)
                {
                    var rootSize = tabReports.Size;
                    var tc = new System.Windows.Forms.TabControl();
                    tc.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
                    tc.Location = new System.Drawing.Point(0, 0);
                    tc.SelectedIndex = 0;
                    tc.Size = new System.Drawing.Size(rootSize.Width, rootSize.Height);
                    tabReports.Controls.Add(tc);
                    foreach (var rp in reports.OrderBy(x=>x.Key.Item2))
                    {
                        if (rp.Value.Item2 == "Резерв" || rp.Value.Item2 == "Удалено") continue;
                        var tp = new System.Windows.Forms.TabPage();
                        var tb = new System.Windows.Forms.TextBox();

                        tp.Location = new System.Drawing.Point(4, 22);
                        tp.Padding = new System.Windows.Forms.Padding(3);
                        tp.Text = string.Format("Сидируемое: отчет № {0}", rp.Key.Item2);
                        if (rp.Key.Item2 == 0)
                        {
                            tp.Text = string.Format("Шапка сидируемого");
                        }
                        tp.UseVisualStyleBackColor = true;
                        tp.AutoScroll = true;


                        tb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
                        tb.Location = new System.Drawing.Point(0, 0);
                        tb.Multiline = true;
                        tb.ReadOnly = true;
                        tb.ScrollBars = System.Windows.Forms.ScrollBars.Both;
                        tb.Size = new System.Drawing.Size(rootSize.Width - 8, rootSize.Height - 20);
                        tb.Text = rp.Value.Item2;


                        tc.Controls.Add(tp);
                        tp.Controls.Add(tb);
                    }
                }
                else
                {
                    var rootSize = tabReports.Size;
                    var tc = new System.Windows.Forms.TabControl();
                    tc.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
                    tc.Location = new System.Drawing.Point(0, 0);
                    tc.SelectedIndex = 0;
                    tc.Size = new System.Drawing.Size(rootSize.Width, rootSize.Height);
                    tabReports.Controls.Add(tc);

                    var tp = new System.Windows.Forms.TabPage();
                    var tb = new System.Windows.Forms.TextBox();

                    tc.Controls.Add(tp);
                    tp.Controls.Add(tb);
                    tp.Location = new System.Drawing.Point(4, 22);
                    tp.Padding = new System.Windows.Forms.Padding(3);
                    tp.Text = string.Format("Для информации");
                    tp.UseVisualStyleBackColor = true;


                    tb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                    | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
                    tb.Location = new System.Drawing.Point(0, 0);
                    tb.Multiline = true;
                    tb.ReadOnly = true;
                    tb.ScrollBars = System.Windows.Forms.ScrollBars.Both;
                    tb.Size = new System.Drawing.Size(rootSize.Width - 8, rootSize.Height - 20);
                    tb.Text = @"Здесь должен быть отчет о сидируемом, но его нет.
Возможные причины: сервис не успел обработать задачи сервера на скачивание страниц, прочитать torrent-файлы или не смог подключиться к torrent-клиенту

Если на вкладке ""Не скачано"" есть раздачи которые Вы храните, то попробуйте сформировать отчет принудительно из пункта меню";
                }
            }
            #endregion
        }
        private void LinkClick(object sender, EventArgs e)
        {
            if (backgroundWorkers.Count != 0 && MessageBox.Show("Выполняются другие задачи. Добавить в очередь новое?", "Внимание", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes)
            {
                //MessageBox.Show("Дождитесь выполнения предыдущей задачи.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            //CreateNewBackgroundWorker();
            try
            {
                var category = _CategorySource.Current as Category;
                if (category == null) return;
                else if (sender == _llUpdateCountSeedersByCategory)
                {
                    Logic.UpdateSeedersByCategory(category);
                }
                else if (sender == _llUpdateTopicsByCategory)
                    dwCreateAndRun(Logic.bwUpdateTopicsByCategory, "Обновление списоков по разделу...", category);
                else if (sender == _llUpdateDataDromTorrentClient)
                    Logic.LoadHashFromClients(category.TorrentClientUID);

                #region Работа с выделеными раздачами
                else if (sender == _llDownloadSelectTopics)
                {
                    var data = (_TopicsSource.DataSource as List<TopicInfo>).Where(x => x.Checked).ToList();
                    dwCreateAndRun(Logic.bwDownloadTorrentFiles, "Скачиваются выделеные торрент-файлы в каталог...", new Tuple<List<TopicInfo>, MainForm>(data, this));
                }
                else if (sender == _llSelectedTopicsToTorrentClient)
                {
                    var data = (_TopicsSource.DataSource as List<TopicInfo>).Where(x => x.Checked).ToList();
                    dwCreateAndRun(Logic.bwSendTorrentFileToTorrentClient, "Скачиваются и добавляются в торрент-клиент выделеные раздачи...", new Tuple<MainForm, List<TopicInfo>, Category>(this, data, category));
                    dwCreateAndRun(Logic.bwUpdateHashFromTorrentClientsByCategoryUID, "Обновляем список раздач из торрент-клиента...", category);
                    //Logic.SendTorrentFileToTorrentClient(data, category);
                    //Logic.LoadHashFromClients(category.TorrentClientUID);
                }
                #endregion
                #region Работа с черным списком раздач
                else if (sender == _llSelectedTopicsToBlackList)
                {

                    var data = (_TopicsSource.DataSource as List<TopicInfo>).Where(x => x.Checked).ToList();
                    data.ForEach(x => x.IsBlackList = true);
                    ClientLocalDB.Current.SaveTopicInfo(data);
                }
                else if (sender == _llSelectedTopicsDeleteFromBlackList)
                {
                    var data = (_TopicsSource.DataSource as List<TopicInfo>).Where(x => x.Checked).ToList();
                    data.ForEach(x => x.IsBlackList = false);
                    ClientLocalDB.Current.SaveTopicInfo(data);
                }
                #endregion

                else if (sender == linkSetNewLabel)
                {
                    if (category == null) return;
                    var dlg = new GetLableName();
                    dlg.Value = string.IsNullOrWhiteSpace(category.Label) ? category.FullName : category.Label;
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        var label = dlg.Value;
                        var data = (_TopicsSource.DataSource as List<TopicInfo>).Where(x => x.Checked).ToList();
                        dwCreateAndRun(Logic.bwSetLabels, "Установка пользовательских меток...", new Tuple<MainForm, List<TopicInfo>, string>(this, data, label));
                    }
                }
                SelectionChanged(_CategorySource, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                MessageBox.Show("Произошла ошибка:\r\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
            Cursor.Current = Cursors.Default;
            //MessageBox.Show("Выполнено", "", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }


        private void ContentClick(object sender, DataGridViewCellEventArgs e)
        {

            if (_dgvReportDownloads.Columns[e.ColumnIndex].DataPropertyName == "Name")
            {
                try
                {
                    var topic = _dgvReportDownloads.Rows[e.RowIndex].Cells[0].Value as int?;
                    if (topic.HasValue)
                    {
                        System.Diagnostics.Process.Start(string.Format("http://rutracker.org/forum/viewtopic.php?t={0}", topic.Value));
                    }
                }
                catch { }
            }
            else if (_dgvReportDownloads.Columns[e.ColumnIndex].DataPropertyName == "Alternative")
            {
                try
                {
                    var topicId = _dgvReportDownloads.Rows[e.RowIndex].Cells[0].Value as int?;
                    if (topicId.HasValue)
                    {
                        var data = _TopicsSource.DataSource as List<TopicInfo>;
                        if (data == null)
                            return;
                        var topic = data.Where(x => x.TopicID == topicId.Value).FirstOrDefault();
                        if (topic == null)
                            return;

                        var name = string.Empty;
                        var year = string.Empty;
                        int t1 = topic.Name.IndexOf('/');

                        if (topic.Name.IndexOf(']') > t1 && t1 != -1)
                            name = topic.Name.Split('/').FirstOrDefault();
                        else if (topic.Name.IndexOf(']') < t1 && t1 != -1)
                            name = topic.Name.Split('/').FirstOrDefault().Split(']')[1];
                        else if (t1 == -1 && topic.Name.IndexOf('[') < 5 && topic.Name.IndexOf('[') != -1)
                            name = topic.Name.Split(']')[1].Split('[').FirstOrDefault();
                        else if (t1 == -1 && topic.Name.IndexOf('[') != -1)
                            name = topic.Name.Split('[').FirstOrDefault();
                        else
                            name = topic.Name.Split('[').FirstOrDefault();

                        var t2 = topic.Name.IndexOf('[', (t1 > -1 ? t1 : 0));

                        if (t2 < 5)
                        {
                            t2 = topic.Name.IndexOf(']') + 1;
                            t2 = topic.Name.IndexOf('[', t2);
                        }

                        year = topic.Name.Substring(t2 == -1 ? 0 : t2 + 1);
                        if (!string.IsNullOrWhiteSpace(year))
                            year = year.Split(new char[] { ',', ' ', ']' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                        if (!string.IsNullOrWhiteSpace(year))
                            name = name + " " + year;

                        System.Diagnostics.Process.Start(string.Format("http://rutracker.org/forum/tracker.php?f={0}&nm={1}", topic.CategoryID, name));
                        //System.Diagnostics.Process.Start(string.Format("http://rutracker.org/forum/viewtopic.php?t={0}", topic.Value));
                    }




                }
                catch { }
            }
        }


        #region BackgroundWorker
        private Dictionary<BackgroundWorker, Tuple<DateTime, object, string>> backgroundWorkers = new Dictionary<BackgroundWorker, Tuple<DateTime, object, string>>();
        private void dwCreateAndRun(DoWorkEventHandler e, string comment = "...", object argument = null)
        {
            var bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            bw.DoWork += e;
            backgroundWorkers.Add(bw, new Tuple<DateTime, object, string>(DateTime.Now, argument, comment));
            if(backgroundWorkers.Count == 1)
                bw.RunWorkerAsync(argument);

            //toolStripStatusLabel1.Text = comment;
            //toolStripProgressBar1.Visible = true;
            //toolStripStatusLabel1.Visible = true;
            //statusStrip1.Refresh();
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Visible = false;
            toolStripStatusLabel1.Text = "";
            toolStripStatusLabel1.Visible = false;
            statusStrip1.Refresh();

            if (sender != null && sender is BackgroundWorker && backgroundWorkers.ContainsKey(sender as BackgroundWorker))
            {
                var bw = sender as BackgroundWorker;
                
                if (backgroundWorkers.ContainsKey(bw)) backgroundWorkers.Remove(bw);
                bw.Dispose();
                bw = null;
            }
            if (backgroundWorkers.Count > 0)
            {
                var dw = backgroundWorkers.OrderBy(x=>x.Value.Item1).First();
                dw.Key.RunWorkerAsync(dw.Value.Item2);
            }
            SelectionChanged(_CategorySource, null);

            if (e.Result != null)
                _logger.Info(e.Result);
            WriteReports();
            ClientLocalDB.Current.SaveToDatabase();
            System.GC.Collect();

        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (sender != null && sender is BackgroundWorker && backgroundWorkers.ContainsKey(sender as BackgroundWorker))
            {
                toolStripStatusLabel1.Text = backgroundWorkers[sender as BackgroundWorker].Item3;
                toolStripProgressBar1.Visible = true;
                toolStripStatusLabel1.Visible = true;
                statusStrip1.Refresh();
            }
            toolStripProgressBar1.Value = e.ProgressPercentage < 0 || e.ProgressPercentage > 100 ? 100 : e.ProgressPercentage;
        }
        #endregion

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!IsClose)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
            else
                notifyIcon.Visible = false;
        }

        private void _dgvReportDownloads_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var newColumn = _dgvReportDownloads.Columns[e.ColumnIndex];
            if (newColumn == ColumnReport1DgvSelect)
            {
                var data = _TopicsSource.DataSource as List<TopicInfo>;
                if (data == null) return;
                data = data.ToList();
                data.ForEach(x => x.Checked = !x.Checked);
                _TopicsSource.Clear();
                _TopicsSource.DataSource = data;
                return;
            }


            DataGridViewColumn oldColumn = _dgvReportDownloads.SortedColumn;
            SortOrder direction;

            // If oldColumn is null, then the DataGridView is not currently sorted.
            direction = newColumn.HeaderCell.SortGlyphDirection == SortOrder.None || newColumn.HeaderCell.SortGlyphDirection == SortOrder.Descending ? SortOrder.Ascending : SortOrder.Descending;

            if (newColumn == null)
            {
                MessageBox.Show("Select a single column and try again.",
                    "Error: Invalid Selection", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                var data = _TopicsSource.DataSource as List<TopicInfo>;
                if (data == null) return;
                data = data.ToList();
                if (newColumn == ColumnReport1DgvSize)
                    data = (direction == SortOrder.Ascending ? from d in data orderby d.Size select d : from d in data orderby d.Size descending select d).ToList();
                else if (newColumn == ColumnReport1DgvName)
                    data = (direction == SortOrder.Ascending ? from d in data orderby d.Name select d : from d in data orderby d.Name descending select d).ToList();
                else if (newColumn == ColumnReport1DgvSeeders)
                    data = (direction == SortOrder.Ascending ? from d in data orderby d.Seeders, Name select d : from d in data orderby d.Seeders descending, Name select d).ToList();
                else if (newColumn == ColumnReport1DgvAvgSeeders)
                    data = (direction == SortOrder.Ascending ? from d in data orderby d.AvgSeeders, Name select d : from d in data orderby d.AvgSeeders descending, Name select d).ToList();
                else if (newColumn == ColumnReport1DgvRegTime)
                    data = (direction == SortOrder.Ascending ? from d in data orderby d.RegTime, Name select d : from d in data orderby d.RegTime descending, Name select d).ToList();
                else if (newColumn == ColumnReport1DgvStatus)
                    data = (direction == SortOrder.Ascending ? from d in data orderby d.StatusToString, Name select d : from d in data orderby d.StatusToString descending, Name select d).ToList();
                else return;
                _TopicsSource.Clear();
                _TopicsSource.DataSource = data;
                newColumn.HeaderCell.SortGlyphDirection = direction;
            }
        }

        private void _dgvReportDownloads_Click(object sender, EventArgs e)
        {
            DataGridViewColumn newColumn = _dgvReportDownloads.Columns.GetColumnCount(DataGridViewElementStates.Selected) == 1 ? _dgvReportDownloads.SelectedColumns[0] : null;
            Console.WriteLine(""); 
        }

        private void WriteReports()
        {
            ClientLocalDB.Current.CreateReportByRootCategories();
            _tcCetegoriesRootReports.Controls.Clear();

            var reports = ClientLocalDB.Current.GetReports(0);
            var consolidatedReport = reports.Where(x => x.Key.Item2 == 0).Select(x => x.Value.Item2).FirstOrDefault();
            _txtConsolidatedReport.Text = string.IsNullOrWhiteSpace(consolidatedReport) ? string.Empty : consolidatedReport;

            consolidatedReport = reports.Where(x => x.Key.Item2 == 1).Select(x => x.Value.Item2).FirstOrDefault();

            _tbConsolidatedTorrentClientsReport.Text = string.IsNullOrWhiteSpace(consolidatedReport) ? string.Empty : consolidatedReport;
            var categories = ClientLocalDB.Current.GetCategories().Where(x => x.CategoryID > 100000);
            var rootSize = _tcCetegoriesRootReports.Size;
            foreach(var c in categories)
            {
                reports = ClientLocalDB.Current.GetReports(c.CategoryID);
                consolidatedReport = reports.Where(x => x.Key.Item2 == 0).Select(x => x.Value.Item2).FirstOrDefault();
                
                if (string.IsNullOrWhiteSpace(consolidatedReport)) continue;

                var tp = new System.Windows.Forms.TabPage();
                var tb = new System.Windows.Forms.TextBox();

                tp.Location = new System.Drawing.Point(4, 22);
                tp.Padding = new System.Windows.Forms.Padding(3);
                tp.Text = c.Name;
                tp.UseVisualStyleBackColor = true;
                tp.AutoScroll = true;
                tp.Size = rootSize;


                tb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
                tb.Location = new System.Drawing.Point(0, 0);
                tb.Multiline = true;
                tb.ReadOnly = true;
                tb.ScrollBars = System.Windows.Forms.ScrollBars.Both;
                tb.Size = new System.Drawing.Size(rootSize.Width - 8, rootSize.Height - 20);
                tb.Text = consolidatedReport;


                _tcCetegoriesRootReports.Controls.Add(tp);
                tp.Controls.Add(tb);
            }
        }

        private void SaveSetingsToFile()
        {
            try
            {
                var path = string.Empty;
                var dlg = new SaveFileDialog();
                dlg.DefaultExt  = "tloback";
                dlg.Filter = "Файл архивных настроек|*.tloback";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                path = dlg.FileName;

                if (string.IsNullOrWhiteSpace(path)) return;
                using (var stream = System.IO.File.Open(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write))
                {
                    using (var write = new System.IO.BinaryWriter(stream, Encoding.UTF8))
                    {
                        foreach(TorrentClientInfo t in ClientLocalDB.Current.GetTorrentClients())
                        {
                            write.Write("TorrentClientInfo");
                            write.Write(t.UID.ToString());
                            write.Write(t.Name);
                            write.Write(t.Type);
                            write.Write(t.ServerName);
                            write.Write(t.ServerPort);
                            write.Write(t.UserName);
                            write.Write(t.UserPassword);
                        }
                        
                        foreach (Category c in ClientLocalDB.Current.GetCategoriesEnable())
                        {
                            write.Write("Category");
                            write.Write(c.CategoryID);
                            write.Write(c.CountSeeders);
                            write.Write(c.TorrentClientUID.ToString());
                            write.Write(c.Folder);
                            write.Write(c.CreateSubFolder);
                            write.Write(c.IsSaveTorrentFiles);
                            write.Write(c.IsSaveWebPage);
                            write.Write(c.Label);
                        }
                        var cats = ClientLocalDB.Current.GetCategoriesEnable().Select(x => x.CategoryID).ToArray();
                        foreach (var rp in ClientLocalDB.Current.GetReports().Where(x=> cats.Contains(x.Key.Item1)))
                        {
                            write.Write("Report");
                            write.Write(rp.Key.Item1);
                            write.Write(rp.Key.Item2);
                            write.Write(rp.Value.Item1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Trace(ex.StackTrace);
            }

        }

        private void ReadSettingsFromFile()
        {
            try
            {
                var path = string.Empty;
                var dlg = new OpenFileDialog();
                dlg.DefaultExt = "tloback";
                dlg.Filter = "Файл архивных настроек|*.tloback";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                path = dlg.FileName;

                if (string.IsNullOrWhiteSpace(path)) return;
                var torrentClients = new List<TorrentClientInfo>();
                var categoies = new List<Category>();
                var reports = new List<Tuple<int, int, string>>();
                using (var stream = System.IO.File.Open(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Read))
                {
                    using (var rdr = new System.IO.BinaryReader(stream))
                    {
                        while (rdr.BaseStream.Length != rdr.BaseStream.Position)
                        {
                            var name = rdr.ReadString();
                            switch(name)
                            {
                                case "TorrentClientInfo":
                                    {
                                        var t = new TorrentClientInfo()
                                        {
                                            UID = Guid.Parse(rdr.ReadString()),
                                            Name = rdr.ReadString(),
                                            Type = rdr.ReadString(),
                                            ServerName = rdr.ReadString(),
                                            ServerPort = rdr.ReadInt32(),
                                            UserName = rdr.ReadString(),
                                            UserPassword = rdr.ReadString()
                                        };
                                        torrentClients.Add(t);

                                    }
                                    break;
                                case "Category":
                                    {
                                        var c = new Category()
                                        {
                                            CategoryID = rdr.ReadInt32(),
                                            IsEnable = true,
                                            CountSeeders = rdr.ReadInt32(),
                                            TorrentClientUID = Guid.Parse(rdr.ReadString()),
                                            Folder = rdr.ReadString(),
                                            CreateSubFolder = rdr.ReadInt32(),
                                            IsSaveTorrentFiles = rdr.ReadBoolean(),
                                            IsSaveWebPage = rdr.ReadBoolean(),
                                            Label = rdr.ReadString(),
                                        };
                                        categoies.Add(c);
                                    }
                                    break;
                                case "Report":
                                    {
                                        reports.Add(new Tuple<int, int, string>(rdr.ReadInt32(), rdr.ReadInt32(), rdr.ReadString()));
                                    }
                                    break;
                            }
                        }
                        ClientLocalDB.Current.SaveTorrentClients(torrentClients);
                        ClientLocalDB.Current.CategoriesSave(categoies);
                        ClientLocalDB.Current.SaveSettingsReport(reports);
                        ClientLocalDB.Current.SaveToDatabase();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Trace(ex.StackTrace);
            }
        }

    }
}
