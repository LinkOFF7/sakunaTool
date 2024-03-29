﻿using LZ4;
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
            if (args == null || args.Length < 1)
            {
                PrintUsage();
                return;
            }
            else if (args[0] == "-e")
            {
                ArcExtract(args[1]);
                return;
            }
            else if (args[0] == "-i") 
            {
                Info(args[1]);
                return;
            } 
            else if (args[0] == "-p")
            {
                if (args[3] == "-compress")
                {
                    ArcPack(args[1], args[2], true);
                    return;
                }
                else if (args[3] == "-nocompress")
                {
                    ArcPack(args[1], args[2], false);
                    return;
                }
            }
            else
            {
                string ext = Path.GetFileNameWithoutExtension(args[0]);
                if(ext == ".arc")
                {
                    ArcExtract(args[0]);
                    return;
                }
            }
        }

        static void ArcExtract(string arcFile)
        {
            var fileLength = File.ReadAllBytes(arcFile).Length;
            var fileNameWO = Path.GetFileNameWithoutExtension(arcFile);

            using (var ms = new MemoryStream())
            using (var lz4buffer = new BinaryWriter(ms))
            using (var reader = new BinaryReader(File.OpenRead(arcFile)))
            {
                var header = Encoding.UTF8.GetString(reader.ReadBytes(4));
                if (header != "TGP0")
                {
                    Console.WriteLine($@"Unexpected header: {header}");
                    return;
                }

                if (File.Exists($"{fileNameWO}_flag.vsn")) File.Delete($"{fileNameWO}_flag.vsn");
                if (File.Exists($"{fileNameWO}_sort.vsn")) File.Delete($"{fileNameWO}_sort.vsn");
                var filenameSize = 96;
                var version = reader.ReadInt16();
                var compression = reader.ReadInt16(); //00 - decrypted, 02 - LZ4
                var filesCount = reader.ReadInt32();
                var uSize = reader.ReadInt32();
                var savePosF = reader.BaseStream.Position;
                var dataStartOffset = (filenameSize + 16) * filesCount + 16;
                var dataLength = fileLength - dataStartOffset;

                reader.BaseStream.Seek(dataStartOffset, SeekOrigin.Begin);
                var data = reader.ReadBytes(dataLength);

                if (compression == 2)
                {
                    Console.WriteLine("DEBUG: Compression type: LZ4");
                    lz4buffer.Write(DecompressLZ4(data, uSize));
                    var dataReader = new BinaryReader(ms);
                    reader.BaseStream.Seek(savePosF, SeekOrigin.Begin);

                    for (int i = 0; i < filesCount; i++)
                    {
                        var filename = Encoding.UTF8.GetString(reader.ReadBytes(filenameSize));
                        filename = Regex.Replace(filename, @"\0", "");
                        var folderName = Path.GetDirectoryName(filename);
                        var offset = reader.ReadInt32();
                        var size = reader.ReadInt32();
                        var flag = reader.ReadInt32();
                        var unk2 = reader.ReadInt32();
                        CreateFlag(flag, fileNameWO);
                        var savePos = reader.BaseStream.Position;
                        dataReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        var file = dataReader.ReadBytes(size);
                        Directory.CreateDirectory($@"{fileNameWO}\{folderName}");
                        CreateSort(filename, fileNameWO);
                        Console.WriteLine($@"Extracting: {filename}");
                        File.WriteAllBytes($@"{fileNameWO}\{filename}", file);
                        reader.BaseStream.Seek(savePos, SeekOrigin.Begin);
                    }
                }
                else if (compression == 0)
                {
                    Console.WriteLine("DEBUG: Compression type: no compression");
                    lz4buffer.Write(data);
                    var dataReader = new BinaryReader(ms);
                    reader.BaseStream.Seek(savePosF, SeekOrigin.Begin);

                    for (int i = 0; i < filesCount; i++)
                    {
                        var filename = Encoding.UTF8.GetString(reader.ReadBytes(filenameSize));
                        filename = Regex.Replace(filename, @"\0", "");
                        var folderName = Path.GetDirectoryName(filename);
                        var offset = reader.ReadInt32();
                        var size = reader.ReadInt32();
                        var flag = reader.ReadInt32();
                        var unk2 = reader.ReadInt32();
                        CreateFlag(flag, fileNameWO);
                        var savePos = reader.BaseStream.Position;
                        dataReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        var file = dataReader.ReadBytes(size);
                        Directory.CreateDirectory($@"{fileNameWO}\{folderName}");
                        CreateSort(filename, fileNameWO);
                        Console.WriteLine($@"Extracting: {filename}");
                        File.WriteAllBytes($@"{fileNameWO}\{filename}", file);
                        reader.BaseStream.Seek(savePos, SeekOrigin.Begin);
                    }
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
            Console.WriteLine("Sakuna of Rice and Ruin .arc export and import tool v1.0");
            Console.WriteLine("by LinkOFF");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine(" sakunaTool.exe [argument] <archive> [additional parameter]");
            Console.WriteLine("");
            Console.WriteLine("Arguments:");
            Console.WriteLine(" -e:      Extracts all files");
            Console.WriteLine(" -p:      Packages a folder to a ARC");
            Console.WriteLine(" -i:      Info about archive");
            Console.WriteLine("");
            Console.WriteLine("Additional parameters:");
            Console.WriteLine(" -nocompress:      Do not compress data during import.");
            Console.WriteLine(" -compress:        Compress the data during import.");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine(@" sakunaTool.exe -e data01.arc                       Extracts contents of data01.arc to folder /data01/");
            Console.WriteLine(@" sakunaTool.exe -p data01 data01.arc -compress      Packs the contents of data01 to data01.cpk with compression");
            Console.WriteLine(@" sakunaTool.exe -i data01.arc                       Show information about archive (size, compression, files...)");
            Console.WriteLine("");
            Console.WriteLine($@"This tool using temp folder for keep flags and sort info.");
            Console.WriteLine($@"Path: {Path.GetTempPath()}Vsn_Sakuna\");
            Console.WriteLine($@"If you get message with flags replacing by '0' during pack, extract original");
            Console.WriteLine($@"arc archive once again! It created a flags and sort cache!");
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

        static uint GetSize(string fileList)
        {
            FileInfo info = new FileInfo(fileList);
            return Convert.ToUInt32(info.Length);
        }

        static string[] GetFileList(string localPath, string arcFile)
        {
            string arcFileWO = Path.GetFileNameWithoutExtension(arcFile);
            string[] fileList;
            string tempFile = $"{arcFileWO}_sort.Vsn";

            if (File.Exists(tempFile))
                return fileList = File.ReadAllLines(tempFile);
            else
                return fileList = Directory.GetFiles(localPath, "*.*", SearchOption.AllDirectories);
        }

        static void ArcPack (string inputDir, string arcFile, bool compress)
        {
            string arcFileWO = Path.GetFileNameWithoutExtension(arcFile);
            int index = inputDir.Length + 1;
            uint offset = 0;
            string[] fileList = GetFileList(inputDir, arcFile);
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
                writer.Write(header);
                writer.Write(version);
                if (compress) writer.Write(lz4Compression);
                else if(!compress) writer.Write(noCompression);
                writer.Write(filesCount);
                writer.Write(uncompressedSize);
                for (int i = 0; i < filesCount; i++)
                {
                    string flagFile = $"{arcFileWO}_flag.vsn";
                    string fileName = fileList[i].Remove(0, index);
                    byte[] fileNameArray = Encoding.UTF8.GetBytes(fileName);
                    byte[] stringSize = new byte[96];
                    Array.Copy(fileNameArray, 0, stringSize, 0, fileNameArray.Length);
                    writer.Write(stringSize);
                    uint size = GetSize(fileList[i]);
                    writer.Write(offset);
                    writer.Write(size);
                    if (File.Exists(flagFile))
                    {
                        string[] flags = File.ReadAllLines(flagFile);
                        writer.Write(int.Parse(flags[i]));
                    }
                    else
                    {
                        Console.WriteLine($@"{fileName}: Flag replaced by '0'!");
                        writer.Write(0);
                    }
                    writer.Write(0);
                    offset += size;
                }
                if (compress)
                {
                    Console.WriteLine("Compressing...");
                    byte[] compressedData = CompressLZ4(uncompressedData);
                    writer.Write(compressedData);
                }
                else if (!compress)
                {
                    Console.WriteLine("Repacking...");
                    writer.Write(uncompressedData);
                }
                Console.WriteLine("Done!");
            }
        }

        static byte[] ReadToEnd(Stream stream)
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

        static void Info (string arcFile)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(arcFile));
            var header = Encoding.UTF8.GetString(reader.ReadBytes(4));
            if (header != "TGP0")
            {
                Console.WriteLine($@"Incorrect magic: {header}. This tool works only with TGP0 magic!");
                return;
            }
            var version = reader.ReadInt16();
            var compression = reader.ReadInt16();
            var files = reader.ReadInt32();
            var uncompressedSize = reader.ReadInt32();

            Console.WriteLine($@"Magic:                         {header}");
            Console.WriteLine($@"Version:                       {version}");
            if (compression == 2) Console.WriteLine($@"Compression type:              Compressed (LZ4)");
            else if (compression == 0) Console.WriteLine($@"Compression type:              No compression");
            Console.WriteLine($@"Files:                         {files}");
            Console.WriteLine($@"Ucompressed data block size:   {uncompressedSize} bytes");
            Console.WriteLine("");
            Console.WriteLine($@"If you get message with flags replacing by '0' during pack, extract original");
            Console.WriteLine($@"arc archive once again! It created a flags and sort cache!");
        }

        static void CreateFlag(int flag, string fileName)
        {
            string tempFile = $"{fileName}_flag.vsn";
            StreamWriter StreamWriter = File.AppendText(tempFile);
            StreamWriter.WriteLine(flag);
            StreamWriter.Flush();
            StreamWriter.Close();
        }

        static void CreateSort(string sortString, string fileName)
        {
            string tempFile = $"{fileName}_sort.vsn";
            StreamWriter StreamWriter_ = File.AppendText(tempFile);
            StreamWriter_.WriteLine(fileName + "\\" + sortString);
            StreamWriter_.Flush();
            StreamWriter_.Close();
        }
    }
}
