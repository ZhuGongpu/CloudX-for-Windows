using System.Collections.Generic;

namespace CloudX.Models
{
    public class Music
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Location { get; set; }
        public List<Album> Albums { get; set; }

        public static Music convertFileURLToMusicItem(string url)
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
            var addMusic = new Music {Artist = Artist, Location = Locate, Name = Name};
            return addMusic;
        }
    }
}