using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using CloudX.utils;

namespace CloudX.Models
{
    public class Album
    {
        public int AlbumId { get; set; }

        [DisplayName("Genre")]
        public int GenreId { get; set; }

        [DisplayName("Artist")]
        public int ArtistId { get; set; }

        public string Title { get; set; }

        public decimal Price { get; set; }

        [DisplayName("Album Art URL")]
        public string AlbumArtUrl { get; set; }

        public virtual Genre Genre { get; set; }
        public virtual Movie Artist { get; set; }
    }

    public static class SampleData
    {
        public static List<Genre> Genres { get; set; }
        public static List<Movie> Artists { get; set; }
        public static List<Music> Albums { get; set; }
        public static List<File> FileList { get; set; }

        public static void addMovie()
        {
            DataTable dataTable = SQLiteUtils.LoadData("movie");

            foreach (DataRow row in dataTable.Rows)
            {
                Movie movie = Movie.convertFileURLToMovieItem(row[0].ToString());
                Artists.Add(movie);
            }
        }

        public static void addMusic()
        {
            DataTable dataTable = SQLiteUtils.LoadData("music");

            foreach (DataRow row in dataTable.Rows)
            {
                Music music = Music.convertFileURLToMusicItem(row[0].ToString());
                Albums.Add(music);
            }
        }

        public static void addFile()
        {
            DataTable dataTable = SQLiteUtils.LoadData("file");

            foreach (DataRow row in dataTable.Rows)
            {
                File file = File.convertFileURLToFileItem(row[0].ToString());
                FileList.Add(file);
            }
        }


        public static void Seed()
        {
            if (Genres != null)
                return;

            Genres = new List<Genre>
            {
                new Genre {Name = "Rock"},
                new Genre {Name = "Jazz"},
                new Genre {Name = "Metal"},
                new Genre {Name = "Alternative"},
                new Genre {Name = "Disco"},
                new Genre {Name = "Blues"},
                new Genre {Name = "Latin"},
                new Genre {Name = "Reggae"},
                new Genre {Name = "Pop"},
                new Genre {Name = "Classical"}

                ////// todo //////
            };

            Artists = new List<Movie>();

            Albums = new List<Music>();

            FileList = new List<File>();

            addMovie();
            addMusic();
            addFile();
        }
    }
}