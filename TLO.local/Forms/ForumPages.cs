using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TLO.local
{
    public partial class ForumPages : UserControl
    {
        private List<Tuple<int, int, TextBox>> Urls { get; set; }

        public ForumPages()
        {
            Urls = new List<Tuple<int, int, TextBox>>();
            InitializeComponent();
        }


        public void LoadSettings()
        {
            this.panel1.Controls.Clear();

            var reports = ClientLocalDB.Current.GetReports();
            var categories = ClientLocalDB.Current.GetCategoriesEnable();
            categories.Add(new Category() { CategoryID = 0, Name = " Сводный отчет", FullName = " Сводный отчет" });


            var tabIndex = 0;
            var height = 10;
            foreach (var category in categories.OrderBy(x => x.FullName))
            {
                var label = new Label();
                label.AutoSize = true;
                label.Location = new System.Drawing.Point(3, height);

                label.Size = new System.Drawing.Size(35, 13);
                label.TabIndex = tabIndex;
                label.Text = category.FullName;
                this.panel1.Controls.Add(label);

                height = height + 16;
                var reportsFilter = reports.Where(x => x.Key.Item1 == category.CategoryID).OrderBy(x => x.Key.Item2).ToArray();
                reportsFilter = reportsFilter.Length == 0 ? new KeyValuePair<Tuple<int, int>, Tuple<string, string>>[]{ new KeyValuePair<Tuple<int, int>, Tuple<string, string>>(new Tuple<int, int>(category.CategoryID,0), new Tuple<string, string>("", ""))} : reportsFilter;
                foreach (var report in reportsFilter)
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
                }
            }
        }
        public void Save()
        {
            var result = Urls.Select(x => new Tuple<int, int, string>(x.Item1, x.Item2, x.Item3.Text)).ToList();
            ClientLocalDB.Current.SaveSettingsReport(result);

        }
    }
}
