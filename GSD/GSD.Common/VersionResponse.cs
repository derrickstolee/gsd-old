using Newtonsoft.Json;

namespace GSD.Common
{
    public class VersionResponse
    {
        public string Version { get; set; }

        public static VersionResponse FromJsonString(string jsonString)
        {
            return JsonConvert.DeserializeObject<VersionResponse>(jsonString);
        }
    }
}
