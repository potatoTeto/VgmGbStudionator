using System.IO;

namespace furGBVGMHeaderRemover
{
    static class Program
    {
        static readonly int[] Empty = new int[0];

        public static int[] Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }

        static void Main(string[] args)
        {
            string inDir = "input";
            string outDir = "output";

            if (!Directory.Exists(inDir))
                Directory.CreateDirectory(inDir);
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            var fIn = args[0];
            var fOut = Path.Combine(Path.GetDirectoryName(fIn), "..", outDir, Path.GetFileNameWithoutExtension(fIn) + ".vgm");

            var headerByteSequence = new byte[] { 0xB3, 0x02, 0x00, 0xB3, 0x04, 0x80, 0xB3, 0x07, 0x00, 0xB3, 0x09, 0x80, 0xB3, 0x0C, 0x00, 0xB3,
                                        0x0E, 0x80, 0xB3, 0x11, 0x00, 0xB3, 0x13, 0x80, 0xB3, 0x00, 0x00, 0xB3, 0x16, 0x8F, 0xB3, 0x15,
                                        0xFF, 0xB3, 0x14, 0x77 };

            byte[] data = File.ReadAllBytes(fIn);

            var positions = data.Locate(headerByteSequence);

            List<byte> newData = new List<byte>();

            for (int i = 0; i < data.Length; i++)
            {
                if (positions.Contains(i)) {
                    i += headerByteSequence.Length - 1;
                    continue;
                }
                newData.Add(data[i]);
            }

            File.WriteAllBytes(fOut, newData.ToArray());
        }
    }
}
