namespace Parser
{
    public struct ProductData
    {
        public string Region { get; set; }

        public string Breadcrumbs { get; set; }

        public string Name { get; set; }

        public bool Availability { get; set; }

        public string OldPrice { get;set; }

        public string Price { get; set; }

        public List<string> ImagesLink { get; set; }

        public string ProductLink { get; set; }
    }
}
