using System;
using System.Collections;
using System.Linq;
using System.Windows.Forms;

class Lab02
{
    static readonly Queue Queue = new Queue();
    static readonly Stack Stack = new Stack();
    static readonly SortedList Capitals = new SortedList();

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        var window = new Form { Text = "Лабораторная №2 — вариант 7", Width = 650, Height = 420 };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        window.Controls.Add(tabs);
        AddPage(tabs, "Очередь строк", Queue, Queue.Enqueue, Queue.Dequeue, Queue.Peek, Queue.Contains, Queue.Clear, false);
        AddPage(tabs, "Стек символов", Stack, Stack.Push, Stack.Pop, Stack.Peek, Stack.Contains, Stack.Clear, true);

        var page = new TabPage("Столицы регионов");
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var city = new TextBox { Width = 220 };
        var code = new TextBox { Width = 80 };
        var list = new ListBox { Width = 595, Height = 245 };
        Action refresh = () => {
            list.Items.Clear();
            foreach (DictionaryEntry row in Capitals) list.Items.Add(row.Key + " — " + row.Value);
        };
        layout.Controls.Add(new Label { Text = "Столица", AutoSize = true });
        layout.Controls.Add(city);
        layout.Controls.Add(new Label { Text = "Код", AutoSize = true });
        layout.Controls.Add(code);
        AddButton(layout, "Добавить", () => {
            int value;
            string key = city.Text.Trim();
            if (key.Length == 0 || !int.TryParse(code.Text, out value) || value <= 0)
                throw new ArgumentException("Введите название и положительный целый код.");
            if (Capitals.ContainsKey(key)) throw new ArgumentException("Столица уже есть в списке.");
            Capitals.Add(key, value);
            refresh();
        });
        AddButton(layout, "Удалить", () => {
            int index = list.SelectedIndex >= 0 ? list.SelectedIndex : Capitals.IndexOfKey(city.Text.Trim());
            if (index < 0) throw new ArgumentException("Выберите столицу или введите её название.");
            Capitals.RemoveAt(index);
            refresh();
        });
        AddButton(layout, "Найти", () => {
            int index = Capitals.IndexOfKey(city.Text.Trim());
            if (index < 0) throw new ArgumentException("Столица не найдена.");
            list.SelectedIndex = index;
        });
        AddButton(layout, "Очистить", () => { Capitals.Clear(); refresh(); });
        layout.Controls.Add(list);
        page.Controls.Add(layout);
        tabs.TabPages.Add(page);
        Capitals.Add("Екатеринбург", 66); Capitals.Add("Казань", 16); Capitals.Add("Уфа", 2);
        refresh();
        Application.Run(window);
    }

    static void AddPage(TabControl tabs, string title, ICollection data, Action<object> add,
        Func<object> remove, Func<object> peek, Func<object, bool> contains, Action clear, bool chars)
    {
        var page = new TabPage(title);
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var input = new TextBox { Width = 110 };
        var list = new ListBox { Width = 595, Height = 245 };
        var count = new Label { AutoSize = true };
        Func<object> value = () => {
            string text = input.Text.Trim();
            if (text.Length == 0 || (chars && text.Length != 1))
                throw new ArgumentException(chars ? "Введите один символ." : "Введите строку.");
            return chars ? (object)text[0] : text;
        };
        Action refresh = () => {
            list.Items.Clear(); list.Items.AddRange(data.Cast<object>().ToArray());
            count.Text = "Элементов: " + data.Count;
        };
        layout.Controls.Add(input);
        AddButton(layout, chars ? "Push" : "Enqueue", () => { add(value()); refresh(); });
        AddButton(layout, chars ? "Pop" : "Dequeue", () => { remove(); refresh(); });
        AddButton(layout, "Peek", () => MessageBox.Show(peek().ToString()));
        AddButton(layout, "Contains", () => MessageBox.Show(contains(value()) ? "Найдено." : "Не найдено."));
        AddButton(layout, "Clear", () => { clear(); refresh(); });
        layout.Controls.Add(count); layout.Controls.Add(list);
        page.Controls.Add(layout); tabs.TabPages.Add(page); refresh();
    }

    static void AddButton(FlowLayoutPanel panel, string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (s, e) => {
            try { action(); }
            catch (ArgumentException ex) { MessageBox.Show(ex.Message); }
            catch (InvalidOperationException) { MessageBox.Show("Коллекция пуста."); }
        };
        panel.Controls.Add(button);
    }
}
