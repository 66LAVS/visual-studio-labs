using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

class Lab01
{
    static readonly Random Random = new Random();
    static double[,] matrix;

    struct Product
    {
        public string Name, Unit;
        public decimal Price;
        public int Quantity;
    }

    class Myclass
    {
        readonly BigInteger x, y, z;
        public Myclass(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public BigInteger Pr() { return x * y * z; }
    }

    static T Read<T>(string label, Func<string, T> parse)
    {
        while (true)
        {
            Console.Write(label + ": ");
            string value = Console.ReadLine();
            if (value == null) throw new System.IO.EndOfStreamException();
            try { return parse(value.Trim()); }
            catch (FormatException) { Console.WriteLine("Проверьте ввод."); }
            catch (OverflowException) { Console.WriteLine("Число слишком велико."); }
        }
    }

    static int Positive(string s)
    {
        int n = int.Parse(s);
        if (n <= 0) throw new FormatException();
        return n;
    }

    static string Required(string s)
    {
        if (s.Length == 0) throw new FormatException();
        return s;
    }

    static void Products()
    {
        int count = Read("Количество товаров", Positive);
        var goods = new List<Product>();
        for (int i = 0; i < count; i++)
        {
            goods.Add(new Product {
                Name = Read("Наименование", Required), Unit = Read("Единица измерения", Required),
                Price = Read("Цена", s => { decimal p = decimal.Parse(s); if (p <= 0) throw new FormatException(); return p; }),
                Quantity = Read("Количество", Positive)
            });
        }
        Console.WriteLine("{0,-25} {1,-15} {2,12} {3,12}", "Товар", "Ед. измерения", "Цена", "Количество");
        foreach (var p in goods) Console.WriteLine("{0,-25} {1,-15} {2,12:F2} {3,12}", p.Name, p.Unit, p.Price, p.Quantity);
    }

    internal static string Euclid(BigInteger a, BigInteger b)
    {
        BigInteger r = a, s = b, x = 1, y = 0, u = 0, v = 1;
        while (s != 0)
        {
            BigInteger q = r / s;
            BigInteger next = r - q * s; r = s; s = next;
            next = x - q * u; x = u; u = next;
            next = y - q * v; y = v; v = next;
        }
        if (r < 0) { r = -r; x = -x; y = -y; }
        return string.Format("НОД = {0}; {1} × ({2}) + {3} × ({4}) = {0}", r, a, x, b, y);
    }

    static TextBox Field(FlowLayoutPanel panel, string label, string value)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true });
        var field = new TextBox { Text = value, Width = 70 };
        panel.Controls.Add(field);
        return field;
    }

    static void Button(FlowLayoutPanel panel, string title, Action action)
    {
        var button = new Button { Text = title, AutoSize = true };
        button.Click += (sender, args) => {
            try { action(); }
            catch (FormatException) { MessageBox.Show("Введите число в указанном формате."); }
            catch (OverflowException) { MessageBox.Show("Число слишком велико."); }
            catch (ArgumentException e) { MessageBox.Show(e.Message); }
            catch (InvalidOperationException e) { MessageBox.Show(e.Message); }
        };
        panel.Controls.Add(button);
    }

    static double[,] Matrix()
    {
        if (matrix == null) throw new InvalidOperationException("Сначала сгенерируйте матрицу.");
        return matrix;
    }

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            try
            {
                if (args[0] == "products") Products();
                else if (args[0] == "product") Console.WriteLine(new Myclass(Read("x", int.Parse), Read("y", int.Parse), Read("z", int.Parse)).Pr());
                else Console.WriteLine("Используйте products, product или запустите без аргументов.");
            }
            catch (System.IO.EndOfStreamException) { Console.WriteLine("Ввод завершён."); }
            return;
        }
        Application.EnableVisualStyles();
        var form = new Form { Text = "Лабораторная №1 — вариант 7", Width = 800, Height = 580 };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };
        form.Controls.Add(panel);
        var a = Field(panel, "a", "240"); var b = Field(panel, "b", "46");
        var gcd = new Label { AutoSize = true };
        Button(panel, "НОД и коэффициенты", () => gcd.Text = Euclid(BigInteger.Parse(a.Text), BigInteger.Parse(b.Text)));
        panel.Controls.Add(gcd); panel.SetFlowBreak(gcd, true);
        var t = Field(panel, "t", "5"); var x = Field(panel, "x", "4"); var y = Field(panel, "y", "2");
        var result = new Label { AutoSize = true };
        Button(panel, "Z = √(x + y + t) − |x − y|", () => {
            double xv = int.Parse(x.Text), yv = int.Parse(y.Text), sum = xv + yv + int.Parse(t.Text);
            if (sum < 0) throw new ArgumentException("Подкоренное выражение должно быть неотрицательным.");
            result.Text = "Z = " + (Math.Sqrt(sum) - Math.Abs(xv - yv)).ToString("G10");
        });
        panel.Controls.Add(result); panel.SetFlowBreak(result, true);
        var n = Field(panel, "Строк", "4"); var m = Field(panel, "Столбцов", "5");
        var grid = new DataGridView { Width = 735, Height = 350, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false };
        Button(panel, "Генерация", () => {
            int rows = Positive(n.Text), cols = Positive(m.Text);
            if (rows > 100 || cols > 100) throw new ArgumentException("Размеры матрицы — от 1 до 100.");
            matrix = new double[rows, cols];
            grid.Columns.Clear(); grid.ColumnCount = cols; grid.RowCount = rows;
            for (int c = 0; c < cols; c++) grid.Columns[c].HeaderText = (c + 1).ToString();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++) grid[c, r].Value = matrix[r, c] = Random.Next(-4999, 5000) / 100.0;
        });
        Button(panel, "Сумма", () => MessageBox.Show(Matrix().Cast<double>().Sum().ToString("F2")));
        Button(panel, "Минимум", () => MessageBox.Show(Matrix().Cast<double>().Min().ToString("F2")));
        var query = Field(panel, "Число", "0");
        Button(panel, "Поиск", () => {
            var values = Matrix();
            double number = double.Parse(query.Text);
            var matches = new List<string>();
            for (int r = 0; r < values.GetLength(0); r++)
                for (int c = 0; c < values.GetLength(1); c++)
                    if (values[r, c] == number) matches.Add(string.Format("Строка {0}, столбец {1}", r + 1, c + 1));
            MessageBox.Show(matches.Count == 0 ? "Число не найдено." : string.Join(Environment.NewLine, matches));
        });
        panel.Controls.Add(grid);
        Application.Run(form);
    }
}
