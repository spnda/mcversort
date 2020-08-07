using Newtonsoft.Json;
using System;
using System.IO;

namespace mcversort {
    class mcversort {
        static readonly string version = "0.1.1";

        static bool checks = false;
        static bool verbose = false;

        static void Main(string[] args) {
            string customPath = null;
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-p") {
                    if (args[i + 1] != null && Directory.Exists(args[i + 1])) {
                        customPath = args[i + 1];
                    } else {
                        Console.WriteLine("Error: No custom path or invalid path specified.");
                    }
                } else if (args[i] == "-c") {
                    checks = true;
                } else if (args[i] == "-v") {
                    verbose = true;
                } else if (args[i] == "-h") {
                    Console.WriteLine("mcversort - " + version);
                    Console.WriteLine("-h\n    Shows this help message.");
                    Console.WriteLine("-v\n    Verbose output.");
                    Console.WriteLine("-p <path>\n    Specify a custom path to your minecraft version folder. On Windows, this is usually %AppData%\\.minecraft\\versions.");
                    return;
                }
            }

            string[] versions = Directory.GetDirectories(customPath ?? Util.GetVersionPath());
            Array.Sort(versions, new NaturalComparer());
            if (versions.Length == 0) {
                Console.WriteLine("Warning: No versions found.");
                return;
            }
            foreach (string version in versions) {
                string jsonLocation = $"{version}/{Path.GetFileName(version)}.json";
                if (!File.Exists(jsonLocation)) continue;
                try {
                    string jsonString = File.ReadAllText(jsonLocation);
                    MinecraftVersion minecraftVersion = JsonConvert.DeserializeObject<MinecraftVersion>(jsonString);
                    if (minecraftVersion == null) continue;
                    minecraftVersion.VersionType = GetVersionTypeForVersion(minecraftVersion);
                    if (minecraftVersion.VersionType == VersionType.VANILLA) continue; // We don't want to modify vanilla versions
                    if (minecraftVersion.InheritsFrom != null && minecraftVersion.InheritsFrom != "") {
                        string inheritingJsonLocation = $"{Path.GetDirectoryName(version)}/{minecraftVersion.InheritsFrom}/{minecraftVersion.InheritsFrom}.json";
                        minecraftVersion.InheritingVersion = JsonConvert.DeserializeObject<MinecraftVersion>(File.ReadAllText(inheritingJsonLocation));
                    }
                    if (minecraftVersion.InheritingVersion == null) continue;
                    Console.WriteLine(minecraftVersion.Id);
                    // Because these third party minecraft versions sometimes use seemingly random release times in their JSON we will need to assign new ones.
                    // We also want future proofing for new versions to come
                    // We will get the position in the filesystem alphabetically and then calculate an appropiate offset.
                    minecraftVersion.ReleaseTime = minecraftVersion.Time = minecraftVersion.InheritingVersion.ReleaseTime.AddMinutes(GetOffsetByPositionInDirectory(versions, minecraftVersion.Id, minecraftVersion.InheritingVersion.Id));
                    File.WriteAllText(jsonLocation, ApplyChangesToJson(minecraftVersion, jsonString));
                } catch (Exception e) {
                    Console.WriteLine("An error occured. Skipping file.");
                    if (verbose) Console.WriteLine(e.ToString());
                }
            }
        }

        private static int GetOffsetByPositionInDirectory(string[] versions, string version, string inheritingVersion) {
            int inheritingPosition = -1, position = -1;
            for (int i = 0; i < versions.Length; i++) {
                if (Path.GetFileName(versions[i]) == version) {
                    position = i;
                } else if (Path.GetFileName(versions[i]) == inheritingVersion) {
                    inheritingPosition = i;
                }
            }
            if (inheritingPosition == -1 || position == -1) return 0;
            return position - inheritingPosition;
        }

        private static VersionType GetVersionTypeForVersion(MinecraftVersion version) {
            if (version?.Id == null) return VersionType.VANILLA;
            if (version.Id.Contains("OptiFine")) return VersionType.OPTIFINE;
            else if (version.Id.Contains("forge")) return VersionType.FORGE;
            else if (version.Id.Contains("fabric-loader")) return VersionType.FABRIC;
            else if (version.Id.Contains("LabyMod")) return VersionType.LABYMOD;
            else return VersionType.VANILLA;
        }

        private static string ApplyChangesToJson(MinecraftVersion minecraftVersion, string jsonString) {
            if (verbose) Console.WriteLine("Applying changes to output JSON for writing...");
            dynamic json = JsonConvert.DeserializeObject(jsonString);
            json["time"] = json["releaseTime"] = minecraftVersion.Time;
            return JsonConvert.SerializeObject(json);
        }
    }
}
