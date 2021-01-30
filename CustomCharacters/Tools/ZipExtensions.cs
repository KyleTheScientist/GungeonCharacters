using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;

namespace CustomCharacters
{
    internal static class ZipExtensions
    {
        public static string[] ReadAllLines(this ZipEntry zipEntry)
        {
            if (zipEntry == null)
                throw new ArgumentNullException(nameof(zipEntry));

            var lines = new List<string>();
            using (var stream = zipEntry.OpenReader())
            using (var reader = new StreamReader(stream))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines.ToArray();
        }

        public static byte[] ReadAllBytes(this ZipEntry zipEntry)
        {
            if (zipEntry == null)
                throw new ArgumentNullException(nameof(zipEntry));

            using (var ms = new MemoryStream())
            {
                zipEntry.Extract(ms);
                ms.Position = 0;
                return ms.ToArray();
            }
        }
    }
}
