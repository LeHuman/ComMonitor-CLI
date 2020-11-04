using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ComMonitor
{
    class JSON
    {
        public static Dictionary<long, string>[] getDataMap(string FilePath)
        {
            Dictionary<long, string> IDS;
            Dictionary<long, string> STRINGS;

            Dictionary<long, string>[] maps = new Dictionary<long, string>[2];

            using (StreamReader file = File.OpenText(FilePath))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JArray arr = (JArray)JToken.ReadFrom(reader);
                JObject J_TAGs = (JObject)arr[0];
                JObject J_IDs = (JObject)arr[1];
                IDS = J_TAGs.ToObject<Dictionary<string, long>>().ToDictionary(x => x.Value, x => x.Key);
                STRINGS = J_IDs.ToObject<Dictionary<string, long>>().ToDictionary(x => x.Value, x => x.Key);
            }

            maps[0] = IDS;
            maps[1] = STRINGS;

            return maps;
        }

    }
}
