using Jellyfin.Plugin.M3UEditor.Entities;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.M3UEditor
{
    public static class Helper
    {
        public static List<M3UItem> ParseM3U(string m3uFile)
        {
            String[] m3uSplit = m3uFile.Split('\n');

            List<M3UItem> items = new List<M3UItem>();

            for (int i = 0; i < m3uSplit.Length; i++)
            {
                if (m3uSplit[i].Length == 0 || m3uSplit[i].Equals("#EXTM3U"))
                    continue;

                if (m3uSplit[i].StartsWith("#EXTINF"))
                {
                    //Parse info
                    M3UItem newItem = new M3UItem();
                    newItem.Attributes = new Dictionary<string, string>();
                    newItem.b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(m3uSplit[i]));
                    string[] m3uattrs = Regex.Split(m3uSplit[i], "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    newItem.ExtInf = m3uattrs[0];
                    foreach (string s in m3uattrs)
                    {
                        if (s.Contains("=") && s.Contains("\""))
                        {
                            Match mc = Regex.Match(s, "(.*?)=\"(.*?)\"");
                            if (mc.Groups.Count == 3)
                            {
                                newItem.Attributes.Add(mc.Groups[1].Value, mc.Groups[2].Value);
                            }
                        }
                    }
                    string[] namesplit = m3uSplit[i].Split("\",");
                    newItem.Name = namesplit[1].TrimStart();
                    newItem.Url = m3uSplit[i + 1];
                    items.Add(newItem);
                }
            }
            return items;
        }

        public static string sha256(string randomString)
        {
            var crypt = new SHA256Managed();
            string hash = String.Empty;
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(randomString));
            foreach (byte theByte in crypto)
            {
                hash += theByte.ToString("x2");
            }
            return hash;
        }
    }
}
