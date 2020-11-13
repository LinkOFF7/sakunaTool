using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LZ4;

namespace sakunaTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) 
            {
                PrintUsage();
                return;
            } 
            else ArcFile(args[0]);
        }

        static void ArcFile(string inputFile)
        {
            var tempFolder = Path.GetTempPath();
            var tempFile = $@"{tempFolder}\lz4.tmp";
            var input = File.ReadAllBytes(inputFile);
            using (var reader = new BinaryReader(File.OpenRead(inputFile)))
            {
                var fileNameWO = Path.GetFileNameWithoutExtension(inputFile);
                var filenameSize = 96;
                var header = reader.ReadInt32();
                var version = reader.ReadInt16();
                var encryptionType = reader.ReadInt16(); //00 - decrypted, 02 - LZ4
                var filesCount = reader.ReadInt32();
                var uSize = reader.ReadInt32();
                var savePosF = reader.BaseStream.Position;
                var dataStartOffset = (filenameSize + 16) * filesCount + 16;
                var dataLength = input.Length - dataStartOffset;

                reader.BaseStream.Seek(dataStartOffset, SeekOrigin.Begin);
                var data = reader.ReadBytes(dataLength);

                if (encryptionType == 2)
                {
                    Console.WriteLine("Compression type: LZ4");
                    var uData = DecompressLZ4(data, uSize);
                    File.WriteAllBytes(tempFile, uData);
                    var dataFile = File.OpenRead(tempFile);
                    var dataReader = new BinaryReader(dataFile);
                    reader.BaseStream.Seek(savePosF, SeekOrigin.Begin);

                    for (int i = 0; i < filesCount; i++)
                    {
                        var filename = Encoding.UTF8.GetString(reader.ReadBytes(filenameSize));
                        filename = Regex.Replace(filename, @"\0", "");
                        var folderName = Path.GetDirectoryName(filename);
                        var offset = reader.ReadInt32();
                        var size = reader.ReadInt32();
                        var unk1 = reader.ReadInt32();
                        var unk2 = reader.ReadInt32();
                        var savePos = reader.BaseStream.Position;
                        dataReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        var file = dataReader.ReadBytes(size);
                        Directory.CreateDirectory($@"{fileNameWO}\{folderName}");
                        Console.WriteLine($@"Extracting: {filename}");
                        File.WriteAllBytes($@"{fileNameWO}\{filename}", file);
                        reader.BaseStream.Seek(savePos, SeekOrigin.Begin);
                    }
                    dataFile.Close();
                    File.Delete(tempFile);
                }
                else if (encryptionType == 0)
                {
                    Console.WriteLine("Compression type: no compression");
                    File.WriteAllBytes(tempFile, data);
                    var dataFile = File.OpenRead(tempFile);
                    var dataReader = new BinaryReader(dataFile);
                    reader.BaseStream.Seek(savePosF, SeekOrigin.Begin);

                    for (int i = 0; i < filesCount; i++)
                    {
                        var filename = Encoding.UTF8.GetString(reader.ReadBytes(filenameSize));
                        filename = Regex.Replace(filename, @"\0", "");
                        var folderName = Path.GetDirectoryName(filename);
                        var offset = reader.ReadInt32();
                        var size = reader.ReadInt32();
                        var unk1 = reader.ReadInt32();
                        var unk2 = reader.ReadInt32();
                        var savePos = reader.BaseStream.Position;
                        dataReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        var file = dataReader.ReadBytes(size);
                        Directory.CreateDirectory($@"{fileNameWO}\{folderName}");
                        Console.WriteLine($@"Extracting: {filename}");
                        File.WriteAllBytes($@"{fileNameWO}\{filename}", file);
                        reader.BaseStream.Seek(savePos, SeekOrigin.Begin);
                    }
                    dataFile.Close();
                    File.Delete(tempFile);
                }
            }
        }
        static byte[] DecompressLZ4(byte[] inputArray, int uSize)
        {
            var uData = LZ4Codec.Decode(inputArray, 0, inputArray.Length, uSize);
            return uData;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Sakuna of Rice and Ruin .arc extraction tool v0.2 (Edelweiss Engine)");
            Console.WriteLine("Created by LinkOFF");
            Console.WriteLine("");
            Console.WriteLine("Usage: sakunaTool.exe <archive.arc> (drag&drop .arc file)");
            Console.ReadKey();
        }
    }
}
