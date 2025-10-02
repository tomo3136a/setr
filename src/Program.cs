using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;
using Tmm;

internal class Program
{
    private const string version = "1.0";
    private static bool b_verbose = false;
    private static bool b_cui = false;
    private bool b_append = false;
    private bool b_update = false;
    private bool b_relative = false;
    private bool b_relative2 = false;

    string cpath;
    string cpath2;
    string ipath;
    string opath;
    string spath;
    string title;
    string title2;
    string msg;
    string cmd;

    List<string> cmds;
    List<string> outs;
    Dictionary<string, string> kvs;


    Program()
    {
        cpath2 = Environment.CurrentDirectory;
        cpath = cpath2;
        ipath = "";
        opath = "";
        spath = "";
        title = "";
        title2 = "";
        msg = "";
        cmd = "";

        cmds = new List<string>();
        outs = new List<string>();
        kvs = new Dictionary<string, string>();
    }

    /// <summary>
    /// main function
    /// {program}.exe {option...} [script...]
    /// </summary>
    /// <param name="args"></param>
    [STAThread]
    private static void Main(string[] args)
    {
#if (NET6_0_OR_GREATER || NETSTANDARD2_0_OR_GREATER)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        var app = new Program();

        // command line
        var ss = ArgsToArray(Environment.CommandLine).Skip(1).ToArray();
        if (app.ParseCommandLine(ss) != 0)
        {
            var s1 = "args:";
            foreach (var s in ss) s1 += " " + s;
            system_println("引数が正しくありません。\n" + s1, 3);
            Environment.ExitCode = -1;
            return;
        }
        app.SetGlobal();

        // input/output file
        var src = app.ipath;
        var dst = app.opath;
        if (dst != "")
        {
            if (File.Exists(dst))
            {
                app.Load(dst);
            }
        }
        if (src == "")
        {
            if (app.Run() < 0)
            {
                Environment.ExitCode = -1;
                return;
            }
        }
        else
        {
            if (!File.Exists(src))
            {
                var msg = Path.GetFileName(src);
                verbose_println(msg + " は見つかりませんでした。", 3);
                Environment.ExitCode = -1;
                return;
            }

            // test update
            if (!app.b_update && File.Exists(dst))
            {
                var dt1 = File.GetLastWriteTime(src);
                var dt2 = File.GetLastWriteTime(dst);
                if (dt1 < dt2) return;
            }

            //run script
            if (!app.RunScript(src))
            {
                var msg = "load script: " + Path.GetFileName(src);
                verbose_println(msg);
                Environment.ExitCode = -1;
                return;
            }
        }

        // save output
        if (app.outs.Count == 0) return;
        app.Save(dst);
    }

    private void SetGlobal()
    {
        if (title == "")
        {
            if (ipath.Length > 0)
            {
                title = Path.GetFileNameWithoutExtension(ipath);
            }
            else
            {
                title = AppName();
            }
        }
        b_relative2 = b_relative;
        cpath2 = cpath;
        title2 = title;
    }

    private void Reset()
    {
        b_relative = b_relative2;
        cpath = cpath2;
        title = title2;
        cmd = "";
        cmds.Clear();
    }

    /// <summary>
    /// Commandline parser
    /// --version       print version
    /// --verbose       verbose mode
    /// --console       gui mode
    /// mode flag:
    /// -v              verbose print mode
    /// -u              update mode
    /// parameter:
    /// -i <path>       set input-file path
    /// -o <path>       set output-file path(default:.tmp/<app>.cmd)
    /// -c <command>    command script
    /// -s <path>       set script path
    /// -t <title>      set title
    /// </summary>
    /// <param name="args">arguments</param>
    /// <returns>error code</returns>
    private int ParseCommandLine(string[] args)
    {
        var opt_flg = false;
        var opt = "";
        var res = 0;
        foreach (var arg in args)
        {
            var s = arg.Trim();
            if (s.Length == 0) continue;
            if (!opt_flg)
            {
                var re = new Regex(@"^(--?|/)(\w+)(?:(-|\+)?|=(.*))?$");
                var m = re.Match(s);
                if (!m.Success)
                {
                    cmds.Add(s);
                    continue;
                }
                opt = m.Groups[2].Value;
                s = m.Groups[4].Value;
                if (m.Groups[1].Value.Length > 1)
                {
                    switch (opt)
                    {
                        case "version": return Cmd_Version();
                        case "verbose": opt = "v"; break;
                        case "console": opt = "C"; break;
                    }
                }
                var b = false;
                switch (opt)
                {
                    case "v": b = !b_verbose; break;
                    case "C": b = !b_cui; break;
                    case "u": b = !b_update; break;
                    case "a": b = !b_append; break;
                    case "r": b = !b_relative; break;
                    case "d": cmd = opt; continue; //delete
                    case "p": cmd = opt; continue; //prompt
                    case "y": cmd = opt; continue; //yesno
                    case "f": cmd = opt; continue; //file select
                    case "g": cmd = opt; continue; //folder select
                    case "l": cmd = opt; continue; //list select
                }
                switch (m.Groups[3].Value)
                {
                    case "+": b = true; break;
                    case "-": b = false; break;
                }
                switch (opt)
                {
                    case "v": b_verbose = b; continue;
                    case "C": b_cui = b; continue;
                    case "u": b_update = b; continue;
                    case "a": b_append = b; continue;
                    case "r": b_relative = b; continue;
                }
                opt_flg = true;
            }
            if (!opt_flg || s.Length == 0) continue;
            s = RemoveQuate(s);
            s = RemoveEscape(s);
            switch (opt)
            {
                case "i": ipath = s; break;
                case "o": opath = s; break;
                case "c": cpath = s; break;
                case "s": spath = s; break;
                case "t": title = s; break;
                case "m": msg += ((msg == "") ? "" : "\n") + s; break;
                default: res = -1; break;
            }
            opt_flg = false;
        }
        return res;
    }

    private static string Token(ref string s, string sep)
    {
        var ret = "";
        var i = s.IndexOf(sep);
        if (i < 0)
        {
            ret = s;
            s = "";
        }
        else
        {
            ret = s.Substring(0, i);
            s = s.Substring(i + sep.Length);
        }
        return ret.ToLower();
    }

    /// <summary>
    /// get application name
    /// </summary>
    /// <returns>application name</returns>
    private static string AppName()
    {
        var s = Assembly.GetExecutingAssembly().Location;
        return Path.GetFileNameWithoutExtension(s);
    }

    /// <summary>
    /// system message print line
    /// </summary>
    /// <param name="s">text</param>
    private static void system_println(string msg, long lv = 0)
    {
        if (b_cui)
        {
            foreach (var line in msg.Split('\n'))
            {
                var s = AppName() + "> " + line;
                switch (lv)
                {
                    case 1: Console.Out.WriteLine(s); break;
                    case 2: Console.Error.WriteLine(s); break;
                    case 3: Console.Error.WriteLine(s); break;
                    default: Console.WriteLine(s); break;
                }
            }
            return;
        }
        var icon = MessageBoxIcon.Question;
        switch (lv)
        {
            case 1: icon = MessageBoxIcon.Information; break;
            case 2: icon = MessageBoxIcon.Warning; break;
            case 3: icon = MessageBoxIcon.Error; break;
            default: icon = MessageBoxIcon.None; break;
        }

        DialogResult res = MessageBox.Show(
            msg, AppName(), MessageBoxButtons.OK, icon);
    }

    /// <summary>
    /// verbose print line
    /// </summary>
    /// <param name="s">text</param>
    private static void verbose_println(string s, long lv = 1)
    {
        if (b_verbose) system_println(s, lv);
    }

    /// <summary>
    /// string to arguments array
    /// </summary>
    /// <param name="s">input string</param>
    /// <returns>arguments array</returns>
    private static string[] ArgsToArray(string s)
    {
        var lst = new List<string>();
        var sb = new StringBuilder();
        var b_space = false;
        var b_quate = false;
        foreach (var c in s.Trim())
        {
            var b = char.IsWhiteSpace(c) | (c == '=') | (c == ',') | (c == ';');
            if (b_space)
            {
                if (b) continue;
                b_space = false;
            }
            if (!b_quate)
            {
                if (b)
                {
                    lst.Add(sb.ToString());
                    sb.Clear();
                    b_space = true;
                    continue;
                }
            }
            sb.Append(c);
            if (c == '"') b_quate = !b_quate;
        }
        if (sb.Length > 0)
        {
            lst.Add(sb.ToString());
        }
        return lst.ToArray();
    }

    /// <summary>
    /// Remove quate character from string
    /// </summary>
    /// <param name="s">target string</param>
    /// <returns></returns>
    private static string RemoveQuate(string s)
    {
        var b_quate = false;
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            if (c == '"' && !b_quate)
            {
                b_quate = true;
                continue;
            }
            b_quate = false;
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static string RemoveEscape(string s)
    {
        return String.Format(s);
    }

    /// <summary>
    /// run script
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private bool RunScript(string path)
    {
        if (!File.Exists(path)) return false;
        var enc = Encoding.GetEncoding(932);
        var lines = File.ReadAllLines(path, enc);
        foreach (var line in lines)
        {
            var s = line.TrimStart();
            Token(ref s, " ");
            s = s.Trim();
            if (s.Length == 0) continue;

            //output comment line
            if (s[0] == '#')
            {
                s = s.Substring(1).Trim();
                outs.Add("rem " + s);
                continue;
            }

            //output set command
            if (s[0] == '*')
            {
                s = s.Substring(1).Trim();
                var args = ArgsToArray(s);
                if (args.Length < 1) continue;
                Reset();
                ParseCommandLine(args.ToArray());
                if (Run() < 0) return false;
                continue;
            }
        }
        return true;
    }

    /// <summary>
    /// run command
    /// </summary>
    /// <returns>error code, 0 is ok</returns>
    private int Run()
    {
        var s = "";
        if (cmds.Count > 0)
        {
            s = cmds[0];
            var i = s.IndexOf('=');
            if (i >= 0)
            {
                cmds.RemoveAt(0);
                if (i > 0)
                {
                    var s2 = s.Substring(0, i).Trim();
                    cmds.Insert(0, s2);
                }
                s = s.Substring(i + 1).Trim();
                if (s.Length > 0)
                {
                    cmds.Insert(1, s);
                }
            }
        }

        var res = 0;
        switch (cmd)
        {
            case "": Cmd_Set(); break;                //set constant
            case "d": res = Cmd_Set(); break;         //set delete
            case "p": res = Cmd_Prompt(); break;      //set input string
            case "y": res = Cmd_YesNo(); break;       //set message
            case "f": res = Cmd_File(); break;        //set file
            case "g": res = Cmd_Folder(); break;      //set folder
            case "l": res = Cmd_List(); break;        //set list
            default: break;
        }
        return res;
    }

    /// <summary>
    /// print version
    /// </summary>
    /// <returns></returns>
    int Cmd_Version()
    {
        Console.WriteLine(version);
        return 0;
    }

    /// <summary>
    /// set string
    /// </summary>
    /// <returns></returns>
    int Cmd_Set()
    {
        if (cmds.Count < 1) return 0;
        string k = cmds[0];
        string v = "";
        if (cmds.Count > 1) v = cmds[1];
        if (cmds.Count > 2)
        {
            for (var i = 2; i < cmds.Count; i++)
            {
                v += " " + cmds[i];
            }
            v = "\"" + v + "\"";
        }
        var s = "set " + k + "=" + v;
        outs.Add(s);
        msg = "";
        return 0;
    }

    /// <summary>
    /// set input string
    /// </summary>
    /// <returns></returns>
    int Cmd_Prompt()
    {
        if (cmds.Count == 0) return -1;
        var s = (cmds.Count > 1) ? cmds[1] : "";
        s = RemoveQuate(s);
        if (s == "")
        {
            if (kvs.ContainsKey(cmds[0]))
            {
                s = kvs[cmds[0]];
            }
        }
        if (b_cui)
        {
            var res = Prompt(ref s);
            if (res != 0) return res;
        }
        else
        {
            Console.WriteLine("msg:" + msg);
            var res = ShowInputBox(ref s, msg, title);
            if (res != DialogResult.OK) return -1;
        }
        s = "set " + cmds[0] + "=" + s;
        outs.Add(s);
        msg = "";
        return 0;
    }

    /// <summary>
    /// message
    /// </summary>
    /// <returns></returns>
    int Cmd_YesNo()
    {
        if (cmds.Count < 1) return -1;
        var v = "1";
        if (kvs.ContainsKey(cmds[0]))
        {
            if (cmds.Count > 1)
            {
                if (kvs[cmds[0]] == cmds[1]) v = "1";
            }
            if (cmds.Count > 2)
            {
                if (kvs[cmds[0]] == cmds[2]) v = "0";
            }
        }
        var s = RemoveQuate(msg);
        if (b_cui)
        {
            if (s == "") s = cmds[0] + " ?";
            s += " [Yes/No/Cancel]";
            while (true)
            {
                Console.WriteLine(s);
                Console.Write(title + "> ");
                var s2 = Console.ReadLine();
                if (s2 == null) return -1;
                s2 = s2.Trim().ToLower();
                if (s2.Length < 1) continue;
                switch (s2[0])
                {
                    case 'y':
                        if (cmds.Count > 1) s = cmds[1];
                        else s = "1";
                        break;
                    case 'n':
                        s = "";
                        if (cmds.Count > 2) s = cmds[2];
                        else if (cmds.Count < 2) s = "0";
                        break;
                    case 'c':
                        s = "";
                        return -2;
                }
            }
        }
        else
        {
            if (s == "") s = cmds[0] + " ?";
            var btn = MessageBoxDefaultButton.Button1;
            if (v == "0") btn = MessageBoxDefaultButton.Button2;
            DialogResult res = MessageBox.Show(
                s, title, MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question, btn);
            s = "";
            if (res == DialogResult.Yes)
            {
                if (cmds.Count > 1) s = cmds[1];
                else s = "1";
            }
            else if (res == DialogResult.No)
            {
                if (cmds.Count > 2) s = cmds[2];
                else s = "0";
            }
            else return -1;
        }
        s = "set " + cmds[0] + "=" + s;
        outs.Add(s);
        msg = "";
        return 0;
    }

    /// <summary>
    /// set path
    /// </summary>
    /// <returns></returns>
    int Cmd_File()
    {
        if (cmds.Count < 1) return -1;
        var s = cpath;
        if (kvs.ContainsKey(cmds[0]))
        {
            s = kvs[cmds[0]];
        }
        if (b_cui)
        {
            var res = Prompt(ref s);
            if (res != 0) return -1;
        }
        else
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (msg != "") dlg.Title = msg;
            var k = "";
            var flt = "";
            foreach (var v in cmds.Skip(1))
            {
                if (k == "")
                {
                    k = v;
                    continue;
                }
                flt += k + "|" + v + "|";
                k = "";
            }
            if (flt != "")
            {
                flt += "すべてのファイル|*.*";
                dlg.Filter = flt;
            }
            if (k != "") s = k;
            s = Path.GetFullPath(s);
            if (Directory.Exists(s))
            {
                dlg.InitialDirectory = s;
                s = "";
            }
            else if (File.Exists(s))
            {
                dlg.InitialDirectory = Path.GetDirectoryName(s);
                s = Path.GetFileName(s);
                dlg.FileName = s;
            }
            else
            {
                s = Path.GetFileName(s);
                dlg.FileName = s;
            }

            if (s != "")
            {

                s = Path.GetExtension(s);
                if (s != "")
                {
                    var i = flt.IndexOf("*" + s);
                    if (i >= 0)
                    {
                        s = flt.Substring(0, i);
                        i = s.Split('|').Count() / 2;
                        dlg.FilterIndex = i;
                    }
                }
                s = "";
            }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                s = dlg.FileName;
            }
            dlg.Dispose();
            if (s == "")
            {
                return -1;
            }
        }
        if (b_relative) s = GetRelativePath(s, cpath);
        s = "set " + cmds[0] + "=" + s;
        outs.Add(s);
        title = "";
        msg = "";
        return 0;
    }

    /// <summary>
    /// set folder
    /// </summary>
    /// <returns></returns>
    int Cmd_Folder()
    {
        if (cmds.Count < 1) return -1;
        var s = cpath;
        if (kvs.ContainsKey(cmds[0]))
        {
            s = kvs[cmds[0]];
        }
        if (b_cui)
        {
            var res = Prompt(ref s);
            if (res != 0) return -1;
        }
        else
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.ShowNewFolderButton = true;
            if (msg != "") dlg.Description = msg;
            if (cmds.Count > 1) s = cmds[1];
            dlg.SelectedPath = s;
            s = "";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                s = dlg.SelectedPath;
            }
            dlg.Dispose();
            if (s == "")
            {
                return -1;
            }
        }
        if (b_relative) s = GetRelativePath(s, cpath);
        s = "set " + cmds[0] + "=" + s;
        outs.Add(s);
        msg = "";
        return 0;
    }

    /// <summary>
    /// set datalist
    /// </summary>
    /// <returns></returns>
    int Cmd_List()
    {
        if (cmds.Count < 1) return -1;
        var s = cpath;
        if (kvs.ContainsKey(cmds[0]))
        {
            s = kvs[cmds[0]];
        }
        if (b_cui)
        {
            //not support
            return -1;
        }
        else
        {
            //if (cmds.Count > 1) s = cmds[1];
            var dlg = new Tmm.UI.InputDialog(msg, title, true);
            //dlg.Text1 = " ";
            //dlg.Text2 = " ";
            try
            {
                foreach (var src in cmds.Skip(1))
                {
                    try
                    {
                        if (File.Exists(src))
                        {
                            var lines = File.ReadAllLines(src);
                            foreach (var line in lines)
                            {
                                dlg.AddListItem(line);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("operation error.\n" + ex.Message,
                            title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                dlg.Value = s;
                s = "";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    s = dlg.Value;
                }
            }
            catch
            {
                MessageBox.Show("operation error. TaggingDialog",
                    title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            dlg.Dispose();
            if (s == "")
            {
                return -1;
            }
            s = "set " + cmds[0] + "=" + s;
            outs.Add(s);
            msg = "";
            return 0;
        }
    }

    /// <summary>
    /// input box
    /// </summary>
    /// <param name="s"></param>
    /// <param name="msg"></param>
    /// <param name="title"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="ptn"></param>
    /// <returns></returns>
    private static DialogResult ShowInputBox(
        ref string s, string msg, string title = "Title",
        int w = 300, int h = 145, string ptn = "")
    {
        var sz = new Size(w, h);
        var m = 10;

        Form dlg = new Form();
        dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
        //dlg.AutoScaleDimensions = new SizeF(6F, 13F);
        //dlg.AutoScaleMode = AutoScaleMode.Font;
        dlg.ClientSize = sz;
        dlg.Text = title;
        dlg.MinimumSize = new Size(w, h);
        dlg.MinimizeBox = false;
        dlg.MaximizeBox = false;
        dlg.ShowIcon = false;

        Label lbl = new Label();
        lbl.Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
        lbl.Text = msg;
        lbl.Location = new Point(m, m);
        lbl.Width = sz.Width - 2 * m;
        lbl.Height = 3 * 20;
        dlg.Controls.Add(lbl);

        TextBox txt = new TextBox();
        txt.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
        txt.BorderStyle = BorderStyle.FixedSingle;
        txt.Size = new Size(sz.Width - 2 * m, 23);
        txt.Location = new Point(m, lbl.Location.Y + lbl.Height + m);
        txt.Text = s;
        dlg.Controls.Add(txt);

        Button ok = new Button();
        ok.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Right);
        ok.DialogResult = DialogResult.OK;
        ok.Name = "ok";
        ok.Size = new Size(75, 23);
        ok.Text = "&OK";
        ok.Location = new Point(sz.Width - 80 - 75 - m, sz.Height - 4 * m);
        dlg.Controls.Add(ok);

        Button cancel = new Button();
        cancel.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Right);
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Name = "cancel";
        cancel.Size = new Size(75, 23);
        cancel.Text = "&Cancel";
        cancel.Location = new Point(sz.Width - 75 - m, sz.Height - 4 * m);
        dlg.Controls.Add(cancel);

        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;

        DialogResult res = dlg.ShowDialog();
        while (true)
        {
            if (res != DialogResult.OK) return res;
            if (ptn == "") break;
            if (Regex.Match(txt.Text, ptn).Success) break;
            res = dlg.ShowDialog();
        }
        s = txt.Text;
        return res;
    }

    /// <summary>
    /// console prompt
    /// </summary>
    /// <param name="s"></param>
    /// <returns>error code</returns>
    private int Prompt(ref string s)
    {
        var s2 = (s.Length > 0) ? " [" + s + "]" : "";
        s2 = (msg + s2).Trim();
        if (s2.Length > 0)
        {
            Console.WriteLine(s2);
        }
        Console.Write(title + "> ");
        s2 = Console.ReadLine();
        if (s2 == null) return -2;
        s2 = s2.Trim();
        if (s2.Length > 0)
        {
            s = s2;
        }
        return 0;
    }

    /// <summary>
    /// get relative path
    /// </summary>
    /// <param name="path"></param>
    /// <param name="root"></param>
    /// <returns>path</returns>
    private static string GetRelativePath(string path, string root)
    {
        var ps = Path.GetFullPath(path.Replace("/", "\\")).Split('\\');
        var rs = Path.GetFullPath(root.Replace("/", "\\")).Split('\\');
        var n = ps.Count();
        if (n > rs.Count()) n = rs.Count();
        if (n == 0) return path;
        if (ps[0] != rs[0]) return path;
        var i = 0;
        for (; i < n; i++)
        {
            if (ps[i] != rs[i]) break;
        }
        var res = "";
        for (var j = i; j < ps.Count(); j++)
        {
            res = res + "\\" + ps[j];
        }
        if (i < rs.Count())
        {
            for (; i < rs.Count() - 1; i++)
            {
                res = "\\.." + res;
            }
            res = "." + res;
        }
        res = "." + res;
        return res;
    }

    void Load(string path)
    {
        var enc = Encoding.GetEncoding(932);
        foreach (var line in File.ReadLines(path))
        {
            var ss = line.Split(new char[] { ' ' }, 2);
            if (ss.Length != 2) continue;
            ss = ss[1].Split(new char[] { '=' }, 2);
            if (ss.Length != 2) continue;
            var k = ss[0];
            var v = ss[1];
            if (kvs.ContainsKey(k)) kvs.Remove(k);
            kvs.Add(k, v);
        }
    }

    /// <summary>
    /// save to output path
    /// </summary>
    /// <param name="path"></param>
    void Save(string path)
    {
        if (path == "")
        {
            foreach (var line in outs)
            {
                Console.WriteLine(line);
            }
            return;
        }
        if (!b_append)
        {
            if (File.Exists(path)) File.Delete(path);
        }
        var enc = Encoding.GetEncoding(932);
        foreach (var line in outs)
        {
            var s = line + Environment.NewLine;
            File.AppendAllText(path, s, enc);
        }
    }
}
