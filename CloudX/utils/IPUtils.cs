using System.Linq;
using System.Net;

namespace CloudX.utils
{
    internal class IPUtils
    {
        public static string GetHostIP()
        {
            IPHostEntry ipe = Dns.GetHostEntry(Dns.GetHostName());

            return
                (from ipAddress in ipe.AddressList
                    where ipAddress.ToString().StartsWith("192.168")
                    select ipAddress.ToString()).FirstOrDefault();
        }
    }
}