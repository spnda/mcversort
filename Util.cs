using System;
using System.Collections.Generic;
using System.Text;

namespace mcversort {
    internal class Util {
        internal static String GetVersionPath() {
            if (IsLinux) return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.minecraft/versions";
            else return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/.minecraft/versions";
        }

        public static bool IsLinux {
            get {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        public static DateTime AddDateTime(DateTime a, DateTime b) {
            return a.AddMilliseconds(b.Millisecond);
        }

        public static int ConvertStringToInt(string str) {
            int ret = 0;
            foreach (char c in str) {
                ret += char.ToUpper(c) - 64;
            }
            return ret;
        }
    }
}
