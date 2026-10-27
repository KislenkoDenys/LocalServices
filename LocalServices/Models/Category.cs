namespace LocalServices.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Facility> Facilities { get; set; } = new();
    }
}
