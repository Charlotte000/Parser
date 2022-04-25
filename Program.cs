namespace Parser
{
    using System.Text;
    using AngleSharp.Dom;

    class Program
    {
        static void Main()
        {
            // Set windows-1251 encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Set current city
            Parser.CurrentCity = Parser.Rostov;

            var data = new List<ProductData>();

            var pageNum = Parser.GetPageCount().Result;
            for (int i = 1; i <= pageNum; i++)
            {
                Console.WriteLine($"Парсинг {i}/{pageNum} страницы:");
                var links = Parser.GetLinks(i).Result;

                data.AddRange(Program.ParseProducts(links));
                Thread.Sleep(5000);
            }

            Program.SaveData(data, "data.csv");
        }

        static List<ProductData> ParseProducts(List<Url> links)
        {
            var data = new List<ProductData>();
            var threads = new List<Thread>();

            // Creating threads
            foreach (var link in links)
            {
                var thread = new Thread(() =>
                {
                    for (int attempt = 0; attempt < 10; attempt++)
                    {
                        try
                        {
                            data.Add(Parser.ParsePage(link).Result);
                            return;
                        }
                        catch (AggregateException)
                        {
                            Thread.Sleep(5000);
                        }
                    }

                    throw new Exception("Exceeded the maximum number of attempts");
                });
                thread.Start();
                threads.Add(thread);
            }

            // Waiting for threads to finish working
            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].Join();
                Console.WriteLine($"  {i + 1}/{threads.Count}");
            }

            return data;
        }

        static void SaveData(List<ProductData> data, string fileName)
        {
            var csv = new StringBuilder();

            // Header
            if (!File.Exists(fileName))
            {
                csv.AppendLine(
                    "Название товара;" +
                    "Доступность;" +
                    "Цена;" +
                    "Цена старая;" +
                    "Хлебные крошки;" +
                    "Регион;" +
                    "Сслыка на товар;" +
                    "Ссылки на картинки");
            }

            // Content
            foreach (var product in data)
            {
                csv.AppendLine(
                    $"{product.Name};" +
                    $"{product.Availability};" +
                    $"{product.Price};" +
                    $"{product.OldPrice};" +
                    $"{product.Breadcrumbs};" +
                    $"{product.Region};" +
                    $"{product.ProductLink};" +
                    $"{string.Join(", ", product.ImagesLink)}");
            }

            File.AppendAllText(fileName, csv.ToString(), Encoding.GetEncoding("windows-1251"));
        }
    }
}