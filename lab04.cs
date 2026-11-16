using System;
using System.Collections;
using System.Linq;

class Lab04
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Задание 1. ArrayList, вариант 7");
        var random = new Random();
        var first = new ArrayList();
        for (int i = 0; i < 12; i++) first.Add(random.Next(-59, 11) / 10.0);
        var second = new ArrayList(first.GetRange(0, first.Count / 2));
        Show("Первый список", first);
        first.Sort(); second.Sort();
        Show("Сортировка первого", first); Show("Сортировка второго", second);
        first.Reverse(); second.Reverse();
        Show("Обращение первого", first); Show("Обращение второго", second);
        second.Insert(0, -1.5); second.Insert(2, -3.2); second.Insert(4, 0.8);
        Show("Вставка в позиции 1, 3 и 5", second);
        Console.WriteLine("Есть -3,2: " + second.Contains(-3.2));
        second.Remove(-3.2);
        Show("После удаления -3,2", second);

        Console.WriteLine("Задание 2. BitArray, вариант 1 (варианта 7 в таблице нет)");
        BitArray a = Bits("10011011"), b = Bits("10101101");
        ShowBits("A", a); ShowBits("B", b);
        ShowBits("A OR B", new BitArray(a).Or(b));
        ShowBits("A AND B", new BitArray(a).And(b));
        ShowBits("NOT A", new BitArray(a).Not());
    }

    static BitArray Bits(string bits) { return new BitArray(bits.Select(c => c == '1').ToArray()); }
    static void ShowBits(string label, BitArray bits)
    {
        Console.WriteLine(label + ": " + string.Concat(bits.Cast<bool>().Select(x => x ? "1" : "0")));
    }
    static void Show(string label, ArrayList values)
    {
        Console.WriteLine(label + ": " + string.Join("  ", values.Cast<double>().Select(x => x.ToString("F1"))));
    }
}
