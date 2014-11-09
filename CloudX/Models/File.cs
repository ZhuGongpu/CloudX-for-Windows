using System.Runtime.InteropServices;
using System.Security;

namespace CloudX.Models
{
    public class File
    {
        public string Name { get; set; }
        public string Format { get; set; }
        public string Location { get; set; }
        public int fileTag { get; set; }
        public string typeImgLocation { get; set; }

        public static File convertFileURLToFileItem(string url)
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
            string Name = url.Substring(dividePoint + 1, len - dividePoint - 1);
            for (int i = len - 1; i >= 0; i--)
            {
                Name.Remove(Name.Length - 1);
                if (url[i] == '.')
                {
                    dividePoint = i;
                    break;
                }
            }
            string format = url.Substring(dividePoint + 1, len - dividePoint - 1);
            var addFile = new File {Format = format, Location = Locate, Name = Name};

            addFile.typeImgLocation = @"/Asset/folder.png";
            addFile.fileTag = Locate.GetHashCode() + Name.GetHashCode();
            return addFile;
        }
    }
}