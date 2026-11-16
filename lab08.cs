using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

// Источники и собранные lab01.exe … lab07.exe должны лежать рядом с lab08.exe.
class Lab08
{
    static readonly string Root = AppDomain.CurrentDomain.BaseDirectory;
    static readonly string Token = Guid.NewGuid().ToString("N");
    static readonly string[,] Lessons = {
        { "Алгоритмы и матрицы", "Реализовать расширенный алгоритм Евклида, ввод товаров, функцию, произведение и операции с матрицей.", "Коэффициенты Безу удовлетворяют равенству a·x + b·y = НОД(a,b)." },
        { "Контейнеры", "Реализовать очередь строк, стек символов и отсортированный список столиц.", "Queue использует FIFO, Stack использует LIFO, SortedList упорядочивает пары по ключу." },
        { "Наследование", "Создать классы с абстрактными методами и наследуемый интерфейс арифметики.", "Абстрактный класс может хранить состояние. Класс может реализовать несколько интерфейсов." },
        { "ArrayList и BitArray", "Обработать два списка и сделать битовые операции над двумя массивами.", "Or, And и Not меняют экземпляр BitArray, поэтому операции выполняются на копиях." },
        { "Графика и COM", "Рисовать фигуры и передавать таблицу Access в Word и Excel.", "Меню цвета задаёт кисть и перо. Экспорт требует Jet или ACE, Word и Excel." },
        { "Потоки и файлы", "Записывать, добавлять и читать товары. Реализовать статические методы VVod, Save и Open.", "StreamReader и StreamWriter работают с текстом. Товары хранятся в VisualStudioLabs в LocalAppData." },
        { "SQL Server", "Создать таблицу факультетов и приложения с отчётом Excel.", "Для запуска нужен SQL Server или LocalDB. Аргумент web включает веб-интерфейс." }
    };

    static void Main(string[] args)
    {
        string author = args.Length == 0 ? "Рамиль" : string.Join(" ", args);
        using (var server = new HttpListener())
        {
            server.Prefixes.Add("http://localhost:8088/");
            server.Start();
            Console.WriteLine("Мои лабораторные работы — http://localhost:8088/. Ctrl+C для остановки.");
            while (true)
            {
                HttpListenerContext context = server.GetContext();
                try { Handle(context, author); }
                catch (IOException) { }
                catch (HttpListenerException) { }
                finally { context.Response.Close(); }
            }
        }
    }

    static void Handle(HttpListenerContext context, string author)
    {
        string path = context.Request.Url.AbsolutePath;
        string method = context.Request.HttpMethod;
        if (path == "/" && method == "GET")
        {
            var links = new StringBuilder("<h1>Мои лабораторные работы</h1><p>Автор: " + Enc(author) + "</p><p>Вариант 7</p><ol>");
            for (int i = 0; i < Lessons.GetLength(0); i++)
                links.AppendFormat("<li><a href='/lab/{0}'>{1}</a></li>", i + 1, Enc(Lessons[i, 0]));
            Send(context, 200, links.Append("</ol>").ToString());
            return;
        }
        int lab;
        if (!path.StartsWith("/lab/", StringComparison.Ordinal) ||
            !int.TryParse(path.Substring(5), out lab) || lab < 1 || lab > Lessons.GetLength(0))
        { Send(context, 404, "<h1>Страница не найдена</h1>"); return; }
        string result = "";
        if (method == "POST")
        {
            if (context.Request.ContentLength64 < 0 || context.Request.ContentLength64 > 128)
            { Send(context, 400, "<p>Некорректный запрос.</p>"); return; }
            using (var reader = new StreamReader(context.Request.InputStream))
            {
                if (HttpUtility.ParseQueryString(reader.ReadToEnd())["token"] != Token)
                { Send(context, 403, "<p>Откройте страницу лабораторной заново.</p>"); return; }
            }
            try { result = "<h2>Результат запуска</h2><pre>" + Enc(RunLab(lab)) + "</pre>"; }
            catch (Exception ex) { result = "<p>" + Enc(ex.Message) + "</p>"; }
        }
        else if (method != "GET") { Send(context, 405, "<p>Метод не поддерживается.</p>"); return; }
        string source = Path.Combine(Root, string.Format("lab{0:00}.cs", lab));
        string code = File.Exists(source) ? File.ReadAllText(source) : "Поместите исходный файл рядом с сервером.";
        Send(context, 200, "<h1>Лабораторная №" + lab + ". " + Enc(Lessons[lab - 1, 0]) + "</h1>" +
            "<h2>Задание</h2><p>" + Enc(Lessons[lab - 1, 1]) + "</p>" +
            "<form method='post'><input type='hidden' name='token' value='" + Token +
            "'><button>Запустить на сервере</button></form>" + result +
            "<h2>Справка</h2><p>" + Enc(Lessons[lab - 1, 2]) + "</p><h2>Код приложения</h2><pre>" +
            Enc(code) + "</pre><a href='/'>К списку работ</a>");
    }

    static string RunLab(int lab)
    {
        string binary = Path.Combine(Root, string.Format("lab{0:00}.exe", lab));
        if (!File.Exists(binary)) throw new FileNotFoundException("Сначала соберите " + Path.GetFileName(binary) + ".");
        bool console = lab == 3 || lab == 4;
        var start = new ProcessStartInfo(binary) {
            UseShellExecute = false, WorkingDirectory = Root, CreateNoWindow = console,
            RedirectStandardInput = console, RedirectStandardOutput = console, RedirectStandardError = console
        };
        if (console) start.StandardOutputEncoding = Encoding.UTF8;
        using (var child = Process.Start(start))
        {
            if (!console) return "Окно приложения открывается там, где запущен сервер.";
            var output = child.StandardOutput.ReadToEndAsync();
            var errors = child.StandardError.ReadToEndAsync();
            if (lab == 3) child.StandardInput.Write("1\n2\n3\n4\n5\n6\nStudent\n2\n7\n8\n3\n4\n5\n");
            child.StandardInput.Close();
            if (!child.WaitForExit(5000))
            {
                child.Kill(); child.WaitForExit();
                throw new TimeoutException("Приложение не завершилось за 5 секунд.");
            }
            return output.Result + errors.Result;
        }
    }

    static string Enc(string value) { return HttpUtility.HtmlEncode(value); }

    static void Send(HttpListenerContext context, int status, string html)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "text/html; charset=utf-8";
        byte[] bytes = Encoding.UTF8.GetBytes("<!doctype html><html lang='ru'><meta charset='utf-8'>" +
            "<title>Мои лабораторные работы</title><style>body{font:16px system-ui;max-width:860px;margin:35px auto;padding:0 18px}pre{background:#f2f4f7;padding:1em;overflow:auto}</style>" +
            html + "</html>");
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
    }
}
