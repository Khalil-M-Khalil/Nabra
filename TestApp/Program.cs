
using System;
using System.Text;
using System.Collections.Generic;

class Program {
    static void Main() {
        var arToEn = new Dictionary<char, char>();
        var enToAr = new Dictionary<char, string>();
        var Layout = new (string En, string Ar)[] {
            ("q", "?"), ("w", "?"), ("e", "?"), ("r", "?"), ("t", "?"), ("y", "?"), ("u", "?"), ("i", "?"), ("o", "?"), ("p", "?"), ("[", "?"), ("]", "?"),
            ("a", "?"), ("s", "?"), ("d", "?"), ("f", "?"), ("g", "?"), ("h", "?"), ("j", "?"), ("k", "?"), ("l", "?"), (";", "?"), ("'", "?"),
            ("z", "?"), ("x", "?"), ("c", "?"), ("v", "?"), ("b", "\u0644\u0627"), ("n", "?"), ("m", "?"), (",", "?"), (".", "?"), ("/", "?")
        };
        foreach(var (en, ar) in Layout) {
            enToAr[en[0]] = ar;
        }
        string text = "(;dt;)";
        var sb = new StringBuilder();
        foreach(var c in text) {
            if(enToAr.TryGetValue(c, out var ar)) sb.Append(ar);
            else sb.Append(c);
        }
        Console.WriteLine(sb.ToString());
    }
}

