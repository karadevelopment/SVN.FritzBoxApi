using System.Net.NetworkInformation;

namespace SVN.FritzBoxApi.Logic
{
    internal static class Utils
    {
        public static string GetMacAddress()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet && ni.OperationalStatus == OperationalStatus.Up)
                {
                    return ni.GetPhysicalAddress().ToString();
                }
            }
            return null;
        }
    }
}