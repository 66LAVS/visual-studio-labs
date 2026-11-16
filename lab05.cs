using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Lab05
{
    static readonly Random Random = new Random();
    static Color brushColor = Color.LightBlue, penColor = Color.Navy;
    static readonly DataGridView Grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false };
    static DataTable products;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        var window = new Form { Text = "Лабораторная №5 — графика и COM", Width = 820, Height = 480 };
        var menu = new MenuStrip();
        var colors = new ToolStripMenuItem("Цвет");
        colors.DropDownItems.Add("Кисть", null, (s, e) => PickColor(ref brushColor));
        colors.DropDownItems.Add("Перо", null, (s, e) => PickColor(ref penColor));
        menu.Items.Add(colors);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 65 };
        var width = new NumericUpDown { Minimum = 1, Maximum = 12, Value = 2, Width = 55 };
        buttons.Controls.Add(new Label { Text = "Толщина пера", AutoSize = true });
        buttons.Controls.Add(width);
        foreach (string figure in new[] { "Эллипсы", "Прямоугольники", "Треугольники" })
        {
            string selected = figure;
            AddButton(buttons, figure, () => OpenDrawing(window, selected, (float)width.Value));
        }
        AddButton(buttons, "Открыть Access", LoadAccess);
        AddButton(buttons, "В Word", () => Export(true));
        AddButton(buttons, "В Excel", () => Export(false));
        var content = new Panel { Dock = DockStyle.Fill };
        content.Controls.Add(Grid);
        content.Controls.Add(buttons);
        window.Controls.Add(content);
        window.Controls.Add(menu);
        window.MainMenuStrip = menu;
        Application.Run(window);
    }

    static void PickColor(ref Color color)
    {
        using (var dialog = new ColorDialog { Color = color })
            if (dialog.ShowDialog() == DialogResult.OK) color = dialog.Color;
    }

    static void OpenDrawing(Form owner, string figure, float width)
    {
        var canvas = new Form { Text = figure, ClientSize = new Size(560, 360) };
        Color fill = brushColor, outline = penColor;
        var shapes = Enumerable.Range(0, 12).Select(i =>
            new Rectangle(Random.Next(10, 420), Random.Next(10, 245), Random.Next(25, 125), Random.Next(25, 100))).ToArray();
        canvas.Paint += (s, e) => {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var pen = new Pen(outline, width))
            using (var brush = new SolidBrush(fill))
            foreach (var shape in shapes)
            {
                if (figure == "Эллипсы") { e.Graphics.FillEllipse(brush, shape); e.Graphics.DrawEllipse(pen, shape); }
                else if (figure == "Прямоугольники") { e.Graphics.FillRectangle(brush, shape); e.Graphics.DrawRectangle(pen, shape); }
                else
                {
                    var vertices = new[] { new Point(shape.Left + shape.Width / 2, shape.Top),
                        new Point(shape.Right, shape.Bottom), new Point(shape.Left, shape.Bottom) };
                    e.Graphics.FillPolygon(brush, vertices); e.Graphics.DrawPolygon(pen, vertices);
                }
            }
        };
        canvas.Show(owner);
    }

    // В таблице заданий нет варианта 7. Используется таблица «Товары» варианта 1.
    static void LoadAccess()
    {
        using (var file = new OpenFileDialog { Filter = "Access (*.mdb;*.accdb)|*.mdb;*.accdb" })
        {
            if (file.ShowDialog() != DialogResult.OK) return;
            var settings = new OleDbConnectionStringBuilder {
                Provider = file.FileName.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase)
                    ? "Microsoft.Jet.OLEDB.4.0" : "Microsoft.ACE.OLEDB.12.0",
                DataSource = file.FileName
            };
            using (var connection = new OleDbConnection(settings.ConnectionString))
            using (var adapter = new OleDbDataAdapter("SELECT [Наименование], [Количество], [Цена], [Стоимость] FROM [Товары]", connection))
            {
                var loaded = new DataTable();
                adapter.Fill(loaded);
                products = loaded;
                Grid.DataSource = products;
            }
        }
    }

    static void Export(bool word)
    {
        if (products == null) throw new InvalidOperationException("Сначала откройте базу Access.");
        string extension = word ? "docx" : "xlsx";
        using (var file = new SaveFileDialog { Filter = extension.ToUpperInvariant() + "|*." + extension, DefaultExt = extension, FileName = "Товары." + extension })
        {
            if (file.ShowDialog() != DialogResult.OK) return;
            Type office = Type.GetTypeFromProgID(word ? "Word.Application" : "Excel.Application");
            if (office == null) throw new InvalidOperationException(word ? "Microsoft Word не установлен." : "Microsoft Excel не установлен.");
            dynamic app = Activator.CreateInstance(office);
            dynamic book = null;
            try
            {
                int rows = products.Rows.Count + 1, cols = products.Columns.Count;
                if (word)
                {
                    book = app.Documents.Add();
                    dynamic table = book.Tables.Add(book.Range(0, 0), rows, cols);
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            table.Cell(r + 1, c + 1).Range.Text = r == 0 ? products.Columns[c].ColumnName : Convert.ToString(products.Rows[r - 1][c]);
                    table.Borders.Enable = 1;
                    book.SaveAs2(file.FileName, 12);
                }
                else
                {
                    book = app.Workbooks.Add();
                    dynamic sheet = book.Worksheets[1];
                    object[,] values = new object[rows, cols];
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++) values[r, c] = r == 0 ? products.Columns[c].ColumnName : products.Rows[r - 1][c];
                    sheet.Range[sheet.Cells[1, 1], sheet.Cells[rows, cols]].Value2 = values;
                    sheet.Columns.AutoFit();
                    app.DisplayAlerts = false;
                    book.SaveAs(file.FileName, 51);
                }
            }
            finally
            {
                try { if (book != null) book.Close(false); }
                finally { try { app.Quit(); } finally { Marshal.FinalReleaseComObject(app); } }
            }
            MessageBox.Show("Файл сохранён.");
        }
    }

    static void AddButton(FlowLayoutPanel panel, string title, Action action)
    {
        var button = new Button { Text = title, AutoSize = true };
        button.Click += (s, e) => {
            try { action(); }
            catch (Exception error) { MessageBox.Show(error.Message, "Ошибка"); }
        };
        panel.Controls.Add(button);
    }
}
