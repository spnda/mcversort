using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace mcversort {
    class mcversort {
        static void Main(string[] args) {
            string customPath = null;
            bool checks = false;
            bool verbose = false;
            
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
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                    Console.WriteLine("mcversort - " + fileVersionInfo.ProductVersion);
                }
            }

            List<string> versions = Directory.GetDirectories(customPath ?? Util.GetVersionPath()).ToList<string>();
            if (versions.Count == 0) {
                Console.WriteLine("Warning: No versions found.");
                return;
            }
            foreach (string version in versions) {
                Console.WriteLine(version);
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
                    // Because these third party minecraft versions sometimes use seemingly random release times in their JSON we will need to assign new ones.
                    // We also want future proofing for new versions to come
                    // Because of this, we will use the version numbers to add to the base release time of the base version.
                    minecraftVersion.ReleaseTime = minecraftVersion.Time = minecraftVersion.InheritingVersion.ReleaseTime;
                    //minecraftVersion.ReleaseTime = minecraftVersion.Time = FixVersionTime(minecraftVersion);
                    File.WriteAllText(jsonLocation, ApplyChangesToJson(minecraftVersion, jsonString));
                } catch (Exception e) {
                    Console.WriteLine("An error occured. Skipping file.");
                    if (verbose) Console.WriteLine(e.ToString());
                }
            }
        }

        private static DateTime FixVersionTime(MinecraftVersion minecraftVersion) {
            DateTime versionStringDateTime = GetDateTimeForVersionString(minecraftVersion);
            if (versionStringDateTime.Millisecond == 0) return minecraftVersion.ReleaseTime;
            return Util.AddDateTime(minecraftVersion.InheritingVersion.ReleaseTime, versionStringDateTime);
        }

        private static DateTime GetDateTimeForVersionString(MinecraftVersion minecraftVersion) {
            int offset = (int)minecraftVersion.VersionType * 10;
            switch (minecraftVersion.VersionType) {
                case VersionType.FORGE: {
                        // For example 1.16.1-forge-32.0.75
                        // - 75 will be our new milliseconds
                        // - 32 + 0 will be our new seconds
                        // - 16 + 1 will be our new minutes
                        string[] split = minecraftVersion.Id.Split(".");
                        return new DateTime(0)
                            .AddMilliseconds(double.Parse(split.Last()))
                            .AddSeconds(double.Parse(split[2].Split("-").Last()) + double.Parse(split[split.Length - 2]))
                            .AddMinutes(double.Parse(split[1]) + double.Parse(split[2].Split("-")[0]));
                    }
                case VersionType.OPTIFINE: {
                        // For example 1.16.1-OptiFine_HD_U_G2_pre10
                        // - pre10 will be our new milliseconds (Release versions will be 100 milliseconds newer than preview versions
                        // - G2 (7 + 2) will be our new seconds
                        // - 16 + 1 will be our new minutes
                        string[] split = minecraftVersion.Id.Split(".");
                        string[] opti = split.Last().Split("_");
                        return new DateTime(0)
                            .AddMilliseconds(opti.Last().Contains("pre") ? double.Parse(Regex.Replace(opti.Last(), @"\D+", "")) : (double.Parse(opti.Last()) + 100))
                            .AddSeconds(Util.ConvertStringToInt(opti[3]))
                            .AddMinutes(double.Parse(split[1]) + double.Parse(split[2].Split("-")[0]));
                    }
                default: return new DateTime(0); // If we have a unknown version, don't change it.
            }
        }

        private static VersionType GetVersionTypeForVersion(MinecraftVersion version) {
            if (version?.Id == null) return VersionType.VANILLA;
            if (version.Id.Contains("OptiFine")) return VersionType.OPTIFINE;
            else if (version.Id.Contains("forge")) return VersionType.FORGE;
            else return VersionType.VANILLA;
        }

        private static string ApplyChangesToJson(MinecraftVersion minecraftVersion, string jsonString) {
            dynamic json = JsonConvert.DeserializeObject(jsonString);
            json["time"] = json["releaseTime"] = minecraftVersion.Time;
            return JsonConvert.SerializeObject(json);
        }
    }
}
