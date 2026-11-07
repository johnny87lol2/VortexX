namespace VortexX.Models
{
    public class GeoInfo
    {
        public string Status = "";
        public string Country = "";
        public string CountryCode = "";
        public string RegionName = "";
        public string City = "";
        public string Timezone = "";
        public string Isp = "";
        public string As = "";
        public string Query = "";
        public double Lat;
        public double Lon;
    }

    public class RobloxServerInfo
    {
        public string ServerIp;
        public string GameName;
        public string PlaceId;
        public string JobId;
    }

    public class RobloxVersion
    {
        public string ClientVersionUpload { get; set; }
    }
}
