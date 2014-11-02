namespace CloudX.Models
{
    public class Movie
    {
        public int ArtistId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Artist { get; set; }

        public static Movie convertFileURLToMovieItem(string url)
        {
            int len = url.Length, dividePoint = 0;
            bool findName = false;
            for (int i = len - 1; i >= 0; i--)
            {
                if (url[i] == '\\' && !findName)
                {
                    dividePoint = i;
                    break;
                }
            }
            string Locate = url.Substring(0, dividePoint);
            string Artist = "";
            string Name = url.Substring(dividePoint + 1, len - dividePoint - 1);
            var addMovie = new Movie {Artist = Artist, Location = Locate, Name = Name};
            return addMovie;
        }
    }
}