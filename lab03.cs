using System;
using System.Numerics;

class Lab03
{
    abstract class Record
    {
        public abstract void Input();
        public abstract void Output();
    }

    class RealTriple : Record
    {
        double x, y, z;
        public RealTriple() { x = y = z = 0; }
        public override void Input() { x = Read("x", double.Parse); y = Read("y", double.Parse); z = Read("z", double.Parse); }
        public override void Output() { Console.WriteLine("x={0}, y={1}, z={2}", x, y, z); }
    }

    class IntegerTriple : Record
    {
        int a, b, c;
        public IntegerTriple() { a = b = c = 0; }
        public override void Input() { a = Read("a", int.Parse); b = Read("b", int.Parse); c = Read("c", int.Parse); }
        public override void Output() { Console.WriteLine("a={0}, b={1}, c={2}", a, b, c); }
    }

    class Student : Record
    {
        string name;
        int curs;
        public Student() { name = "-"; curs = 0; }
        public override void Input() { name = Read("Имя", Required); curs = Read("Курс", int.Parse); }
        public override void Output() { Console.WriteLine("Имя: {0}, курс: {1}", name, curs); }
    }

    class NumberRecord : Record
    {
        int name, number;
        public NumberRecord() { name = number = 0; }
        public override void Input() { name = Read("Код имени", int.Parse); number = Read("Номер", int.Parse); }
        public override void Output() { Console.WriteLine("Код имени: {0}, номер: {1}", name, number); }
    }

    interface IRecord { void Input(); void Output(); }
    interface IArithmetic : IRecord { void Sum(); void Mult(); }

    class Myclass : IArithmetic
    {
        BigInteger x, y, z;
        public void Input() { x = Read("x", BigInteger.Parse); y = Read("y", BigInteger.Parse); z = Read("z", BigInteger.Parse); }
        public void Output() { Console.WriteLine("x={0}, y={1}, z={2}", x, y, z); }
        public void Sum() { Console.WriteLine("Сумма: " + (x + y + z)); }
        public void Mult() { Console.WriteLine("Произведение: " + (x * y * z)); }
    }

    static string Required(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new FormatException();
        return text.Trim();
    }

    static T Read<T>(string label, Func<string, T> parse)
    {
        while (true)
        {
            Console.Write(label + ": ");
            string value = Console.ReadLine();
            if (value == null) throw new System.IO.EndOfStreamException();
            try { return parse(value); }
            catch (FormatException) { Console.WriteLine("Проверьте формат."); }
            catch (OverflowException) { Console.WriteLine("Число слишком велико."); }
        }
    }

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        try
        {
            Console.WriteLine("Задания 1–2. Абстрактные методы и полиморфизм");
            foreach (Record record in new Record[] { new RealTriple(), new IntegerTriple(), new Student(), new NumberRecord() })
            {
                record.Output(); record.Input(); record.Output();
            }
            Console.WriteLine("Задание 3. Наследование интерфейсов");
            IArithmetic values = new Myclass();
            values.Input(); values.Output(); values.Sum(); values.Mult();
        }
        catch (System.IO.EndOfStreamException) { Console.WriteLine("Ввод завершён."); }
    }
}
