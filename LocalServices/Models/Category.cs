using System.ComponentModel.DataAnnotations;

namespace LocalServices.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Display(Name = "Назва")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Опис")]
        public string? Description { get; set; }

        [Display(Name = "Заклади")]
        public List<Facility> Facilities { get; set; } = new();
    }
}