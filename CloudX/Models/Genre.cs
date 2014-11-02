using System.Collections.Generic;

namespace CloudX.Models
{
    public class Genre
    {
        public int GenreId { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public List<Album> Albums { get; set; }
    }
}