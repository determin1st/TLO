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
    internal partial class SelectCategory : Form
    {
        static NLog.Logger _logger;
        public Category SelectedCategory { get; private set; }
        public List<Category> SelectedCategories { get; private set; }

        public SelectCategory()
        {
            if (_logger == null) _logger = NLog.LogManager.GetLogger("SelectCategory");
            InitializeComponent();
            SelectedCategories = new List<Category>();
        }

        public void Read()
        {
            try
            {
                ClientLocalDB.Current.CategoriesSave(RuTrackerOrg.Current.GetCategories(), true);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Не удалось загрузить список категорий.\r\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                _logger.Error(ex.Message + "\r\n" + ex.StackTrace);
            }
            var categories = ClientLocalDB.Current.GetCategories().OrderBy(x=>x.FullName).ToArray();
            foreach (var category1 in categories.Where(x => x.CategoryID > 999999).OrderBy(x => x.FullName).ToArray())
            {
                List<TreeNode> array1 = new List<TreeNode>();

                foreach (var category2 in categories.Where(x => x.ParentID == category1.CategoryID).OrderBy(x => x.FullName).ToArray())
                {
                    List<TreeNode> array2 = new List<TreeNode>();
                    foreach (var category3 in categories.Where(x => x.ParentID == category2.CategoryID).OrderBy(x => x.FullName).ToArray())
                    {
                        var obj3 = new TreeNode(category3.Name);
                        obj3.Tag = category3;
                        array2.Add(obj3);
                    }

                    if (array2.Count() != 0)
                    {
                        var obj2 = new TreeNode(category2.Name, array2.ToArray());
                        obj2.Tag = category2;
                        array1.Add(obj2);
                    }
                    else
                    {
                        var obj2 = new TreeNode(category2.Name);
                        obj2.Tag = category2;
                        array1.Add(obj2);
                    }
                }
                if (array1.Count() != 0)
                {
                    var obj = new TreeNode(category1.Name, array1.ToArray());
                    obj.Tag = category1;
                    treeView1.Nodes.Add(obj);
                }
                else
                {
                    var obj = new TreeNode(category1.Name);
                    obj.Tag = category1;
                    treeView1.Nodes.Add(obj);
                }
            }

        }

        private void _btCancel_Click(object sender, EventArgs e)
        {
            SelectedCategory = null;
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }

        private void _btSelected_Click(object sender, EventArgs e)
        {
            try
            {
                if (treeView1 == null) return;
                var selectedNode = treeView1.SelectedNode;
                if (selectedNode == null) return;
                var category = selectedNode.Tag as Category;
                if (category == null || category.CategoryID > 999999)
                    MessageBox.Show("Не выбран раздел или выбран корневой раздел\r\n(Корневой раздел нельзя выбирать)");
                else
                {
                    SelectedCategory = category;
                    DialogResult = System.Windows.Forms.DialogResult.OK;
                    Close();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Интересно что вывалит? :(.\r\n " + ex.Message);
            }
        }

        private void _txtFrom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {

                if (string.IsNullOrWhiteSpace(_txtFrom.Text)) return;
                try
                {
                    if(_txtFrom.Text.Split('=').Length != 2) return;
                    var rt = new RuTrackerOrg(Settings.Current.KeeperName, Settings.Current.KeeperPass);
                    var cat = rt.GetCategoriesFromPost(_txtFrom.Text);
                    SelectedCategories = (from c in ClientLocalDB.Current.GetCategories()
                                          join t in cat on c.CategoryID equals t.Item1
                                          select c).ToList();
                    var reports = new List<Tuple<int, int, string>>();
                    foreach (var c in cat)
                    {
                        reports.Add(new Tuple<int, int, string>(c.Item1, 0, c.Item2));
                    }
                    ClientLocalDB.Current.SaveSettingsReport(reports);
                    DialogResult = System.Windows.Forms.DialogResult.OK;
                    Close();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    _logger.Error(ex.Message);
                    _logger.Debug(ex.StackTrace);
                }
            }
        }
    }
}
