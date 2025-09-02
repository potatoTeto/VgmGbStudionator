using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace VgmGbStudionator
{
    class VGMReader
    {
        public static void ReadVGM(string inputFilePath, string outputFilePath)
        {
            try
            {
                using (FileStream fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    // --- Step 1: Read and validate VGM header ---
                    // The VGM header is at least 0x40 bytes long.
                    // First 4 bytes must be the ASCII string "Vgm ".
                    byte[] header = reader.ReadBytes(0x40);
                    string sig = Encoding.ASCII.GetString(header, 0, 4);
                    if (sig != "Vgm ")
                    {
                        Console.WriteLine("Invalid VGM file.");
                        return;
                    }

                    // Version: 4-byte little-endian BCD version at 0x08
                    uint vgmVersion = BitConverter.ToUInt32(header, 0x08);
                    string vgmVerStr = $"{vgmVersion >> 8}.{vgmVersion & 0xFF:X2}";
                    
                    // Check for expected v1.72 VGM
                    if (vgmVerStr != "1.72")
                    {
                        Console.WriteLine($"[WARNING] Unsupported VGM version (Yours: v{vgmVerStr}; Expected: v1.72). Will convert, but no promises it'll work!");
                    } else
                    {
                        Console.WriteLine($"VGM Version: v{vgmVerStr}");
                    }

                    // EOF offset: relative to 0x04, points to end of file (size - 4).
                    uint eofOffset = BitConverter.ToUInt32(header, 0x04);
                    
                    // GD3 offset: relative to 0x14, points to embedded GD3 tag data (if present).
                    uint gd3Offset = BitConverter.ToUInt32(header, 0x14);

                    // Data offset: relative to 0x34, points to the start of the command stream.
                    // If this is zero, the command stream starts at 0x40.
                    uint dataOffset = BitConverter.ToUInt32(header, 0x34);
                    if (dataOffset == 0) dataOffset = 0x40;

                    // Compute absolute position in file where commands begin.
                    long commandStart = 0x34 + dataOffset;

                    // Copy all header + pre-command data into memory unchanged.
                    fs.Seek(0, SeekOrigin.Begin);
                    byte[] preCommandData = reader.ReadBytes((int)commandStart);

                    // --- Step 2: Process the VGM command stream ---
                    fs.Seek(commandStart, SeekOrigin.Begin);
                    List<byte> commandData = new List<byte>();

                    // Track which GB PSG registers have already been stripped once.
                    // We only skip the *first* write to each channel's init registers.
                    HashSet<byte> strippedRegisters = new HashSet<byte>();

                    while (fs.Position < fs.Length)
                    {
                        byte cmd = reader.ReadByte();

                        if (cmd == 0xB3 && fs.Position + 2 <= fs.Length)
                        {
                            // 0xB3 = Game Boy PSG register write
                            byte reg = reader.ReadByte(); // register ID
                            byte val = reader.ReadByte(); // register value

                            // If this is the very first write to an "init" register → skip it.
                            if (!strippedRegisters.Contains(reg) && IsGbInitRegister(reg))
                            {
                                strippedRegisters.Add(reg);
                                continue; // do not add these 3 bytes to output
                            }

                            // Otherwise, preserve this command.
                            commandData.Add(cmd);
                            commandData.Add(reg);
                            commandData.Add(val);
                        }
                        else
                        {
                            // Any other command type (timing waits, other chip writes, etc.)
                            // is copied directly without modification.
                            commandData.Add(cmd);
                        }
                    }

                    // --- Step 3: Rebuild the modified VGM file ---
                    List<byte> finalFile = new List<byte>();
                    finalFile.AddRange(preCommandData); // unchanged header + pre-command data
                    finalFile.AddRange(commandData);    // modified command stream

                    // --- Step 4: Fix header offsets ---
                    // Update EOF offset (0x04): must always equal file length - 4.
                    uint newFileLength = (uint)finalFile.Count;
                    uint newEofOffset = newFileLength - 4;
                    byte[] eofBytes = BitConverter.GetBytes(newEofOffset);
                    for (int i = 0; i < 4; i++)
                        finalFile[0x04 + i] = eofBytes[i];

                    // If GD3 metadata is present, adjust its offset to reflect removed bytes.
                    if (gd3Offset != 0)
                    {
                        uint oldFileLength = eofOffset + 4;
                        uint removedBytes = oldFileLength - newFileLength;
                        uint newGd3Offset = gd3Offset - removedBytes;

                        byte[] gd3Bytes = BitConverter.GetBytes(newGd3Offset);
                        for (int i = 0; i < 4; i++)
                            finalFile[0x14 + i] = gd3Bytes[i];
                    }

                    // --- Step 5: Save final stripped VGM file ---
                    File.WriteAllBytes(outputFilePath, finalFile.ToArray());
                    Console.WriteLine($"Saved stripped VGM to: {outputFilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading VGM: " + ex.Message);
            }
        }

        // Determines whether a given PSG register belongs to one of the
        // Game Boy’s sound channel init registers. We strip only the
        // *first* write to each of these registers.
        private static bool IsGbInitRegister(byte reg)
        {
            return (reg >= 0x10 && reg <= 0x14) || // Square 1 channel registers
                   (reg >= 0x16 && reg <= 0x19) || // Square 2 channel registers
                   (reg >= 0x1A && reg <= 0x1E) || // Wave channel registers
                   (reg >= 0x20 && reg <= 0x23) || // Noise channel registers
                   reg == 0x26;                    // NR52 (sound master enable)
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string inDir = "input";
            string outDir = "output";

            // Ensure the input and output directories exist before use
            if (!Directory.Exists(inDir))
                Directory.CreateDirectory(inDir);
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            // Require at least one argument: the path to the VGM file to process
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide the path to the VGM file as an argument.");
                return;
            }

            string inputFilePath = args[0];

            // Check that the given input file actually exists
            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine($"The file '{inputFilePath}' does not exist.");
                return;
            }

            // Build output path inside the "output" directory,
            // keeping original filename but forcing ".vgm" extension
            string outputFilePath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(inputFilePath) + ".vgm");

            // Process the input file and create the stripped VGM
            VGMReader.ReadVGM(inputFilePath, outputFilePath);
        }
    }
}
