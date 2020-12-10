using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarkovBundleHelper
{
    public class Dependency
    {
        public string FileName => Path.GetFileName(FilePath);
        public string FilePath { get; }

        public Dependency(string filePath)
        {
            FilePath = filePath;
        }

        public static bool operator ==(Dependency d1, Dependency d2)
        {
            return d1?.Equals(d2) ?? false;
        }

        public static bool operator !=(Dependency d1, Dependency d2)
        {
            return !(d1 == d2);
        }

        public override bool Equals(object obj)
        {
            return GetHashCode() == obj?.GetHashCode();
        }

        protected bool Equals(Dependency other)
        {
            return FilePath == other.FilePath;
        }

        public override int GetHashCode()
        {
            return (FilePath != null ? FilePath.GetHashCode() : 0);
        }
    }
}
