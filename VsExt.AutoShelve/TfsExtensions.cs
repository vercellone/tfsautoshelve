using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VsExt.AutoShelve
{
    public static class TfsExtensions
    {
        public static bool DifferFrom(this PendingChange[] changes, PendingChange[] shelvedchanges)
        {
            var recentChanges = changes.OrderByDescending(c => GetLastChangeDate(c)).Take(10);
            var shelveditems = shelvedchanges.ToList().ToDictionary(c => c.ServerItem);
            foreach (var change in recentChanges)
            {
                if (!shelveditems.ContainsKey(change.ServerItem) || !shelveditems[change.ServerItem].UploadHashValue.SequenceEqual(GetMD5HashValue(change)))
                {
                    return true;
                }
            }
            return false;
        }

        private static DateTime GetLastChangeDate(PendingChange change)
        {
            if (change.IsDelete)
            {
                return change.CreationDate;
            }
            else
            {
                return File.GetLastWriteTime(change.LocalItem);
            }
        }

        private static byte[] GetMD5HashValue(PendingChange change)
        {
            if (change.IsDelete || !File.Exists(change.LocalItem) )
            {
                return change.UploadHashValue;
            }
            else
            {
                return MD5.Create().ComputeHash(File.ReadAllBytes(change.LocalItem));
            }
        }

        public static string GetDomain(this string name)
        {
            int stop = name.IndexOf("\\");
            return (stop > -1) ? name.Substring(0, stop) : string.Empty;
        }

        public static string GetLogin(this string name)
        {
            int stop = name.IndexOf("\\");
            return name.Substring(stop + 1);
            //return (stop > -1) ? name.Substring(stop + 1, name.Length - stop - 1) : string.Empty;
        }

    }
}
