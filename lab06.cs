using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

// Вариант 1: для работ №5–6 методичка не содержит варианта 7.
class Lab06
{
    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    struct Tovar
    {
        public string Name;
        public int Quantity;
        public decimal Price;
        public decimal Cost { get { return Quantity * Price; } }

        public string Serialize()
        {
            string name = Convert.ToBase64String(Encoding.UTF8.GetBytes(Name));
            return string.Join("\t", name, Quantity.ToString(Invariant), Price.ToString(Invariant), Cost.ToString(Invariant));
        }

        public static Tovar Parse(string line)
        {
            string[] parts = line.Split('\t');
            if (parts.Length != 4) throw new FormatException("Некорректная строка в файле.");
            var item = Create(Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])),
                int.Parse(parts[1], Invariant), decimal.Parse(parts[2], Invariant));
            if (item.Cost != decimal.Parse(parts[3], Invariant)) throw new FormatException("Стоимость не совпадает с ценой × количество.");
            return item;
        }

        public static Tovar Create(string name, int quantity, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name) || quantity <= 0 || price <= 0)
                throw new FormatException("Введите название, положительное количество и цену.");
            decimal.Multiply(quantity, price);
            return new Tovar { Name = name.Trim(), Quantity = quantity, Price = price };
        }

        public override string ToString()
        {
            return string.Format("{0} | {1} × {2:F2} = {3:F2}", Name, Quantity, Price, Cost);
        }
    }

    static class StaticExercise
    {
        public static class Tovar
        {
            static string name;
            static int quantity;
            static decimal price, cost;

            public static void VVod(string title, int units, decimal unitPrice)
            {
                var item = Lab06.Tovar.Create(title, units, unitPrice);
                name = item.Name; quantity = item.Quantity; price = item.Price; cost = item.Cost;
            }

            public static void Save(string path)
            {
                if (name == null) throw new InvalidOperationException("Сначала введите товар.");
                Write(path, new[] { Lab06.Tovar.Create(name, quantity, price) }, false);
            }

            public static string Open(string path)
            {
                List<Lab06.Tovar> items = Read(path);
                if (items.Count != 1) throw new FormatException("В файле должна быть одна запись.");
                var item = items[0];
                VVod(item.Name, item.Quantity, item.Price);
                return string.Format("{0} | {1} × {2:F2} = {3:F2}", name, quantity, price, cost);
            }
        }
    }

    static List<Tovar> Read(string path)
    {
        var items = new List<Tovar>();
        using (var reader = new StreamReader(path, Encoding.UTF8))
        {
            string line;
            while ((line = reader.ReadLine()) != null) items.Add(Tovar.Parse(line));
        }
        return items;
    }

    static void Write(string path, IEnumerable<Tovar> items, bool append)
    {
        using (var writer = new StreamWriter(path, append, new UTF8Encoding(false)))
            foreach (var item in items) writer.WriteLine(item.Serialize());
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        var window = new Form { Text = "Лабораторная №6 — файлы", Width = 750, Height = 460 };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        window.Controls.Add(tabs);
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VisualStudioLabs");
        Directory.CreateDirectory(folder);
        BuildPage(tabs, Path.Combine(folder, "tovars.txt"), false);
        BuildPage(tabs, Path.Combine(folder, "mydb1.txt"), true);
        Application.Run(window);
    }

    static void BuildPage(TabControl tabs, string path, bool staticMode)
    {
        var page = new TabPage(staticMode ? "Статический класс Tovar" : "Структура Tovar");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var name = Field(panel, "Наименование", 180);
        var quantity = Field(panel, "Количество", 65);
        var price = Field(panel, "Цена", 90);
        panel.SetFlowBreak(price, true);
        var view = new ListBox { Width = 690, Height = 285 };
        var items = new List<Tovar>();
        Action refresh = () => {
            view.Items.Clear();
            foreach (var item in items) view.Items.Add(item.ToString());
        };
        Func<Tovar> input = () => Tovar.Create(name.Text, int.Parse(quantity.Text), decimal.Parse(price.Text));
        if (staticMode)
        {
            AddButton(panel, "VVod", () => {
                var item = input();
                StaticExercise.Tovar.VVod(item.Name, item.Quantity, item.Price);
                view.Items.Clear(); view.Items.Add(item.ToString());
            });
            AddButton(panel, "Save", () => StaticExercise.Tovar.Save(path));
            AddButton(panel, "Open", () => {
                string record = StaticExercise.Tovar.Open(path);
                view.Items.Clear(); view.Items.Add(record);
            });
        }
        else
        {
            AddButton(panel, "Добавить в список", () => { items.Add(input()); refresh(); });
            AddButton(panel, "Записать список", () => Write(path, items, false));
            AddButton(panel, "Добавить в файл", () => {
                var item = input();
                Write(path, new[] { item }, true);
                var saved = Read(path);
                items.Clear(); items.AddRange(saved); refresh();
            });
            AddButton(panel, "Прочитать", () => {
                var loaded = Read(path);
                items.Clear(); items.AddRange(loaded); refresh();
            });
        }
        panel.Controls.Add(view);
        panel.Controls.Add(new Label { Text = path, AutoSize = true });
        page.Controls.Add(panel); tabs.TabPages.Add(page);
    }

    static TextBox Field(FlowLayoutPanel panel, string label, int width)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true });
        var textbox = new TextBox { Width = width };
        panel.Controls.Add(textbox);
        return textbox;
    }

    static void AddButton(FlowLayoutPanel panel, string title, Action action)
    {
        var button = new Button { Text = title, AutoSize = true };
        button.Click += (s, e) => {
            try { action(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
        };
        panel.Controls.Add(button);
    }
}
