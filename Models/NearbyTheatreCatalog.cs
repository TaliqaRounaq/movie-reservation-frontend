namespace CineBook.Models
{
    public static class NearbyTheatreCatalog
    {
        public static List<Theatre> GetAll()
        {
            return new List<Theatre>
            {
                new Theatre { Id = 1, Name = "INOX Shivam", Area = "Residency Road" },
                new Theatre { Id = 2, Name = "Broadway Cinemas", Area = "Lal Chowk" },
                new Theatre { Id = 3, Name = "Cinepolis Bemina", Area = "Bemina Byepass" },
                new Theatre { Id = 4, Name = "PVR Sonwar", Area = "Sonwar Bagh" },
                new Theatre { Id = 5, Name = "Regal Talkies", Area = "Maisuma" }
            };
        }
    }
}
