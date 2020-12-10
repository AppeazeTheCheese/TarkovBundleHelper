using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarkovBundleHelper
{
    static class ByteHelpers
    {
        private static readonly int[] Empty = new int[0];

        public static int[] Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            for (var i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        private static bool IsMatch(IReadOnlyList<byte> array, int position, IReadOnlyCollection<byte> candidate)
        {
            if (candidate.Count > (array.Count - position))
                return false;

            return !candidate.Where((t, i) => array[position + i] != t).Any();
        }

        private static bool IsEmptyLocate(IReadOnlyCollection<byte> array, IReadOnlyCollection<byte> candidate)
        {
            return array == null
                   || candidate == null
                   || array.Count == 0
                   || candidate.Count == 0
                   || candidate.Count > array.Count;
        }
    }
}
