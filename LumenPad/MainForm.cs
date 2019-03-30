﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

using FastColoredTextBoxNS;

namespace Lumen.Studio {
    public partial class MainForm : LumenStudioForm {
        /// <summary> Singleton realization  </summary>
        public static MainForm Instance { get; private set; }

        private static TaskFactory Factory { get; } = new TaskFactory();

        public static TextBoxManager MainTextBoxManager { get; set; }

        public static Project Project { get; set; }
        public static String CurrentFile { get; set; }

        public String err;
        public Place? b;
        public Place? e;

        public static Boolean AllowRegiStringationChangas = true;

        public MainForm(String[] args) : base() {
            this.InitializeComponent();

            Instance = this;

            MainTextBoxManager = new TextBoxManager(this.textBox) {
                Language = Settings.Languages[0]
            };

            this.splitContainer2.SplitterWidth = 1;

            ConsoleWriter.Instance = new ConsoleWriter(this.output);
            ConsoleReader.Instance = new ConsoleReader(this.output);

            Console.SetOut(ConsoleWriter.Instance);
            Console.SetIn(ConsoleReader.Instance);

            this.ApplyColorScheme();
            this.ColorizeTopMenu();
            this.ColorizeBottomMenu();

            if (Settings.LastOpenedProject != null) {
                this.OpenProject(Settings.LastOpenedProject);
            }

            if (args.Length > 0) {
                if (File.Exists(args[0])) {
                    this.OpenFile(args[0]);
                } else {
                    this.OpenProject(args[0]);
                }
            }
        }

        #region ApplyTheme
        internal void ApplyColorScheme() {
            this.output.Font = new Font("Consolas", 9f);

            this.textBox.BackColor = Settings.BackgroundColor;
            this.textBox.ForeColor = Settings.ForegroundColor;
            this.textBox.IndentBackColor = Settings.BackgroundColor;
            this.textBox.LineNumberColor = Settings.ForegroundColor;
            this.textBox.PaddingBackColor = Settings.BackgroundColor;
            this.textBox.TextAreaBorderColor = Settings.BackgroundColor;
            this.textBox.ServiceLinesColor = Settings.BackgroundColor;
            this.treeView1.BackColor = Settings.BackgroundColor;
            this.treeView1.ForeColor = Settings.ForegroundColor;

            this.topMenu.BackColor = Settings.BackgroundColor;
            this.BackColor = Settings.LinesColor;
            this.output.BackColor = Settings.BackgroundColor;
            this.output.ForeColor = Settings.ForegroundColor;
            this.splitContainer1.BackColor = Settings.LinesColor;
            this.splitContainer2.BackColor = Settings.LinesColor;

            this.bottomMenu.BackColor = Settings.BackgroundColor;
            this.bottomMenu.ForeColor = Settings.ForegroundColor;
        }

        internal void HighlightUnsavedFile() {
            TreeNode[] nodes = this.treeView1.Nodes.Find(CurrentFile, true);

            if (nodes.Length >= 1) {
                TreeNode node = nodes[0];
                node.ForeColor = Color.DarkRed;
            }
        }

        private void ColorizeTopMenu() {
            foreach (Object i in this.topMenu.Items) {
                if (i is ToolStripMenuItem item) {
                    this.ColorizeMenuItem(item);
                }
            }
        }

        private void ColorizeBottomMenu() {
            foreach (Object i in this.bottomMenu.Items) {
                if (i is ToolStripMenuItem item) {
                    this.ColorizeMenuItem(item);
                }
            }
        }

        private void ColorizeMenuItem(ToolStripMenuItem item) {
            item.BackColor = Settings.BackgroundColor;
            item.ForeColor = Settings.ForegroundColor;

            if (item.HasDropDownItems) {
                foreach (Object i in item.DropDownItems) {
                    if (i is ToolStripMenuItem mi) {
                        this.ColorizeMenuItem(mi);
                    }
                }
            }
        }
        #endregion

        private void Run(Object sender, EventArgs e) {
            this.output.Clear();
            // this.errorTable.Rows.Clear();

            if (MainTextBoxManager.Language != null) {
                if (MainTextBoxManager.Language.RunCommand != null) {
                    this.SavePreviousFile();
                    this.StartProcess(MainTextBoxManager.Language.RunCommand.Replace("[FILE]", CurrentFile));
                } else {
                    this.SavePreviousFile();
                    Task.Factory.StartNew(() => Light.Interpriter.Start(CurrentFile, new Lang.Scope()));
                }
            }
        }

        private void StartProcess(String name) {
            Tuple<String, String> nameAndArgs = this.GetNameAndArgs(name);

            Process proc = new Process() {
                StartInfo = new ProcessStartInfo {
                    FileName = nameAndArgs.Item1,
                    Arguments = nameAndArgs.Item2
                }
            };

            Factory.StartNew(() => {
                proc.Start();
                proc.WaitForExit();
            });
        }

        private Tuple<String, String> GetNameAndArgs(String command) {
            String[] src = command.Split(' ');

            StringBuilder builder = new StringBuilder();
            for (Int32 i = 1; i < src.Length; i++) {
                builder.Append(src[i]).Append(" ");
            }

            return new Tuple<String, String>(src[0], builder.ToString());
        }

        internal void RaiseError(IError result) {
            if (result.ErrorLine > 0) {
                Range range = this.textBox.GetLine(result.ErrorLine - 1);

                this.err = result.ErrorMessage;

                if (result.ErrorCharEnd == -1) {
                    this.b = new Place(result.ErrorCharBegin, result.ErrorLine - 1);
                    this.e = new Place(range.Length, result.ErrorLine - 1);
                    range = this.textBox.GetRange(this.b.Value, this.e.Value);
                } else {
                    this.b = new Place(result.ErrorCharBegin, result.ErrorLine - 1);
                    this.e = new Place(result.ErrorCharEnd, result.ErrorLine - 1);
                    range = this.textBox.GetRange(this.b.Value, this.e.Value);
                }

                range.SetStyle(Settings.Error);

                //this.errorTable.Rows.Add(result.ErrorType, result.ErrorMessage);
            }
        }

        private void TextBox_TextChanged(Object sender, TextChangedEventArgs e) {
            this.err = null;
            this.b = null;
            // this.errorTable.Rows.Clear();

            MainTextBoxManager?.OnTextChanged(e);
        }

        private void Form1_Load(Object sender, EventArgs e) {
            foreach (Language i in Settings.Languages) {
                if (i.Actor != null && File.Exists(i.Actor)) {
                    Lang.Scope s = new Lang.Scope();
                    s.AddUsing(Lang.Prelude.Instance);
                  /*  Interop.Interop op = new Interop.Interop();

                    s.Set("studio", Interop.Interop.Module);

                    s.Set(i.Name, new Lang.LambdaFun(null));*/

                    Light.Interpriter.Start(i.Actor, s);

                    i.Fn = s.Get(i.Name) as Lang.Module;
                }
            }

            if (CurrentFile == null) {
                if (!File.Exists("default.lm")) {
                    File.Create("default.lm").Close();
                }

                this.OpenFile("default.lm");
            }
        }

        #region File System

        private void OpenFile(String path) {
            if (File.Exists(path)) {
                if (path.EndsWith(".png")) {
                    this.OpenImage(path);
                    return;
                }

                this.SavePreviousFile();

                this.ProcessExtenstion(Path.GetExtension(path));
                AllowRegiStringationChangas = false;

                MainTextBoxManager.TextBox.Text = File.ReadAllText(path);

                AllowRegiStringationChangas = true;

                CurrentFile = path;

                if (Project != null) {
                    this.Text = path.Replace(Project.Path + "\\", "") + " - Lumen Studio";
                }
            }
        }

        private void FileOpenWithoutSave(String path) {
            if (File.Exists(path)) {
                if (path.EndsWith(".png")) {
                    this.OpenImage(path);
                    return;
                }

                this.ProcessExtenstion(Path.GetExtension(path));

                AllowRegiStringationChangas = false;

                MainTextBoxManager.TextBox.Text = File.ReadAllText(path);

                AllowRegiStringationChangas = true;

                CurrentFile = path;

                this.Text = path.Replace(Project.Path + "\\", "") + " - Lumen Studio";
            }
        }

        private void ProcessExtenstion(String extension) {
            foreach (Language language in Settings.Languages) {
                if (language.Extensions.Contains(extension)) {
                    this.CustomizeForLanguage(language);
                    return;
                }
            }
        }

        private void CustomizeForLanguage(Language language) {
            MainTextBoxManager.Language = language;
        }

        private void SavePreviousFile() {
            if (CurrentFile != null) {
                File.WriteAllText(CurrentFile, MainTextBoxManager.TextBox.Text);
                MainTextBoxManager.ChangesSaved = true;
                TreeNode[] nodes = this.treeView1.Nodes.Find(CurrentFile, true);

                if (nodes.Length == 1) {
                    TreeNode node = nodes[0];
                    node.ForeColor = Settings.ForegroundColor;
                }
            }
        }

        public void DeleteRecursive(String dir) {
            foreach (String d in Directory.EnumerateDirectories(dir)) {
                this.DeleteRecursive(d);
            }

            foreach (String f in Directory.EnumerateFiles(dir)) {
                File.Delete(f);
            }

            Directory.Delete(dir);
        }

        public void FileWriteWithCreating(String path) {
            String[] s = path.Split('\\');

            for (Int32 i = 1; i < s.Length - 1; i++) {
                String p = "";
                for (Int32 j = 0; j <= i; j++) {
                    p += s[j] + "\\";
                }

                if (!Directory.Exists(p)) {
                    Directory.CreateDirectory(p);
                }
            }

            File.WriteAllText(path, MainTextBoxManager.TextBox.Text);
        }

        #endregion

        private void OpenImage(String path) {
            this.textBox.Visible = false;

            PictureBox z = new PictureBox {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ImageLocation = path,
                WaitOnLoad = false
            };

            this.splitContainer2.Panel1.Controls.Add(z);
        }

        private void TextBox_ToolTipNeeded(Object sender, ToolTipNeededEventArgs e) {
            if (this.b.HasValue) {
                if (e.Place.iLine == this.b.Value.iLine) {
                    if (e.Place.iChar >= this.b.Value.iChar && e.Place.iChar <= this.e.Value.iChar) {
                        e.ToolTipTitle = this.err;
                        e.ToolTipText = "     ";
                    }
                    return;
                }
            }

            /*foreach(KeyValuePair<(System.Int32 begin, System.Int32 end, System.Int32 line), IEntity> i in ScopeMetadata.symboltable) {
				if (e.Place.iLine + 1 == i.Key.line) {
					if (e.Place.iChar >= i.Key.begin && e.Place.iChar <= i.Key.end) {
						if(i.Value is FunctionMetadata fm) {
							e.ToolTipTitle = BuildFunctionHeader(fm);
						}
						else if (i.Value is VariableMetadata vm) {
							e.ToolTipTitle = Stringify(vm);
						} else if (i.Value is LiteralMetadata lm) {
							e.ToolTipTitle = lm.v;
						}

						e.ToolTipText = "     ";
						return;
					}
				}
			}*/
        }

        private void OpenToolStringipMenuItem_Click(Object sender, EventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK) {
                this.OpenFile(dialog.FileName);
            }
        }

        private void SaveToolStringipMenuItem_Click(Object sender, EventArgs e) {
            this.SavePreviousFile();
        }

        private void MainForm_FormClosed(Object sender, FormClosedEventArgs e) {
            if (Directory.Exists(".tmp")) {
                this.DeleteRecursive(".tmp");
            }

            if (Project != null && Project.Path != null) {
                XmlNode lastData = Settings.MainSettings.DocumentElement["LastData"];
                if (lastData != null) {
                    lastData.Attributes["project"].Value = Project.Path;
                } else {
                    XmlAttribute attribute = Settings.MainSettings.CreateAttribute("project");
                    attribute.Value = Project.Path;
                    lastData = Settings.MainSettings.CreateElement("LastData");
                    lastData.Attributes.SetNamedItem(attribute);
                    Settings.MainSettings.DocumentElement.AppendChild(lastData);
                }
                Settings.MainSettings.Save("settings\\main.xml");
            }

            Application.Exit();
        }

        private void BottomPanelHideAll() {
            foreach (Control i in this.bottomPanel.Controls) {
                i.Visible = false;
            }
        }

        #region Project Manager 

        private void ProjectToolStringipMenuItem_Click(Object sender, EventArgs e) {
            FolderBrowserDialog ofd = new FolderBrowserDialog();

            if (ofd.ShowDialog() == DialogResult.OK) {
                this.OpenProject(ofd.SelectedPath);
            }
        }

        private void OpenProject(String selectedPath) {
            Project = new Project {
                Name = this.GetName(selectedPath),
                Path = selectedPath
            };

            this.Fill(this.treeView1.Nodes.Add(Project.Name), Path.GetFullPath(selectedPath));
        }

        private void Fill(TreeNode node, String path) {
            foreach (String i in Directory.EnumerateDirectories(path)) {
                TreeNode n = node.Nodes.Add(i, i.Replace(path + "\\", ""), 0);
                this.Fill(n, i);
            }

            foreach (String i in Directory.EnumerateFiles(path)) {
                node.Nodes.Add(i, i.Replace(path + "\\", ""), this.GetImageIndex(i));
            }
        }

        private Int32 GetImageIndex(String i) {
            if (i.EndsWith(".html")) {
                return 2;
            }

            if (i.EndsWith(".py")) {
                return 3;
            }

            if (i.EndsWith(".lm")) {
                return 4;
            }

            if (i.EndsWith(".css")) {
                return 5;
            }
            if (i.EndsWith(".png")) {
                return 6;
            }


            /*
			if (i.EndsWith(".txt")) {
				return 4;
			}

			if (i.EndsWith(".cs")) {
				return 2;
			}
			*/
            return 1;
        }

        private String GetName(String path) {
            String[] cons = path.Split('\\');

            return cons[cons.Length - 1];
        }

        private void TreeView1_NodeMouseDoubleClick(Object sender, TreeNodeMouseClickEventArgs e) {
            if (Project == null) {
                return;
            }

            if (!Directory.Exists(".tmp")) {
                Directory.CreateDirectory(".tmp");
            }

            if (CurrentFile != null) {
                if (!MainTextBoxManager.ChangesSaved) {
                    this.FileWriteWithCreating(".tmp" + CurrentFile.Replace(Project.Path, ""));
                }
            }

            String path = e.Node.Name;

            if (File.Exists(".tmp\\" + path.Replace(Project.Path, ""))) {
                this.FileOpenWithoutSave(".tmp\\" + path.Replace(Project.Path, ""));
                CurrentFile = path;
            } else {
                this.FileOpenWithoutSave(path);
            }
        }

        #endregion

        private void TextBox_KeyDown(Object sender, KeyEventArgs e) {
            if (e.Control && e.KeyCode == Keys.S) {
                this.SavePreviousFile();
            }
        }

        private void CreateProjectClick(Object sender, EventArgs e) {
            new CreateProjectWindow().ShowDialog();
        }

        private void Panel3_Click(Object sender, EventArgs e) {
            Application.Exit();
        }

        private void Panel4_Click(Object sender, EventArgs e) {
            if (this.WindowState == FormWindowState.Normal) {
                this.WindowState = FormWindowState.Maximized;
            } else {
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void TextBox_MouseDoubleClick(Object sender, MouseEventArgs e) {
            if (e.X < this.textBox.LeftIndent) {
                Place place = this.textBox.PointToPlace(e.Location);
                if (e.Button == MouseButtons.Right) {
                    if (this.textBox.Bookmarks.Contains(place.iLine)) {
                        /*KlischeeFreeKlischeeStduio.KSDocSettings window = new KlischeeFreeKlischeeStduio.KSDocSettings();
                        window.ShowDialog();
                        Bookmark bookmark = this.textBox.Bookmarks.GetBookmark(place.iLine);
                        bookmark.Condition = window.condition;
                        bookmark.Action = window.action;*/
                    }
                    return;
                }

                if (this.textBox.Bookmarks.Contains(place.iLine)) {
                    this.textBox.Bookmarks.Remove(place.iLine);
                    this.textBox.GetLine(place.iLine).ClearStyle(Settings.UnactiveBreakpoint, Settings.Type, Settings.String, Settings.Keyword);
                    MainTextBoxManager.TextBox.OnTextChanged(this.textBox.Range);
                } else {
                    this.textBox.Bookmarks.Add(new Bookmark(MainTextBoxManager.TextBox, "", place.iLine, Settings.TextBoxBookMarkColor));
                    this.textBox.GetLine(place.iLine).ClearStyle(Settings.UnactiveBreakpoint, Settings.Type, Settings.String, Settings.Keyword);
                    this.textBox.GetLine(place.iLine).SetStyle(Settings.UnactiveBreakpoint);
                }
            }
        }

        private void OutputToolStripMenuItem_Click(Object sender, EventArgs e) {
            this.BottomPanelHideAll();
            this.output.Visible = true;
        }

        private void ShowInteractive(Object sender, EventArgs e) {
            this.BottomPanelHideAll();
            if(this.interactive == null) {
                this.IntializeInteractive();
            }
            this.interactive.Visible = true;
        }

        TextBoxManager tbmng;
        ConsoleEmulator interactive;
        private void IntializeInteractive() {
            this.interactive = new ConsoleEmulator();
            this.bottomPanel.Controls.Add(this.interactive);
            this.interactive.Dock = DockStyle.Fill;
            this.interactive.Font = new Font("Consolas", 9f);
            this.interactive.ShowLineNumbers = false;
            this.interactive.BackColor = Settings.BackgroundColor;
            this.interactive.ForeColor = Settings.ForegroundColor;

            this.tbmng = new TextBoxManager(this.interactive) {
                Language = Settings.Languages[0]
            };

            this.interactive.TextChanged += (sender, e) => {
                Settings.Languages[0].OnTextChanged(this.tbmng, e.ChangedRange);
            };

            Task.Factory.StartNew(() => {
                Lang.Scope mainScope = new Lang.Scope();

                ConsoleWriter localWriter = new ConsoleWriter(this.interactive);
                ConsoleReader localReader = new ConsoleReader(this.interactive);

                while (true) {
                    this.interactive.Write(">>> ");
                    String command = this.interactive.ReadLine();
                    while (!command.TrimEnd().EndsWith(";;")) {
                        this.interactive.Write("... ");
                        command += Environment.NewLine + this.interactive.ReadLine();
                    }

                    Console.SetIn(localReader);
                    Console.SetOut(localWriter);
                    Lang.Value result = Light.Interpriter.Eval(command.TrimEnd(new System.Char[] { ' ', '\t', '\r', '\n', ';' }), "interactive", mainScope);
                    Console.WriteLine($"//-> {result} :: {result.Type}");
                    Console.SetIn(ConsoleReader.Instance);
                    Console.SetOut(ConsoleWriter.Instance);
                }
            });
        }
    }
}