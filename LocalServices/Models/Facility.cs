using System.ComponentModel.DataAnnotations.Schema;

namespace LocalServices.Models
{
    public class Facility
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public List<Service> Services { get; set; } = new();
    }
}
