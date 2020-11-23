using LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace sakunaTool
{
    class sakunaTool
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }
            else if (args[0] == "-e") ArcExtract(args[1]);
            else if (args[0] == "-l") List(args[1]);
            else if (args[0] == "-p")
            {
                if (args.Length == 0)
                {
                    PrintUsage();
                    return;
                }
                else
                {
                    ArcPack(args[1], args[2], true);
                }
            }
        }

        static void ArcExtract(string arcFile)
        {
            var tempFolder = Path.GetTempPath();
            var tempFile = $@"{tempFolder}\lz4.tmp";
            var input = File.ReadAllBytes(arcFile);
            using (var reader = new BinaryReader(File.OpenRead(arcFile)))
            {
                var fileNameWO = Path.GetFileNameWithoutExtension(arcFile);
                var filenameSize = 96;
                var header = reader.ReadInt32();
                var version = reader.ReadInt16();
                var compression = reader.ReadInt16(); //00 - decrypted, 02 - LZ4
                var filesCount = reader.ReadInt32();
                var uSize = reader.ReadInt32();
                var savePosF = reader.BaseStream.Position;
                var dataStartOffset = (filenameSize + 16) * filesCount + 16;
                var dataLength = input.Length - dataStartOffset;

                reader.BaseStream.Seek(dataStartOffset, SeekOrigin.Begin);
                var data = reader.ReadBytes(dataLength);

                if (compression == 2)
                {
                    Console.WriteLine("DEBUG: Compression type: LZ4");
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
                else if (compression == 0)
                {
                    Console.WriteLine("DEBUG: Compression type: no compression");
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
        static byte[] CompressLZ4(byte[] inputArray)
        {
            var cData = LZ4.LZ4Codec.EncodeHC(inputArray, 0, inputArray.Length);
            return cData;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Sakuna of Rice and Ruin .arc extraction tool v0.2 (Edelweiss Engine)");
            Console.WriteLine("Created by LinkOFF");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine(" sakunaTool.exe [argument] <archive>");
            Console.WriteLine("");
            Console.WriteLine("Arguments:");
            Console.WriteLine(" -e:      Extracts all files");
            Console.WriteLine(" -p:      Packages a folder to a ARC");
            Console.WriteLine(" -l:      List all files in archive");
            Console.WriteLine("");
            Console.WriteLine("Additional parameters:");
            Console.WriteLine(" -nocompress:      Do not compress data during repack. Compress is on by default.");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine(@" sakunaTool.exe -e data01.arc             Extracts contents of data01.arc to folder /data01/");
            Console.WriteLine(@" sakunaTool.exe -p data01.arc data01\     Packs the contents of data01 to data01.cpk");
        }

        static long GetDirectorySize(string p)
        {
            string[] a = Directory.GetFiles(p, "*.*", SearchOption.AllDirectories);
            long b = 0;
            foreach (string name in a)
            {
                FileInfo info = new FileInfo(name);
                b += info.Length;
            }
            return b;
        }

        static int GetSize(string fileList)
        {
            FileInfo info = new FileInfo(fileList);
            return Convert.ToInt32(info.Length);
        }

        static void ArcPack (string inputDir, string arcFile, bool compress)
        {
            //WORK IN PROGRESS!!! Not working yet!
            int index = inputDir.Length + 1;
            int offset = 0;
            string[] fileList = Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories);
            byte[] header = Encoding.UTF8.GetBytes("TGP0");
            Int16 version = 3;
            Int16 lz4Compression = 2;
            Int16 noCompression = 0;
            Int32 filesCount = fileList.Length;
            Int32 uncompressedSize = (int)GetDirectorySize(inputDir);

            var streams = new List<Stream>();
            foreach (var file in fileList)
            {
                FileStream fs = new FileStream(file, FileMode.Open);
                streams.Add(fs);
            }
            var uncompressedData = ReadToEnd(new CombinationStream(streams));

            using (BinaryWriter writer = new BinaryWriter(new FileStream(arcFile, FileMode.Create)))
            {
                Console.WriteLine("Compressing...");
                writer.Write(header);
                writer.Write(version);
                if (compress) writer.Write(lz4Compression);
                else if(!compress) writer.Write(noCompression);
                writer.Write(filesCount);
                writer.Write(uncompressedSize);

                for (int i = 0; i < filesCount; i++)
                {
                    string fileName = fileList[i].Remove(0, index);
                    byte[] fileNameArray = Encoding.UTF8.GetBytes(fileName);
                    byte[] stringSize = new byte[96];
                    Array.Copy(fileNameArray, 0, stringSize, 0, fileNameArray.Length);
                    writer.Write(stringSize);
                    int size = GetSize(fileList[i]);
                    if (i != 0) offset += size; // try to get offset

                    writer.Write(offset); // Here is offset but i can't get it
                    writer.Write(size);
                    writer.Write(1);
                    writer.Write(0);
                }
                if (compress)
                {
                    byte[] compressedData = CompressLZ4(uncompressedData);
                    writer.Write(compressedData);
                }
                else if (!compress)
                {
                    writer.Write(uncompressedData);
                }
                Console.WriteLine("Done!");
            }
        }

        public static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        static void List (string arcFile)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(arcFile));
            reader.BaseStream.Seek(0x08, SeekOrigin.Begin);
            var files = reader.ReadInt32();
            reader.BaseStream.Seek(0x10, SeekOrigin.Begin);
            string[] filenames = GetFilenames(reader, files);
            foreach (var filename in filenames)
            {
                Console.WriteLine(filename.ToString());
            }
        }

        static string[] GetFilenames(BinaryReader reader, int files)
        {
            int singleBlockSize = 96;
            string[] filenames = new string[files];
            for (int i = 0; i < files; i++)
            {
                var filename = Regex.Replace(Encoding.UTF8.GetString(reader.ReadBytes(singleBlockSize)), @"\0", "");
                reader.BaseStream.Seek(reader.BaseStream.Position + 16, SeekOrigin.Begin);
                filenames[i] = filename;
            }
            return filenames;
        }
    }
}
