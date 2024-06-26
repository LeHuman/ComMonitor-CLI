﻿using ComMonitor.Log;
using ComMonitor.Terminal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ComMonitor.MsgMap {

    public static class JSONMap {
        public static bool Loaded { get => _Loaded; }

        private static string Path;
        private static bool _Loaded;
        private static string MapBlocking;
        private static int blockingLength = 8;
        private static Dictionary<long, string> IDs, Strings;

        public static void LoadJSONMap(string Path, string MapBlocking, int blockingLength) {
            if (Path == null)
                return;

            if (File.Exists(Path)) {
                JSONMap.Path = Path;
                JSONMap.MapBlocking = MapBlocking;
                JSONMap.blockingLength = blockingLength;
                LoadDataMap(Path);
                _Loaded = true;
            } else {
                if (Path.Length != 0)
                    throw new SystemException($"JSON path does not exist: {Path}");
            }
        }

        private static void LoadDataMap(string FilePath) {
            using StreamReader file = File.OpenText(FilePath);
            using JsonTextReader reader = new(file);
            JArray arr = (JArray)JToken.ReadFrom(reader);
            JObject J_TAGs = (JObject)arr[0];
            JObject J_IDs = (JObject)arr[1];
            IDs = J_TAGs.ToObject<Dictionary<string, long>>().ToDictionary(x => x.Value, x => x.Key);
            Strings = J_IDs.ToObject<Dictionary<string, long>>().ToDictionary(x => x.Value, x => x.Key);
        }

        public static string GetMappedMessage(Span<byte> data) {
            // TODO: Implement custom blocking of messages
            long ID_key = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(0, 2));
            long String_key = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2, 2));
            long num = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4, 4));
            bool bad = false;
            if (!IDs.TryGetValue(ID_key, out string id)) {
                id = "Bad ID";
                bad = true;
            }
            if (!Strings.TryGetValue(String_key, out string str)) {
                str = "Bad String ID";
                bad = true;
            }
            if (!bad)
                FileLog.Log(ID_key.ToString() + " " + String_key.ToString() + " " + num.ToString() + "\n");
            return id + " " + str + " " + num;
        }

        public static void LogMap() {
            if (Loaded && FileLog.Available()) {
                FileLog.Flush();
                bool tmstmp = FileLog.TimestampEnabled;
                FileLog.TimestampEnabled = false;
                using (StreamReader file = File.OpenText(Path))
                    FileLog.Log("---[ LOG MAP START ]---\n" + file.ReadToEnd() + "\n\n---[ LOG MAP END ]---\n");
                FileLog.Flush();
                FileLog.TimestampEnabled = tmstmp;
            } else {
                Term.WriteLine("Message Map not printed");
            }
        }
    }
}
