using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.IO;

namespace GitVersionTree
{
    public partial class MainForm : Form
    {
        private Dictionary<string, string> DecorateDictionary = new Dictionary<string, string>();
        private List<List<string>> Nodes = new List<List<string>>();
        
        private string DotFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + Application.ProductName + ".dot";
        private string PdfFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + Application.ProductName + ".pdf";
        private string LogFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + Application.ProductName + ".log";
        string RepositoryName;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Text = Application.ProductName + " - v" + Application.ProductVersion.Substring(0, 3);

            RefreshPath();
        }

        private void GitPathBrowseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog BrowseOpenFileDialog = new OpenFileDialog();
            BrowseOpenFileDialog.Title = "Select git.exe";
            if (!String.IsNullOrEmpty(Reg.Read("GitPath")))
            {
                BrowseOpenFileDialog.InitialDirectory = Reg.Read("GitPath");
            }
            BrowseOpenFileDialog.FileName = "git.exe";
            BrowseOpenFileDialog.Filter = "Git Application (git.exe)|git.exe";
            if (BrowseOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                Reg.Write("GitPath", BrowseOpenFileDialog.FileName);
                RefreshPath();
            }
        }

        private void GraphvizDotPathBrowseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog BrowseOpenFileDialog = new OpenFileDialog();
            BrowseOpenFileDialog.Title = "Select dot.exe";
            if (!String.IsNullOrEmpty(Reg.Read("GraphvizPath")))
            {
                BrowseOpenFileDialog.InitialDirectory = Reg.Read("GraphvizPath");
            }
            BrowseOpenFileDialog.FileName = "dot.exe";
            BrowseOpenFileDialog.Filter = "Graphviz Dot Application (dot.exe)|dot.exe";
            if (BrowseOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                Reg.Write("GraphvizPath", BrowseOpenFileDialog.FileName);
                RefreshPath();
            }
        }

        private void GitRepositoryPathBrowseButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog BrowseFolderBrowserDialog = new FolderBrowserDialog();
            BrowseFolderBrowserDialog.Description = "Select Git repository";
            BrowseFolderBrowserDialog.ShowNewFolderButton = false;
            if (!String.IsNullOrEmpty(Reg.Read("GitRepositoryPath")))
            {
                BrowseFolderBrowserDialog.SelectedPath = Reg.Read("GitRepositoryPath");
            }
            if (BrowseFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Reg.Write("GitRepositoryPath", BrowseFolderBrowserDialog.SelectedPath);
                RefreshPath();
            }
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Reg.Read("GitPath")) ||
                String.IsNullOrEmpty(Reg.Read("GraphvizPath")) ||
                String.IsNullOrEmpty(Reg.Read("GitRepositoryPath")))
            {
                MessageBox.Show("Please select a Git, Graphviz & Git repository.", "Generate", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                StatusRichTextBox.Text = "";
                RepositoryName = new DirectoryInfo(GitRepositoryPathTextBox.Text).Name;
                DotFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + RepositoryName + ".dot";
                PdfFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + RepositoryName + ".pdf";
                LogFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + RepositoryName + ".log";
                File.WriteAllText(LogFilename, "");
                Generate();
            }
        }

        private void HomepageLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/crc8/GitVersionTree");
        }
        
        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RefreshPath()
        {
            if (!String.IsNullOrEmpty(Reg.Read("GitPath")))
            {
                GitPathTextBox.Text = Reg.Read("GitPath");
            }
            if (!String.IsNullOrEmpty(Reg.Read("GraphvizPath")))
            {
                GraphvizDotPathTextBox.Text = Reg.Read("GraphvizPath");
            }
            if (!String.IsNullOrEmpty(Reg.Read("GitRepositoryPath")))
            {
                GitRepositoryPathTextBox.Text = Reg.Read("GitRepositoryPath");
            }
        }

        private void Status(string Message)
        {
            StatusRichTextBox.AppendText(DateTime.Now + " - " + Message + "\r\n");
            StatusRichTextBox.SelectionStart = StatusRichTextBox.Text.Length;
            StatusRichTextBox.ScrollToCaret();
            Refresh();
        }

        private string Execute(string Command, string Argument)
        {
            string ExecuteResult = String.Empty;
            Process ExecuteProcess = new Process();
            ExecuteProcess.StartInfo.UseShellExecute = false;
            ExecuteProcess.StartInfo.CreateNoWindow = true;
            ExecuteProcess.StartInfo.RedirectStandardOutput = true;
            ExecuteProcess.StartInfo.FileName = Command;
            ExecuteProcess.StartInfo.Arguments = Argument;
            ExecuteProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ExecuteProcess.Start();
            ExecuteResult = ExecuteProcess.StandardOutput.ReadToEnd();
            ExecuteProcess.WaitForExit();
            if (ExecuteProcess.ExitCode == 0)
            {
                return ExecuteResult;
            }
            else
            {
                return String.Empty;
            }
        }

        private void Generate()
        {
            var repo = new LibGit2Sharp.Repository(Reg.Read("GitRepositoryPath"));

            StringBuilder DotStringBuilder = new StringBuilder();
            Status("Generating dot file ...");
            DotStringBuilder.Append("digraph " + RepositoryName.Replace(" ","").Replace("-","") + " {\r\n");

            var objs = repo.ObjectDatabase;

            foreach(var obj in objs)
            {
                if (obj.GetType() == typeof(LibGit2Sharp.Commit))
                {
                    var commit = (LibGit2Sharp.Commit)obj; 

                    foreach (var parent in commit.Parents)
                    {
                        DotStringBuilder.Append(
                            "\"" + 
                            parent.Sha + "\n" +
                            parent.Author + "\n" +
                            parent.MessageShort.Replace("\"","'") + "\n" +
                            "\"" + 
                            "->" + 
                            "\"" +
                            commit.Sha + "\n" +
                            commit.Author + "\n" +
                            commit.MessageShort.Replace("\"", "'") + "\n" +
                            "\"" + 
                            ";\n");
                    }
                }
            }

            DotStringBuilder.Append("}\r\n");
            File.WriteAllText(@DotFilename, DotStringBuilder.ToString());

            Status("Generating version tree ...");
            Process DotProcess = new Process();
            DotProcess.StartInfo.UseShellExecute = false;
            DotProcess.StartInfo.CreateNoWindow = true;
            DotProcess.StartInfo.RedirectStandardOutput = true;
            DotProcess.StartInfo.FileName = GraphvizDotPathTextBox.Text;
            DotProcess.StartInfo.Arguments = "\"" + @DotFilename + "\" -Tpdf -Gsize=10,10 -o\"" + @PdfFilename + "\"";
            DotProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            DotProcess.Start();
            DotProcess.WaitForExit();

            DotProcess.StartInfo.Arguments = "\"" + @DotFilename + "\" -Tps -o\"" + @PdfFilename.Replace(".pdf", ".ps") + "\"";
            DotProcess.Start();
            DotProcess.WaitForExit();
            if (DotProcess.ExitCode == 0)
            {
                if (File.Exists(@PdfFilename))
                {
#if (!DEBUG)
                    /*
                    Process ViewPdfProcess = new Process();
                    ViewPdfProcess.StartInfo.FileName = @PdfFilename;
                    ViewPdfProcess.Start();
                    //ViewPdfProcess.WaitForExit();
                    //Close();
                    */
#endif
                }
            }
            else
            {
                Status("Version tree generation failed ...");
            }

            Status("Done! ...");
        }
    }
}
