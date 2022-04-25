namespace Parser
{
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
    using AngleSharp.Dom;
    using AngleSharp.Html.Parser;

    public static class Parser
    {
        public const string BaseURL = "https://www.toy.ru/";

        public const string Moscow = "77000000000";

        public const string SaintPetersburg = "78000000000";

        public const string Rostov = "61000001000";

        public static string CurrentCity = Parser.Moscow;

        private static HtmlParser htmlParser = new ();

        public static async Task<int> GetPageCount()
        {
            // Parsion html
            var html = await Parser.GetRequest(new Url("catalog/boy_transport/?count=45", Parser.BaseURL));
            if (html == null)
            {
                return -1;
            }

            var parsedHtml = Parser.htmlParser.ParseDocument(html);

            var paginationBlock = parsedHtml.GetElementsByClassName("pagination justify-content-between")[0];
            return int.Parse(paginationBlock.GetElementsByClassName("page-item")[^2].TextContent.Trim());
        }

        public static async Task<List<Url>> GetLinks(int pageNum)
        {
            var links = new List<Url>();

            // Parsing html
            var html = await Parser.GetRequest(new Url($"catalog/boy_transport/?count=45&PAGEN_8={pageNum}", Parser.BaseURL));
            if (html == null)
            {
                return links;
            }

            var parsedHtml = Parser.htmlParser.ParseDocument(html);

            // Getting product block
            var blocks = parsedHtml.GetElementsByClassName("col-12 col-sm-6 col-md-6 col-lg-4 col-xl-4 my-2");
            foreach (var block in blocks)
            {
                var link = new Url(
                    block.GetElementsByClassName("d-block p-1 product-name gtm-click")[0].GetAttribute("href"),
                    Parser.BaseURL);
                links.Add(link);
            }

            return links;
        }

        public static async Task<ProductData> ParsePage(Url url)
        {
            // Parsing html
            var html = await Parser.GetRequest(url);
            if (html == null)
            {
                return default;
            }

            var parsedHtml = Parser.htmlParser.ParseDocument(html);

            // Region
            var region = parsedHtml.GetElementsByClassName("col-12 select-city-link")[0].ChildNodes[3].TextContent.Trim();

            // Name
            var name = parsedHtml.GetElementsByClassName("detail-name")[0].TextContent;

            // Breadcrumbs
            var breadcrumbsBlock = parsedHtml.GetElementsByClassName("breadcrumb")[0];
            var breadcrumbs = string.Join(
                " - ",
                breadcrumbsBlock.GetElementsByTagName("a").Select(a => a.TextContent)) +
                $" - {name}";

            // Old & new price, availability
            var detailBlock = parsedHtml.GetElementsByClassName("detail-block border h-100 p-2")[0];
            var oldPrice = string.Empty;
            var price = string.Empty;
            var availability = false;
            if (detailBlock.GetElementsByClassName("net-v-nalichii").Length == 0)
            {
                availability = detailBlock.GetElementsByClassName("col-6 py-2")[1].GetElementsByClassName("ok").Length >= 1;
                price = Parser.ParsePrice(detailBlock.GetElementsByClassName("price")[0].TextContent);

                if (detailBlock.GetElementsByClassName("old-price").Length > 0)
                {
                    oldPrice = Parser.ParsePrice(detailBlock.GetElementsByClassName("old-price")[0].TextContent);
                }
            }

            // Links to images
            var imagesLink = parsedHtml
                .GetElementsByClassName("col-12 col-md-10 col-lg-7")[0]
                .GetElementsByTagName("a")
                .Select(img => img.GetAttribute("href"))
                .ToList();

            // Product link
            var productLink = url.Href;

            return new ProductData
            {
                Region = region,
                Breadcrumbs = breadcrumbs,
                Name = name,
                Availability = availability,
                OldPrice = oldPrice,
                Price = price,
                ImagesLink = imagesLink!,
                ProductLink = productLink,
            };
        }

        private static async Task<string?> GetRequest(Url url)
        {
            var handler = new HttpClientHandler() { UseCookies = true };
            handler.CookieContainer.Add(url, new Cookie("BITRIX_SM_city", Parser.CurrentCity));
            var client = new HttpClient(handler);

            var request = await client.GetAsync(url);
            if (request.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var byteArray = await request.Content.ReadAsByteArrayAsync();
            return Encoding.GetEncoding("windows-1251").GetString(byteArray);
        }

        private static string ParsePrice(string text)
            => new (text.Where(char.IsDigit).ToArray());
    }
}
