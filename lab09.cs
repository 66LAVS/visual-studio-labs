using System;
using System.IO;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Windows.Forms;

// Без аргументов: файловый менеджер. web [достижения.txt]: сайт.
// dh: обмен с другим участником. dh-demo: демонстрация обоих участников.
class Lab09
{
    // Малые параметры предназначены для учебной демонстрации.
    const int Prime = 7919;
    const int Generator = 7;

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            if (args.Length == 0) BrowseFiles();
            else if (args[0] == "web") RunWeb(args.Length > 1 ? args[1] : null);
            else if (args[0] == "dh") Exchange();
            else if (args[0] == "dh-demo") Demonstrate();
            else { Console.Error.WriteLine("Режимы: web [достижения.txt], dh, dh-demo."); Environment.ExitCode = 1; }
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); Environment.ExitCode = 1; }
    }

    static void BrowseFiles()
    {
        Application.EnableVisualStyles();
        var form = new Form { Text = "Лабораторная №9 — диски, каталоги и файлы", Width = 900, Height = 570 };
        var area = new SplitContainer { Dock = DockStyle.Fill, Width = 880, SplitterDistance = 340 };
        var entries = new ListBox { Dock = DockStyle.Fill };
        var preview = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, ReadOnly = true, WordWrap = false };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 70 };
        var drives = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 70 };
        var path = new TextBox { Width = 385 };
        area.Panel1.Controls.Add(entries); area.Panel2.Controls.Add(preview);
        form.Controls.Add(area); form.Controls.Add(bar);
        string current = null;
        Action<string> open = folder => {
            string absolute = Path.GetFullPath(folder);
            var dirs = Directory.GetDirectories(absolute);
            var files = Directory.GetFiles(absolute);
            Array.Sort(dirs, StringComparer.CurrentCultureIgnoreCase);
            Array.Sort(files, StringComparer.CurrentCultureIgnoreCase);
            current = absolute; path.Text = current; preview.Clear(); entries.Items.Clear();
            foreach (string d in dirs) entries.Items.Add(new Item(d, true));
            foreach (string f in files) entries.Items.Add(new Item(f, false));
        };
        bar.Controls.Add(drives); bar.Controls.Add(path);
        AddButton(bar, "Перейти", () => open(path.Text));
        AddButton(bar, "Вверх", () => {
            if (current == null) return;
            var parent = Directory.GetParent(current);
            if (parent != null) open(parent.FullName);
        });
        AddButton(bar, "Копировать файл", () => {
            var item = entries.SelectedItem as Item;
            if (item == null || item.Directory) throw new InvalidOperationException("Выберите файл.");
            using (var save = new SaveFileDialog { FileName = Path.GetFileName(item.Path), OverwritePrompt = true })
                if (save.ShowDialog() == DialogResult.OK) File.Copy(item.Path, save.FileName, true);
        });
        foreach (var drive in DriveInfo.GetDrives()) drives.Items.Add(drive.Name);
        drives.SelectedIndexChanged += (s, e) => Guard(() => open(drives.SelectedItem.ToString()));
        entries.DoubleClick += (s, e) => Guard(() => {
            var item = entries.SelectedItem as Item;
            if (item == null) return;
            if (item.Directory) open(item.Path);
            else preview.Text = ReadPreview(item.Path);
        });
        Guard(() => open(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
        Application.Run(form);
    }

    class Item
    {
        public readonly string Path;
        public readonly bool Directory;
        public Item(string path, bool directory) { Path = path; Directory = directory; }
        public override string ToString() { return (Directory ? "[Папка] " : "") + System.IO.Path.GetFileName(Path); }
    }

    static string ReadPreview(string path)
    {
        var data = new byte[32768];
        int count = 0;
        bool truncated;
        using (var file = File.OpenRead(path))
        {
            while (count < data.Length)
            {
                int read = file.Read(data, count, data.Length - count);
                if (read == 0) break;
                count += read;
            }
            truncated = file.ReadByte() != -1;
        }
        string text;
        using (var stream = new MemoryStream(data, 0, count))
        using (var reader = new StreamReader(stream, new UTF8Encoding(false, !truncated), true))
        {
            try { text = reader.ReadToEnd(); }
            catch (DecoderFallbackException) { text = null; }
        }
        if (text == null || text.IndexOf('\0') >= 0)
            text = "Двоичный файл. Первые " + Math.Min(count, 128) + " байт:" + Environment.NewLine
                + BitConverter.ToString(data, 0, Math.Min(count, 128));
        else if (truncated) text += Environment.NewLine + "[Показаны первые 32 КиБ]";
        return text;
    }

    static void AddButton(FlowLayoutPanel panel, string label, Action action)
    {
        var button = new Button { Text = label, AutoSize = true };
        button.Click += (s, e) => Guard(action);
        panel.Controls.Add(button);
    }

    static void Guard(Action action)
    {
        try { action(); }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    static void RunWeb(string achievementsFile)
    {
        string achievements = achievementsFile == null ? null : File.ReadAllText(achievementsFile, Encoding.UTF8);
        using (var server = new HttpListener())
        {
            server.Prefixes.Add("http://localhost:8089/");
            server.Start();
            Console.WriteLine("Мои заслуги — http://localhost:8089/. Ctrl+C для остановки.");
            while (true)
            {
                var context = server.GetContext();
                try
                {
                    string html;
                    if (context.Request.HttpMethod != "GET")
                    { context.Response.StatusCode = 405; html = "<h1>Метод не поддерживается</h1>"; }
                    else if (context.Request.Url.AbsolutePath == "/")
                        html = "<h1>Мои заслуги</h1><p>Сайт Рамиля о дипломах и достижениях.</p><a href='/achievements'>Дипломы и пояснения</a>";
                    else if (context.Request.Url.AbsolutePath == "/achievements")
                        html = "<h1>Дипломы и пояснения</h1>" + AchievementList(achievements) + "<p><a href='/'>На главную</a></p>";
                    else { context.Response.StatusCode = 404; html = "<h1>Страница не найдена</h1><a href='/'>На главную</a>"; }
                    context.Response.ContentType = "text/html; charset=utf-8";
                    byte[] bytes = Encoding.UTF8.GetBytes("<!doctype html><html lang='ru'><meta charset='utf-8'><title>Мои заслуги</title>" + html + "</html>");
                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                }
                catch (IOException) { }
                catch (HttpListenerException) { }
                finally { context.Response.Close(); }
            }
        }
    }

    static string AchievementList(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "<p>Для заполнения запустите программу с UTF-8 файлом: lab09.exe web achievements.txt.</p>" +
                "<p>Одна запись на строку: название диплома | год | пояснение.</p>";
        var html = new StringBuilder("<ul>");
        using (var reader = new StringReader(text))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] fields = line.Split(new[] { '|' }, 3);
                html.Append("<li><strong>").Append(HttpUtility.HtmlEncode(fields[0].Trim())).Append("</strong>");
                for (int i = 1; i < fields.Length; i++)
                    html.Append(" — ").Append(HttpUtility.HtmlEncode(fields[i].Trim()));
                html.Append("</li>");
            }
        }
        return html.Append("</ul>").ToString();
    }

    static BigInteger PrivateExponent()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[2];
            int value;
            do { rng.GetBytes(bytes); value = BitConverter.ToUInt16(bytes, 0) & 8191; }
            while (value < 2 || value > Prime - 2);
            return value;
        }
    }

    static BigInteger PublicValue(BigInteger privateExponent) { return BigInteger.ModPow(Generator, privateExponent, Prime); }
    static BigInteger SharedValue(BigInteger peer, BigInteger own) { return BigInteger.ModPow(peer, own, Prime); }

    static void Exchange()
    {
        BigInteger own = PrivateExponent();
        Console.WriteLine("Учебные параметры: p={0}, g={1}", Prime, Generator);
        Console.WriteLine("Передайте одногруппнику публичное число {0}.", PublicValue(own));
        while (true)
        {
            Console.Write("Введите публичное число одногруппника: ");
            string text = Console.ReadLine();
            if (text == null) return;
            BigInteger peer;
            if (BigInteger.TryParse(text, out peer) && peer > 0 && peer < Prime)
            {
                Console.WriteLine("Общий учебный ключ: " + SharedValue(peer, own));
                return;
            }
            Console.WriteLine("Введите целое число от 1 до " + (Prime - 1) + ".");
        }
    }

    static void Demonstrate()
    {
        BigInteger a = PrivateExponent(), b = PrivateExponent();
        BigInteger openA = PublicValue(a), openB = PublicValue(b);
        BigInteger kA = SharedValue(openB, a), kB = SharedValue(openA, b);
        Console.WriteLine("p={0}, g={1}; A={2}, B={3}", Prime, Generator, openA, openB);
        Console.WriteLine("Ключ Алисы: {0}; ключ Боба: {1}; совпадают: {2}", kA, kB, kA == kB);
        Environment.ExitCode = kA == kB ? 0 : 1;
    }
}
