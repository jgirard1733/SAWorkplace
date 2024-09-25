namespace InternalItems.Models
{
#pragma warning disable CS1591
    /// <summary>
    /// Matches Swaggers SwaggerDoc model.
    /// SHOULD NOT NEED TO UPDATE THIS!
    /// </summary>
    public class ApiInformation
    {
        public string Title { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public Contact Contact { get; set; }

    }

    public class Contact
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Url { get; set; }
    }
#pragma warning restore CS1591
}
