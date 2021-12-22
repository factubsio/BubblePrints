using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BlueprintExplorer.Form1;

namespace BlueprintExplorer
{
    public partial class NotificationsView : Form
    {
        public void Show(Form owner, List<BubbleNotification> notifications)
        {
            table.Controls.Clear();
            int row = 0;
            table.RowStyles.Clear();
            for (int i = 0; i < notifications.Count + 1; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            }
            foreach (var notification in notifications)
            {
                var label = new Label()
                {
                    Text = notification.Message,
                    Height = 100,
                    Dock = DockStyle.Fill,
                };

                var button = new Button()
                {
                    Text = notification.ActionText,
                    Dock = DockStyle.Fill,
                };
                button.Click += (_, _) =>
                {
                    notification.Complete = true;
                    notification.Action();
                    button.Enabled = false;
                };

                var nTable = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                };
                nTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                nTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                nTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                nTable.Controls.Add(label, 0, 0);
                nTable.Controls.Add(button, 0, 1);

                table.Controls.Add(nTable, 0, row);

                row++;
            }
            table.Controls.Add(new Label()
            {
                Text = "",
                Dock = DockStyle.Fill
            }, 0, 1);

            StartPosition = FormStartPosition.CenterParent;
            ShowDialog(owner);
        }

        public NotificationsView()
        {
            InitializeComponent();
        }
    }
}
