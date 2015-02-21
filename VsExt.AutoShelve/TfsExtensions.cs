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
        public static bool HaveChanged(this PendingChange[] changes)
        {
            foreach (var change in changes)
            {
                if (!change.UploadHashValue.SequenceEqual(GetMD5HashValue(change)))
                {
                    return true;
                }
            }
            return false;
        }

        private static byte[] GetMD5HashValue(PendingChange change)
        {
            if (File.Exists(change.LocalItem))
            {
                return MD5.Create().ComputeHash(File.ReadAllBytes(change.LocalItem));
            }
            else
            {
                return change.HashValue;
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
