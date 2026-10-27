namespace LocalServices.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int FacilityId { get; set; }
        public Facility? Facility { get; set; }
    }
}
