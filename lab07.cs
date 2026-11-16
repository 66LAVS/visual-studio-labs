using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Web;
using System.Xml;
using System.IO.Compression;

// Запуск: lab07.exe [web|init]. Требуется SQL Server или LocalDB.
// LAB07_SQL задаёт строку подключения к серверу. По умолчанию — (localdb)\MSSQLLocalDB.
class Lab07
{
    const string Database = "VisualStudioLab07";
    const string Table = "dbo.[Факультеты]";
    static readonly string Token = Guid.NewGuid().ToString("N");
    static readonly string Server = Environment.GetEnvironmentVariable("LAB07_SQL")
        ?? @"Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True;Connect Timeout=5";

    static SqlConnection Connect(string database)
    {
        var settings = new SqlConnectionStringBuilder(Server) { InitialCatalog = database };
        var connection = new SqlConnection(settings.ConnectionString);
        try { connection.Open(); return connection; }
        catch { connection.Dispose(); throw; }
    }

    static void Initialize()
    {
        using (var sql = Connect("master"))
        using (var cmd = new SqlCommand("IF DB_ID(N'" + Database + "') IS NULL CREATE DATABASE [" + Database + "]", sql))
            cmd.ExecuteNonQuery();
        using (var sql = Connect(Database))
        using (var cmd = new SqlCommand(
            "IF OBJECT_ID(N'dbo.Факультеты', N'U') IS NULL CREATE TABLE " + Table + @" (
                Id INT IDENTITY PRIMARY KEY,
                [Наименование] NVARCHAR(200) NOT NULL UNIQUE,
                [Количество сотрудников] INT NOT NULL CHECK ([Количество сотрудников] >= 0),
                [Количество студентов] INT NOT NULL CHECK ([Количество студентов] >= 0)
            );", sql))
            cmd.ExecuteNonQuery();
    }

    static DataTable Rows()
    {
        using (var sql = Connect(Database))
        using (var adapter = new SqlDataAdapter("SELECT Id, [Наименование], [Количество сотрудников], [Количество студентов] FROM " + Table + " ORDER BY [Наименование]", sql))
        {
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }
    }

    static void Save(int id, string name, string workers, string students)
    {
        int w, s;
        name = (name ?? "").Trim();
        if (name.Length == 0 || name.Length > 200 || !int.TryParse(workers, out w) || w < 0 ||
            !int.TryParse(students, out s) || s < 0)
            throw new ArgumentException("Укажите название до 200 символов и два неотрицательных целых числа.");
        string sqlText = id == 0
            ? "INSERT INTO " + Table + " VALUES (@name, @workers, @students)"
            : "UPDATE " + Table + " SET [Наименование]=@name, [Количество сотрудников]=@workers, [Количество студентов]=@students WHERE Id=@id";
        using (var sql = Connect(Database))
        using (var cmd = new SqlCommand(sqlText, sql))
        {
            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name;
            cmd.Parameters.Add("@workers", SqlDbType.Int).Value = w;
            cmd.Parameters.Add("@students", SqlDbType.Int).Value = s;
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
            if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Факультет не найден.");
        }
    }

    static void Delete(int id)
    {
        using (var sql = Connect(Database))
        using (var cmd = new SqlCommand("DELETE FROM " + Table + " WHERE Id=@id", sql))
        {
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
            if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Факультет не найден.");
        }
    }

    [STAThread]
    static void Main(string[] args)
    {
        string mode = args.Length == 0 ? "desktop" : args[0];
        try
        {
            Initialize();
            if (mode == "web") RunWeb();
            else if (mode == "init") Console.WriteLine("Таблица «Факультеты» готова.");
            else Desktop();
        }
        catch (Exception e)
        {
            if (mode == "desktop") MessageBox.Show(e.Message, "Лабораторная №7");
            else { Console.Error.WriteLine(e.Message); Environment.ExitCode = 1; }
        }
    }

    static void Desktop()
    {
        Application.EnableVisualStyles();
        var form = new Form { Text = "Лабораторная №7 — факультеты", Width = 870, Height = 460 };
        var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        Action refresh = () => grid.DataSource = Rows();
        var controls = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 70 };
        var name = Field(controls, "Наименование", 150);
        var workers = Field(controls, "Сотрудников", 60);
        var students = Field(controls, "Студентов", 60);
        Func<int> selected = () => {
            if (grid.CurrentRow == null) throw new InvalidOperationException("Выберите строку.");
            return Convert.ToInt32(grid.CurrentRow.Cells[0].Value);
        };
        grid.CellClick += (s, e) => {
            if (e.RowIndex < 0) return;
            var row = grid.Rows[e.RowIndex];
            name.Text = Convert.ToString(row.Cells[1].Value);
            workers.Text = Convert.ToString(row.Cells[2].Value);
            students.Text = Convert.ToString(row.Cells[3].Value);
        };
        Button(controls, "Добавить", () => { Save(0, name.Text, workers.Text, students.Text); refresh(); });
        Button(controls, "Изменить", () => { Save(selected(), name.Text, workers.Text, students.Text); refresh(); });
        Button(controls, "Удалить", () => {
            int id = selected();
            if (MessageBox.Show("Удалить выбранный факультет?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            Delete(id); refresh();
        });
        Button(controls, "Обновить", refresh);
        Button(controls, "Отчёт Excel", () => {
            using (var file = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "Факультеты.xlsx" })
                if (file.ShowDialog() == DialogResult.OK) File.WriteAllBytes(file.FileName, Workbook(Rows()));
        });
        form.Controls.Add(grid); form.Controls.Add(controls);
        refresh(); Application.Run(form);
    }

    static TextBox Field(FlowLayoutPanel panel, string label, int width)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true });
        var box = new TextBox { Width = width };
        panel.Controls.Add(box);
        return box;
    }

    static void Button(FlowLayoutPanel panel, string label, Action action)
    {
        var button = new Button { Text = label, AutoSize = true };
        button.Click += (s, e) => { try { action(); } catch (Exception ex) { MessageBox.Show(ex.Message); } };
        panel.Controls.Add(button);
    }

    static void RunWeb()
    {
        using (var server = new HttpListener())
        {
            server.Prefixes.Add("http://localhost:8077/");
            server.Start();
            Console.WriteLine("http://localhost:8077/ — Ctrl+C для остановки.");
            while (true)
            {
                var context = server.GetContext();
                try { Handle(context); }
                catch (IOException) { }
                catch (HttpListenerException) { }
                catch (Exception)
                {
                    try { Send(context, 500, "Операция завершилась ошибкой."); }
                    catch (InvalidOperationException) { }
                }
                finally { context.Response.Close(); }
            }
        }
    }

    static void Handle(HttpListenerContext ctx)
    {
        string path = ctx.Request.Url.AbsolutePath;
        if (ctx.Request.HttpMethod == "GET" && path == "/report")
        {
            byte[] report = Workbook(Rows());
            ctx.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            ctx.Response.AddHeader("Content-Disposition", "attachment; filename=faculties.xlsx");
            ctx.Response.OutputStream.Write(report, 0, report.Length);
            return;
        }
        if (ctx.Request.HttpMethod == "POST" && path == "/")
        {
            if (ctx.Request.ContentLength64 < 0 || ctx.Request.ContentLength64 > 4096) { Send(ctx, 400, "Некорректный запрос."); return; }
            using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
            {
                var fields = HttpUtility.ParseQueryString(reader.ReadToEnd());
                if (fields["token"] != Token) { Send(ctx, 403, "Откройте форму заново."); return; }
                int id;
                if (!int.TryParse(fields["id"], out id) || id < 0) { Send(ctx, 400, "Некорректный ID."); return; }
                try
                {
                    if (fields["action"] == "delete" && id > 0) Delete(id);
                    else if (fields["action"] == "save") Save(id, fields["name"], fields["workers"], fields["students"]);
                    else { Send(ctx, 400, "Неизвестное действие."); return; }
                }
                catch (ArgumentException e) { Send(ctx, 400, HttpUtility.HtmlEncode(e.Message)); return; }
                catch (SqlException) { Send(ctx, 400, "Запись не сохранена. Проверьте уникальность названия и подключение к БД."); return; }
            }
            ctx.Response.StatusCode = 303; ctx.Response.RedirectLocation = "/";
            return;
        }
        if (ctx.Request.HttpMethod != "GET" || path != "/") { Send(ctx, 404, "Страница не найдена."); return; }
        var html = new StringBuilder("<h1>Факультеты</h1><a href='/report'>Скачать отчёт Excel</a><table border='1'><tr><th>ID</th><th>Наименование</th><th>Сотрудников</th><th>Студентов</th></tr>");
        foreach (DataRow row in Rows().Rows)
        {
            html.Append("<tr>");
            foreach (object cell in row.ItemArray) html.Append("<td>").Append(HttpUtility.HtmlEncode(Convert.ToString(cell))).Append("</td>");
            html.Append("</tr>");
        }
        html.Append("</table><h2>Добавить или обновить</h2><form method='post'><input type='hidden' name='token' value='").Append(Token)
            .Append("'><label>ID (0 для новой записи) <input name='id' type='number' min='0' value='0' required></label><br>")
            .Append("<label>Название <input name='name' maxlength='200' required></label><br><label>Сотрудников <input name='workers' type='number' min='0' required></label><br>")
            .Append("<label>Студентов <input name='students' type='number' min='0' required></label><br><button name='action' value='save'>Сохранить</button></form>")
            .Append("<h2>Удалить по ID</h2><form method='post'><input type='hidden' name='token' value='").Append(Token)
            .Append("'><label>ID <input name='id' type='number' min='1' required></label> <button name='action' value='delete'>Удалить</button></form>");
        Send(ctx, 200, html.ToString());
    }

    static void Send(HttpListenerContext ctx, int status, string html)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "text/html; charset=utf-8";
        byte[] bytes = Encoding.UTF8.GetBytes("<!doctype html><html lang='ru'><meta charset='utf-8'><title>Факультеты</title>" + html);
        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
    }

    // Минимальная книга Open XML, которую можно открыть в Excel.
    internal static byte[] Workbook(DataTable table)
    {
        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        const string relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        const string package = "http://schemas.openxmlformats.org/package/2006/relationships";
        using (var buffer = new MemoryStream())
        {
            using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
            {
                Entry(zip, "[Content_Types].xml", "<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'><Default Extension='rels' ContentType='application/vnd.openxmlformats-package.relationships+xml'/><Default Extension='xml' ContentType='application/xml'/><Override PartName='/xl/workbook.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml'/><Override PartName='/xl/worksheets/sheet1.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml'/></Types>");
                Entry(zip, "_rels/.rels", "<Relationships xmlns='" + package + "'><Relationship Id='rId1' Type='" + relationships + "/officeDocument' Target='xl/workbook.xml'/></Relationships>");
                Entry(zip, "xl/workbook.xml", "<workbook xmlns='" + ns + "' xmlns:r='" + relationships + "'><sheets><sheet name='Факультеты' sheetId='1' r:id='rId1'/></sheets></workbook>");
                Entry(zip, "xl/_rels/workbook.xml.rels", "<Relationships xmlns='" + package + "'><Relationship Id='rId1' Type='" + relationships + "/worksheet' Target='worksheets/sheet1.xml'/></Relationships>");
                using (var stream = zip.CreateEntry("xl/worksheets/sheet1.xml").Open())
                using (var xml = XmlWriter.Create(stream))
                {
                    xml.WriteStartElement("worksheet", ns); xml.WriteStartElement("sheetData", ns);
                    xml.WriteStartElement("row", ns);
                    foreach (DataColumn column in table.Columns) Cell(xml, ns, column.ColumnName);
                    xml.WriteEndElement();
                    foreach (DataRow row in table.Rows)
                    {
                        xml.WriteStartElement("row", ns);
                        foreach (object value in row.ItemArray) Cell(xml, ns, value);
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement(); xml.WriteEndElement();
                }
            }
            return buffer.ToArray();
        }
    }

    static void Cell(XmlWriter xml, string ns, object value)
    {
        xml.WriteStartElement("c", ns);
        if (value is int) xml.WriteElementString("v", ns, Convert.ToString(value, CultureInfo.InvariantCulture));
        else
        {
            xml.WriteAttributeString("t", "inlineStr"); xml.WriteStartElement("is", ns);
            xml.WriteElementString("t", ns, Convert.ToString(value)); xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }

    static void Entry(ZipArchive zip, string path, string xml)
    {
        using (var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false))) writer.Write(xml);
    }
}
