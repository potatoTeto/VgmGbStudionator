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
                // Step 1: Open the input VGM file for reading
                using (FileStream fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    // Step 2: Read the VGM header and check the signature ("Vgm ")
                    byte[] header = reader.ReadBytes(4);
                    string headerStr = Encoding.ASCII.GetString(header);
                    if (headerStr != "Vgm ")
                    {
                        Console.WriteLine("Invalid VGM file: Incorrect header.");
                        return;
                    }

                    uint eofRelOffset = reader.ReadUInt32();  // Read the Relative offset to end of file (i.e. file length - 4). This is mainly used to find the next track when concatenating player stubs and multiple files.

                    // Step 3: Read the version number as 32-bit BCD (Expected: 1.72 or later)
                    uint version = reader.ReadUInt32();  // Read the 32-bit version as BCD

                    // Decode the BCD value into major and minor version
                    int decodedMinor = (int)(version & 0xFF);  // Extract the least significant byte (minor version)

                    // BCD decoding for the minor version
                    int decodedMinorBCD = ((decodedMinor >> 4) * 10) + (decodedMinor & 0x0F);  // Decode BCD

                    // Decode the major version from the second byte
                    int decodedMajor = (int)((version >> 8) & 0xFF);  // Extract the second byte for major version

                    // Display the version properly
                    Console.WriteLine($"VGM Version: {decodedMajor}.{decodedMinorBCD}");

                    // Handle version-specific logic
                    if (decodedMajor < 1 || (decodedMajor == 1 && decodedMinorBCD < 72))
                    {
                        Console.WriteLine($"Unsupported VGM version: {decodedMajor}.{decodedMinorBCD}");
                        return;
                    }

                    // Step 4: Read the header size
                    uint headerSize = reader.ReadUInt32();
                    Console.WriteLine("VGM Header Size: " + headerSize);

                    // Step 5: Calculate the size of initial data (before GB header)
                    int initialDataSize = 0x6f; // Offset where GB header starts
                    byte[] initialData = reader.ReadBytes(initialDataSize);

                    // Step 6: Read the remaining data (GB header and the rest of the file)
                    byte[] remainingData = reader.ReadBytes((int)(fs.Length - fs.Position));

                    // Step 7: Process the GB header portion
                    List<byte> gbHeaderData = new List<byte>(remainingData);

                    // We know that the first 0xB3 appears at offset 0x6f in the GB header, so we'll start removing those bytes
                    int removalCount = 0;
                    for (int i = 0; i < gbHeaderData.Count - 2 && removalCount < 12; i++)
                    {
                        // If 0xB3 command is found, remove it and the next two bytes (3-byte sequence)
                        if (gbHeaderData[i] == 0xB3)
                        {
                            gbHeaderData.RemoveRange(i, 3);  // Remove 3 bytes starting from the current index
                            removalCount++;

                            // After removing 3 bytes, adjust the loop index to stay on the correct byte
                            i -= 2;  // Skip the next two bytes that were just removed
                        }
                    }

                    // Output status for removal
                    Console.WriteLine($"Removed {removalCount} instances of 3-byte 0xB3 commands from the GB header.");

                    // Step 8: Combine the header, version, size, initial data, modified GB header data, and the rest of the file
                    List<byte> finalFileData = new List<byte>();
                    finalFileData.AddRange(header);  // Add the "Vgm " signature
                    finalFileData.AddRange(BitConverter.GetBytes(eofRelOffset));  // Add the EoF Relative Offset
                    finalFileData.AddRange(BitConverter.GetBytes(version));  // Add the separate minor and major version bytes
                    finalFileData.AddRange(BitConverter.GetBytes(headerSize));  // Add the header size
                    finalFileData.AddRange(initialData);  // Add the untouched initial data
                    finalFileData.AddRange(gbHeaderData);  // Add the modified GB header

                    // Step 9: Save the modified data to the output file
                    using (FileStream outputFs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                    using (BinaryWriter writer = new BinaryWriter(outputFs))
                    {
                        writer.Write(finalFileData.ToArray());  // Write the modified data
                    }

                    Console.WriteLine("VGM file modified and saved to: " + outputFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading VGM file: " + ex.Message);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string inDir = "input";
            string outDir = "output";

            // Ensure the input and output directories exist
            if (!Directory.Exists(inDir))
                Directory.CreateDirectory(inDir);
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            // Ensure a file path argument was passed
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide the path to the VGM file as an argument.");
                return;
            }

            string inputFilePath = args[0];

            // Check if the provided file exists
            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine($"The file '{inputFilePath}' does not exist.");
                return;
            }

            // Construct output file path
            string outputFilePath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(inputFilePath) + ".vgm");

            // Call the method to read and parse the VGM file
            VGMReader.ReadVGM(inputFilePath, outputFilePath);
        }
    }
}
