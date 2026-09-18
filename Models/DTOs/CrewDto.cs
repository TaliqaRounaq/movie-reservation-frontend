using CineBook.Models.Enums;

namespace CineBook.Models.DTOs
{
    public class CrewDto
    {
        public int CrewId { get; set; }
        public string Name { get; set; } = string.Empty;
        public CrewRole Role { get; set; }
    }
}