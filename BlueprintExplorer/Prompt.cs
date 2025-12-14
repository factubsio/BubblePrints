using System;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public class Prompt : IDisposable
    {
        public Form RootForm { get; private set; }
        public  bool Result { get; private set; }

        public Prompt(string caption, Action<Panel> builder)
        {
            Result = ShowDialog(caption, builder);
        }
        //use a using statement
        private bool ShowDialog(string caption, Action<Panel> builder)
        {
            RootForm = new Form()
            {
                Width = 800,
                Height = 800,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true
            };

            Button confirmation = new() { Text = "Ok", DialogResult = DialogResult.OK, Height = 40 };
            confirmation.Click += (sender, e) => { RootForm.Close(); };
            confirmation.Dock = DockStyle.Bottom;
            RootForm.Controls.Add(confirmation);
            RootForm.AcceptButton = confirmation;


            var root = new Panel();
            root.Dock = DockStyle.Fill;
            RootForm.Controls.Add(root);
            builder(root);

            return RootForm.ShowDialog() == DialogResult.OK;
        }

        public void Dispose()
        {
            //See Marcus comment
            if (RootForm != null)
            {
                RootForm.Dispose();
                RootForm = null;
            }
        }
    }
}
