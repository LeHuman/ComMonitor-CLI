using Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terminal;

namespace MsgMap
{
    internal static class JSONMap
    {
        public static string Path;
        private static string MapBlocking;
        private static int blockingLength = 8;
        public static bool Loaded = false;
        public static Dictionary<long, string> IDs;
        public static Dictionary<long, string> Strings;

        public static void LoadJSONMap(string Path, string MapBlocking, int blockingLength)
        {
            if (Path == null)
                return;

            if (File.Exists(Path))
            {
                JSONMap.Path = Path;
                JSONMap.MapBlocking = MapBlocking;
                JSONMap.blockingLength = blockingLength;
                Dictionary<long, string>[] maps = getDataMap(Path);
                IDs = maps[0];
                Strings = maps[1];
                Loaded = true;
            }
            else
            {
                if (Path.Length != 0)
                    throw new SystemException($"JSON path does not exist: {Path}");
            }
        }

        public static void LogMap()
        {
            if (Loaded && FileLog.Available())
            {
                FileLog.Flush();
                bool tmstmp = FileLog.Timestamp;
                FileLog.Timestamp = false;
                using (StreamReader file = File.OpenText(Path))
                    FileLog.Log("---[ LOG MAP START ]---\n" + file.ReadToEnd() + "\n\n---[ LOG MAP END ]---\n");
                FileLog.Flush();
                FileLog.Timestamp = tmstmp;
            }
            else
            {
                Term.PrintLine("Message Map not printed");
            }
        }

        private static Dictionary<long, string>[] getDataMap(string FilePath)
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

        public static string GetMappedMessage(Span<byte> data)
        {
            // TODO: Implement custom blocking of messages
            long ID_key = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(0, 2));
            long String_key = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2, 2));
            long num = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4, 4));
            bool bad = false;
            if (!IDs.TryGetValue(ID_key, out string id))
            {
                id = "Bad ID";
                bad = true;
            }
            if (!Strings.TryGetValue(String_key, out string str))
            {
                str = "Bad String ID";
                bad = true;
            }
            if (!bad)
                FileLog.Log(ID_key.ToString() + " " + String_key.ToString() + " " + num.ToString() + "\n");
            return id + " " + str + " " + num + "\n";
        }
    }
}
