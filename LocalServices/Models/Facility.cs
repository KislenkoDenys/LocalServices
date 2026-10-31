using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LocalServices.Models
{
    public class Facility
    {
        public int Id { get; set; }

        [Display(Name = "Назва")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Місто")]
        public string? City { get; set; }

        [Display(Name = "Вулиця та номер будинку")]
        public string? Street { get; set; }

        [Display(Name = "Опис")]
        public string? Description { get; set; }

        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [Display(Name = "Години роботи")]
        public string? WorkingHours { get; set; }

        [Display(Name = "Широта (Latitude)")]
        public double? Latitude { get; set; }

        [Display(Name = "Довгота (Longitude)")]
        public double? Longitude { get; set; }

        [Display(Name = "Зображення")]
        public string? ImagePath { get; set; }

        [NotMapped]
        [JsonIgnore]
        [Display(Name = "Файл зображення")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Категорія")]
        [Range(1, int.MaxValue, ErrorMessage = "Будь ласка, оберіть категорію зі списку.")]
        public int CategoryId { get; set; }

        [Display(Name = "Категорія")]
        public Category? Category { get; set; }

        [Display(Name = "Послуги")]
        public List<Service> Services { get; set; } = new();
    }
}
