using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TLO.local
{
    public partial class SettingsForm : Form
    {
        private BindingSource _TorrentClientsSource = new BindingSource();
        private BindingSource _CategoriesSource = new BindingSource();

        public SettingsForm()
        {
            InitializeComponent();

            _tbTorrentClientName.Enabled = false;
            _cbTorrentClientType.Enabled = false;
            _tbTorrentClientHostIP.Enabled = false;
            _tbTorrentClientPort.Enabled = false;
            _tbTorrentClientUserName.Enabled = false;
            _tbTorrentClientUserPassword.Enabled = false;

            dgwTorrentClients.AutoGenerateColumns = false;
            dgwTorrentClients.ClearSelection();
            dgwTorrentClients.DataSource = null;
            _TorrentClientsSource = new BindingSource();
            _TorrentClientsSource.DataSource = ClientLocalDB.Current.GetTorrentClients();
            dgwTorrentClients.DataSource = _TorrentClientsSource;

            dgwCategories.AutoGenerateColumns = false;
            dgwCategories.ClearSelection();
            dgwCategories.DataSource = null;
            _CategoriesSource = new BindingSource();
            _CategoriesSource.DataSource = ClientLocalDB.Current.GetCategoriesEnable();
            dgwCategories.DataSource = _CategoriesSource;
            if (_CategoriesSource.Count > 0) _CategoriesSource.Position = 0;
            if (_TorrentClientsSource.Count > 0) _TorrentClientsSource.Position = 0;

            forumPages1.LoadSettings();
            //CreatePageAllCategories();

            
            var settings = Settings.Current;
            _appKeeperName.Text = settings.KeeperName;
            _appKeeperPass.Text = settings.KeeperPass;
            _appIsUpdateStatistics.Checked = settings.IsUpdateStatistics;
            _appCountDaysKeepHistory.Value = settings.CountDaysKeepHistory;
            _appPeriodRunAndStopTorrents.Value = settings.PeriodRunAndStopTorrents;
            _appCountSeedersReport.Value = settings.CountSeedersReport;
            _appIsAvgCountSeeders.Checked = settings.IsAvgCountSeeders;
            _appSelectLessOrEqual.Checked = settings.IsSelectLessOrEqual;
            _appLogLevel.Value = settings.LogLevel.HasValue ? settings.LogLevel.Value : 0;
            _appIsNotSaveStatistics.Checked = settings.IsNotSaveStatistics;
            _appReportTop1.Text = settings.ReportTop1;
            _appReportTop2.Text = settings.ReportTop2;
            _appReportLine.Text = settings.ReportLine;
            _appReportBottom.Text = settings.ReportBottom;
        }


        private void _Focus_Enter(object sender, EventArgs e)
        {
            if (_TorrentClientsSource.Current != null)
            {
                #region
                var obj = _TorrentClientsSource.Current as TorrentClientInfo;
                if (sender == _tbTorrentClientName)
                    obj.Name = _tbTorrentClientName.Text;
                else if (sender == _cbTorrentClientType)
                    obj.Type = _cbTorrentClientType.Text;
                else if (sender == _tbTorrentClientHostIP)
                    obj.ServerName = _tbTorrentClientHostIP.Text;
                else if (sender == _tbTorrentClientPort)
                {
                    int i = 0;
                    if (int.TryParse(_tbTorrentClientPort.Text, out i))
                        obj.ServerPort = i;
                    else
                        _tbTorrentClientPort.Text = "0";
                }
                else if (sender == _tbTorrentClientUserName)
                    obj.UserName = _tbTorrentClientUserName.Text;
                else if (sender == _tbTorrentClientUserPassword)
                    obj.UserPassword = _tbTorrentClientUserPassword.Text;
                else if (sender == _tcrbCurrent && _tcrbCurrent.Checked)
                {
                    obj.ServerName = "127.0.0.1";
                    _tbTorrentClientHostIP.Enabled = false;
                }
                else if (sender == _tcrbRemote && _tcrbRemote.Checked)
                {
                    obj.ServerName = _tbTorrentClientHostIP.Text; _tbTorrentClientHostIP.Enabled = true;
                }
                #endregion
            }
            if (_CategoriesSource.Current != null)
            {
                var obj = _CategoriesSource.Current as Category;
                if (sender == _CategoriesCbTorrentClient)
                {
                    var obj2 = _CategoriesCbTorrentClient.SelectedItem as TorrentClientInfo;
                    if (obj2 != null) obj.TorrentClientUID = obj2.UID;
                }
                else if (sender == _CategoriesCbStartCountSeeders)
                {
                    var i = 0;
                    if (int.TryParse(_CategoriesCbStartCountSeeders.SelectedItem as string, out i))
                        obj.CountSeeders = i;
                }
                else if (sender == _CategoriesTbFolderDownloads)
                    obj.Folder = _CategoriesTbFolderDownloads.Text;
                else if (sender == _cbIsSaveTorrentFile)
                    obj.IsSaveTorrentFiles = _cbIsSaveTorrentFile.Checked;
                else if (sender == _cbIsSaveWebPage)
                    obj.IsSaveWebPage = _cbIsSaveWebPage.Checked;
                else if (sender == _cbSubFolder)
                {
                    var value = _cbSubFolder.SelectedItem as string;
                    if (string.IsNullOrWhiteSpace(value)) return;
                    /*  Не нужен
                        С ID топика
                        Запрашивать*/
                    switch (value)
                    {
                        case "Не нужен":
                            obj.CreateSubFolder = 0;
                            break;
                        case "С ID топика":
                            obj.CreateSubFolder = 1;
                            break;
                        case "Запрашивать":
                            obj.CreateSubFolder = 2;
                            break;
                    }
                }
                else if (sender == _CategoriesTbLabel)
                    obj.Label = string.IsNullOrWhiteSpace(_CategoriesTbLabel.Text) ? obj.FullName : _CategoriesTbLabel.Text.Trim();
            }
        }

        private void SelectionChanged(object sender, EventArgs e)
        {
            #region dgwTorrentClients
            if (sender == dgwTorrentClients)
            {
                if (_TorrentClientsSource.Current == null)
                {
                    _tbTorrentClientName.Enabled = false;
                    _cbTorrentClientType.Enabled = false;
                    _tbTorrentClientHostIP.Enabled = false;
                    _tbTorrentClientPort.Enabled = false;
                    _tbTorrentClientUserName.Enabled = false;
                    _tbTorrentClientUserPassword.Enabled = false;

                    _tbTorrentClientName.Text = string.Empty;
                    _cbTorrentClientType.Text = string.Empty;
                    _tbTorrentClientHostIP.Text = string.Empty;
                    _tbTorrentClientPort.Text = string.Empty;
                    _tbTorrentClientUserName.Text = string.Empty;
                    _tbTorrentClientUserPassword.Text = string.Empty;
                    _tcrbRemote.Checked = false;
                    _tcrbCurrent.Checked = true;
                    _tbTorrentClientHostIP.Enabled = false;
                }
                else
                {
                    var obj = _TorrentClientsSource.Current as TorrentClientInfo;
                    _tbTorrentClientName.Enabled = true;
                    _cbTorrentClientType.Enabled = true;
                    _tbTorrentClientHostIP.Enabled = true;
                    _tbTorrentClientPort.Enabled = true;
                    _tbTorrentClientUserName.Enabled = true;
                    _tbTorrentClientUserPassword.Enabled = true;

                    _tbTorrentClientName.Text = obj.Name;
                    _cbTorrentClientType.Text = obj.Type;
                    if (obj.ServerName == "127.0.0.1")
                    {
                        _tcrbRemote.Checked = false;
                        _tcrbCurrent.Checked = true;
                    }
                    else
                    {
                        _tbTorrentClientHostIP.Text = obj.ServerName;
                        _tcrbCurrent.Checked = false;
                        _tcrbRemote.Checked = true;
                    }
                    _tbTorrentClientPort.Text = obj.ServerPort.ToString();
                    _tbTorrentClientUserName.Text = obj.UserName;
                    _tbTorrentClientUserPassword.Text = obj.UserPassword;
                }
            }
            #endregion

            #region dgwCategories
            if (sender == dgwCategories)
            {
                if (_CategoriesSource.Current == null)
                {
                    _CategoriesTbCategoryID.Text = string.Empty;
                    _CategoriesTbFullName.Text = string.Empty;
                    _CategoriesCbStartCountSeeders.Enabled = false;
                    _CategoriesTbLabel.Text = string.Empty;
                }
                else
                {
                    var obj = _CategoriesSource.Current as Category;
                    _CategoriesTbCategoryID.Text = obj.CategoryID.ToString();
                    _CategoriesTbFullName.Text = obj.FullName;
                    _CategoriesCbStartCountSeeders.Enabled = true;
                    _CategoriesCbStartCountSeeders.SelectedItem = obj.CountSeeders >= 0 ? obj.CountSeeders.ToString() : "-";
                    _CategoriesTbFolderDownloads.Text = obj.Folder;

                    _CategoriesCbTorrentClient.DataSource = null;
                    _CategoriesCbTorrentClient.DataSource = _TorrentClientsSource.DataSource;
                    var cl = _CategoriesCbTorrentClient.DataSource as List<TorrentClientInfo>;
                    _CategoriesCbTorrentClient.SelectedItem = cl.Where(x => x.UID == obj.TorrentClientUID).FirstOrDefault();

                    /*  Не нужен
                        С ID топика
                        Запрашивать*/
                    switch (obj.CreateSubFolder)
                    {
                        case 0:
                            _cbSubFolder.SelectedItem = "Не нужен";
                            break;
                        case 1:
                            _cbSubFolder.SelectedItem = "С ID топика";
                            break;
                        case 2:
                            _cbSubFolder.SelectedItem = "Запрашивать";
                            break;
                    }
                    _cbIsSaveWebPage.Checked = obj.IsSaveWebPage;
                    _cbIsSaveTorrentFile.Checked = obj.IsSaveTorrentFiles;
                    _CategoriesTbLabel.Text = string.IsNullOrWhiteSpace(obj.Label) ? obj.FullName : obj.Label;
                }
            }
            #endregion

            if (sender == _appIsNotSaveStatistics)
            {
                if (_appIsNotSaveStatistics.Checked)
                {
                    _appIsUpdateStatistics.Checked = false;
                    _appIsUpdateStatistics.Enabled = false;
                }
                else
                    _appIsUpdateStatistics.Enabled = true;
            }
        }

        private void ClickButtons(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            #region TorrentClients
            try
            {
                if (sender == _btTorrentClientAdd)
                {
                    _TorrentClientsSource.Add(new TorrentClientInfo());
                    _TorrentClientsSource.Position = _TorrentClientsSource.Count;
                }
                else if (sender == _btTorrentClientDelete)
                {
                    if (_TorrentClientsSource.Current == null)
                        return;

                    var obj = _TorrentClientsSource.Current as TorrentClientInfo;

                    if (MessageBox.Show("Вы хотите удалить из списка torrent-клиент \"" + obj.Name + "\"?", "Запрос подтверждения", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                        _TorrentClientsSource.Remove(obj);
                }
            }
            catch { }
            #endregion

            #region Categories
            try
            {
                if (sender == _btCategoryAdd)
                {
                    var dialog = new SelectCategory();
                    dialog.Read();
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        if (dialog.SelectedCategories.Count() > 0)
                        {
                            dialog.SelectedCategories.ForEach(x => 
                            { 
                                x.IsEnable = true;
                                _CategoriesSource.Add(x);
                            });
                            _CategoriesSource.Position = _CategoriesSource.Count;
                        }

                        if (dialog.SelectedCategory == null)
                            return;
                        else if ((_CategoriesSource.DataSource as List<Category>).Any(x => x.CategoryID == dialog.SelectedCategory.CategoryID))
                            MessageBox.Show("Выбрана категория уже присутствует");
                        else
                        {
                            dialog.SelectedCategory.IsEnable = true;
                            _CategoriesSource.Add(dialog.SelectedCategory);
                            _CategoriesSource.Position = _CategoriesSource.Count;
                        }
                    }
                }
                else if (sender == _btCategoryRemove)
                {
                    if (_CategoriesSource.Current == null)
                        return;

                    var obj = _CategoriesSource.Current as Category;

                    if (MessageBox.Show("Удалить из обработки раздел \"" + obj.Name + "\"?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                        _CategoriesSource.Remove(obj);
                }
                else if (sender == _CategoriesBtSelectFolder)
                {
                    if (_CategoriesSource.Current == null)
                        return;

                    var obj = _CategoriesSource.Current as Category;

                    var dlg = new FolderBrowserDialog();
                    dlg.SelectedPath = string.IsNullOrWhiteSpace(obj.Folder) ? "c:\\" : obj.Folder;
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        obj.Folder = dlg.SelectedPath;
                        _CategoriesTbFolderDownloads.Text = obj.Folder;
                    }
                }
            }
            catch { }
            #endregion

            try
            {
                if (sender == _btSave)
                {
                    ClientLocalDB.Current.SaveTorrentClients(_TorrentClientsSource.DataSource as List<TorrentClientInfo>, true);
                    ClientLocalDB.Current.CategoriesSave(_CategoriesSource.DataSource as List<Category>);
                    forumPages1.Save();
                    DialogResult = System.Windows.Forms.DialogResult.OK;
                    var settings = Settings.Current;
                    settings.KeeperName = _appKeeperName.Text;
                    settings.KeeperPass= _appKeeperPass.Text;
                    settings.IsUpdateStatistics = _appIsUpdateStatistics.Checked;
                    settings.CountDaysKeepHistory = (int)_appCountDaysKeepHistory.Value;
                    settings.PeriodRunAndStopTorrents = (int)_appPeriodRunAndStopTorrents.Value;
                    settings.CountSeedersReport = (int)_appCountSeedersReport.Value;
                    settings.IsAvgCountSeeders = _appIsAvgCountSeeders.Checked;
                    settings.IsSelectLessOrEqual = _appSelectLessOrEqual.Checked;
                    settings.LogLevel = (int)_appLogLevel.Value;
                    settings.IsNotSaveStatistics = _appIsNotSaveStatistics.Checked;
                    settings.ReportTop1 = _appReportTop1.Text.Replace("\n", "\r\n").Replace("\r\r", "\r");
                    settings.ReportTop2 = _appReportTop2.Text.Replace("\n", "\r\n").Replace("\r\r", "\r");
                    settings.ReportLine = _appReportLine.Text.Replace("\n", "\r\n").Replace("\r\r", "\r");
                    settings.ReportBottom = _appReportBottom.Text.Replace("\n", "\r\n").Replace("\r\r", "\r");
                    settings.Save();
                    ClientLocalDB.Current.SaveToDatabase();
                    Close();
                }
                else if (sender == _btCancel)
                {
                    DialogResult = System.Windows.Forms.DialogResult.Cancel;
                    Close();
                }
                else if (_btCheck == sender)
                {
                    List<string> errors = new List<string>();
                    foreach (var c in (_TorrentClientsSource.DataSource as List<TorrentClientInfo>))
                    {
                        try
                        {
                            ITorrentClient tc = c.Create();
                            if (tc == null)
                            {
                                errors.Add(string.Format("Торрент-клиент \"{0}\": Не удалось определить тип torrent-клиента", c.Name));
                                continue;
                            }
                            tc.Ping();
                        }
                        catch
                        {
                            errors.Add(string.Format("Не удалось подключиться к торрент-клиенту \"{0}\"", c.Name));
                        }
                    }
                    foreach (var t in errors)
                        MessageBox.Show(t, "Проверка");
                    MessageBox.Show("Подключение к torrent-клиентам проверено.", "Проверка");
                }
            }
            catch(Exception ex)
            {
                Cursor.Current = Cursors.Default;
                MessageBox.Show(ex.Message);
            }
        }

        private void CreatePageAllCategories()
        {
            var control = this._tpAllCategories.Controls[0];
            

            var categories = ClientLocalDB.Current.GetCategories().ToDictionary(x=>x.CategoryID, x=>x);
            var categoriesEnable = ClientLocalDB.Current.GetCategoriesEnable().ToDictionary(x=>x.CategoryID, x=>x);
            var keyParent = categoriesEnable.ToDictionary(x => x.Key, x => x.Value.ParentID);
            for (int i = 0; i < 3; i++)
            {
                foreach (var c in categoriesEnable.Values.ToArray())
                {
                    if (categoriesEnable.ContainsKey(c.ParentID) || !categories.ContainsKey(c.ParentID)) continue;
                    categoriesEnable.Add(categories[c.ParentID].CategoryID, categories[c.ParentID]);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                var cl = categories.Values.ToList();
                foreach (var c in categoriesEnable.Values.ToList())
                {
                    var ccl = cl.Where(x => !categoriesEnable.ContainsKey(x.CategoryID) && x.ParentID == c.CategoryID).ToArray();
                    foreach (var cc in ccl)
                    {
                        if (categoriesEnable.ContainsKey(cc.CategoryID) || !categories.ContainsKey(cc.CategoryID)) continue;
                        categoriesEnable.Add(cc.CategoryID, cc);
                    }
                }
            }

            var reports = ClientLocalDB.Current.GetReports().Where(x => x.Key.Item2 == 0 && x.Key.Item1 != 0).ToDictionary(x => x.Key.Item1, x => x.Value.Item1);

            var tabIndex = 0;
            var height = 10;
            foreach (var category in categoriesEnable.Values.OrderBy(x => x.FullName))
            {
                var label = new Label();
                label.AutoSize = true;
                label.Location = new System.Drawing.Point(3, height);

                label.Size = new System.Drawing.Size(35, 13);
                label.TabIndex = tabIndex;
                label.Text = category.FullName;
                control.Controls.Add(label);

                height = height + 16;


                var lavel = new Label();
                lavel.Location = new System.Drawing.Point(6, height);
                lavel.Size = new System.Drawing.Size(123, 20);
                lavel.Text = "Списки хранимого";
                control.Controls.Add(lavel);

                tabIndex++;
                var textBox = new TextBox();
                textBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
                textBox.Location = new System.Drawing.Point(135, height);
                textBox.Size = new System.Drawing.Size(this.panel1.Size.Width - 135, 20);
                textBox.TabIndex = tabIndex;
                textBox.Text = string.IsNullOrWhiteSpace(category.ReportList) && reports.ContainsKey(category.CategoryID) ? reports[category.CategoryID] : category.ReportList;
                control.Controls.Add(textBox);

                //Urls.Add(new Tuple<int, int, TextBox>(report.Key.Item1, report.Key.Item2, textBox));
                height = height + 26;

                /*foreach (var report in reports.Where(x => x.Key.Item1 == category.CategoryID).OrderBy(x => x.Key.Item2))
                {
                    if (category.CategoryID == 0 && report.Key.Item2 != 0) continue;
                    tabIndex++;
                    var textBox = new TextBox();
                    textBox.Enabled = false;
                    textBox.Location = new System.Drawing.Point(6, height);
                    textBox.Size = new System.Drawing.Size(123, 20);
                    textBox.TabIndex = tabIndex;
                    textBox.Text = "Отчет " + (report.Key.Item2 != 0 ? report.Key.Item2.ToString() + (report.Value.Item2 == "Резерв" ? " (Резерв)" : "") : " (Шапка)");
                    if (category.CategoryID == 0) textBox.Text = "Сводный отчет";
                    this.panel1.Controls.Add(textBox);
                    // 
                    // textBox2
                    // 
                    tabIndex++;
                    textBox = new TextBox();
                    textBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
                    textBox.Location = new System.Drawing.Point(135, height);
                    textBox.Size = new System.Drawing.Size(this.panel1.Size.Width - 135, 20);
                    textBox.TabIndex = tabIndex;
                    textBox.Text = string.IsNullOrWhiteSpace(report.Value.Item1) ? "" : report.Value.Item1;
                    this.panel1.Controls.Add(textBox);

                    Urls.Add(new Tuple<int, int, TextBox>(report.Key.Item1, report.Key.Item2, textBox));
                    height = height + 26;
                }*/
            }
        }
    }
}
