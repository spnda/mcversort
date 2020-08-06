using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace mcversort {
    public class NaturalComparer : Comparer<string> {
        public override int Compare(string x, string y) {
            if (x == y) return 0;

            string[] x1, y1;
            x1 = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
            y1 = Regex.Split(y.Replace(" ", ""), "([0-9]+)");

            for (int i = 0; i < x1.Length && i < y1.Length; i++)
                if (x1[i] != y1[i]) {
                    if (int.TryParse(x1[i], out int x2) && int.TryParse(y1[i], out int y2))
                        return x2.CompareTo(y2);

                    return x1[i].CompareTo(y1[i]);
                }

            if (y1.Length > x1.Length) return 1;
            else if (x1.Length > y1.Length) return -1;
            else return 0;
        }
    }
}
