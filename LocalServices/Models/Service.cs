using System.ComponentModel.DataAnnotations;

namespace LocalServices.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Display(Name = "Назва")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Ціна")]
        public decimal Price { get; set; }

        [Display(Name = "Заклад")]
        [Range(1, int.MaxValue, ErrorMessage = "Будь ласка, оберіть заклад зі списку.")]
        public int FacilityId { get; set; }

        [Display(Name = "Заклад")]
        public Facility? Facility { get; set; }
    }
}
