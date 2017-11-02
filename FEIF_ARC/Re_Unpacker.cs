using System;
using System.IO;
using System.Linq;
using System.Text;
using GovanifY.Utility;

namespace FEIF_ARC
{
    class Re_Unpacker
    {
        /// <summary>
        /// 创建一个arc归档，传入一个文件夹，将文件夹中的所有文件写入arc归档中
        /// 不支持二级文件夹
        /// </summary>
        /// <param name="outdir"></param>
        /// <param name="newname"></param>
        public static void CreateFireEmblemArchive(string outdir, string newname)
        {
            Console.WriteLine("Creating archive {0}", Path.GetFileName(newname));

            FileStream newfilestream = File.Create(newname);

            //Let's get the number of files
            //获取文件夹中的所有文件
            string[] files = Directory.GetFiles(outdir);

            uint FileCount = (uint)files.Length;
            Console.WriteLine("{0} files detected!", FileCount);

            /**
             * 使用UTF-8编码写入
             */
            //var ShiftJIS = Encoding.GetEncoding(932);

            BinaryStream newFile = new BinaryStream(newfilestream);

            MemoryStream infos = new MemoryStream();
            BinaryWriter FileInfos = new BinaryWriter(infos);


            Console.WriteLine("Creating dummy header...");
            newFile.Write(0);//Dummy; file size

            //MetaOffset 0x4
            newFile.Write(0);//dummy should be MetaOffset
            newFile.Write(FileCount);
            newFile.Write(FileCount + 3);
            ///写入0x70个0字节
            byte nil = 0;
            for (int i = 0; i < 0x70; i++)
            {
                newFile.Write(nil);
            }
            ///文件索引
            int z = 0;
            ///将文件数据写入
            foreach (string fileName in files)
            {
                Console.WriteLine("Adding file {0}...", Path.GetFileName(fileName));
                byte[] filetoadd = File.ReadAllBytes(fileName);
                uint fileoff = (uint)newFile.Tell();
                newFile.Write(filetoadd);
                while ((int)newFile.Tell() % 128 != 0)
                {
                    newFile.Write(nil);
                }
                FileInfos.Write(0);//Name position
                FileInfos.Write(z);//FileIndex
                FileInfos.Write(filetoadd.Length);//Length of the file
                FileInfos.Write(fileoff - 0x80);//Data Offset - 0x80
                z++;
            }

            long countinfo = newFile.Tell();
            newFile.Write(files.Length);//Count is written there afaik
            long infopointer = newFile.Tell();
            Console.WriteLine("Adding dummy FileInfos...");
            /// 移动内存流指针到文件头部
            /// 写入信息
            infos.Seek(0, SeekOrigin.Begin);
            var infopos = newFile.Tell();
            newFile.Write(infos.ToArray());

            Console.WriteLine("Rewriting header...");
            long metapos = newFile.Tell();
            newFile.Seek(4, SeekOrigin.Begin);
            newFile.Write((uint)metapos - 0x20);

            newFile.Seek(metapos, SeekOrigin.Begin);

            Console.WriteLine("Adding FileInfos pointer...");
            for (int i = 0; i < files.Length; i++)
            {
                newFile.Write((uint)((infopointer + i * 16) - 0x20));
            }

            Console.WriteLine("Adding Advanced pointers...");

            newFile.Write((uint)0x60);
            newFile.Write(0);
            newFile.Write((uint)(countinfo - 0x20));
            newFile.Write((uint)5);
            newFile.Write((uint)(countinfo + 4 - 0x20));
            newFile.Write((uint)0xB);
            for (int i = 0; i < files.Length; i++)
            {
                newFile.Write((uint)((countinfo + 4) + i * 16) - 0x20);

                //Second pointer is a bit more complicated
                if (i == 0)
                {
                    newFile.Write((uint)0x10);
                }
                else
                {
                    if (i == 1)
                    {
                        newFile.Write((uint)0x1C);
                    }
                    else
                    {
                        newFile.Write((uint)(0x1C + (10 * (i - 1))));//Currently this pointer is unknown, so we assume blindly that a basic pattern is correct
                    }
                }
            }

            //This need to be reversed!
            //0, 5, 0B, 10, 1C, 26, 30, 3A, 44, 4E
            //+5, +6, +4, +12, +10, +10, +10, +10, +10

            Console.WriteLine("Adding Filenames...");
            var datcount = new byte[] { 0x44, 0x61, 0x74, 0x61, 0x00, 0x43, 0x6F, 0x75, 0x6E, 0x74, 0x00, 0x49, 0x6E, 0x66, 0x6F, 0x00 };
            newFile.Write(datcount);
            int y = 0;

            foreach (string fileName in files)
            {
                FileInfos.Seek(y * 16, SeekOrigin.Begin);
                long namepos = newFile.Tell();
                FileInfos.Write((uint)namepos - 0x20);
                newFile.Write(Encoding.UTF8.GetBytes(Path.GetFileName(fileName)));
                newFile.Write(nil);
                y++;
            }
            Console.WriteLine("Rewriting FileInfos...");
            newFile.Seek(infopos, SeekOrigin.Begin);

            infos.Seek(0, SeekOrigin.Begin);
            newFile.Write(infos.ToArray());

            Console.WriteLine("Finishing the job...");
            newFile.Seek(0, SeekOrigin.Begin);
            UInt32 newlength = (UInt32)newFile.BaseStream.Length;
            newFile.Write(newlength);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done!");
            newFile.Close();

            Console.ReadKey();
        }
        /// <summary>
        /// 从一个arc归档解出所有文件
        /// </summary>
        /// <param name="outdir"></param>
        /// <param name="archive"></param>
        public static void ExtractFireEmblemArchive(string outdir, byte[] archive)
        {
            if (Directory.Exists(outdir)) { Directory.Delete(outdir, true); }
            Directory.CreateDirectory(outdir);

            //Encoding ShiftJIS = Encoding.GetEncoding(932);
            //使用UTF-8编码

            //MetaOffset 0x4
            uint MetaOffset = BitConverter.ToUInt32(archive, 4) + 0x20;
            //FileCount 0x8
            uint FileCount = BitConverter.ToUInt32(archive, 0x8);

            //In case we're using awakening archive type
            bool awakening = (BitConverter.ToUInt32(archive, 0x20) != 0);

            Console.WriteLine("Extracting {0} files from {1} to {2}...", FileCount, Path.GetFileName(outdir.Substring(0, outdir.Length - 1)) + ".arc", Path.GetFileName(outdir.Substring(0, outdir.Length - 1)) + "/");

            for (int i = 0; i < FileCount; i++)
            {
                int FileMetaOffset = 0x20 + BitConverter.ToInt32(archive, (int)MetaOffset + 4 * i);
                int FileNameOffset = BitConverter.ToInt32(archive, FileMetaOffset) + 0x20;
                int FileIndex = BitConverter.ToInt32(archive, FileMetaOffset + 4);
                uint FileDataLength = BitConverter.ToUInt32(archive, FileMetaOffset + 8);
                int FileDataOffset = BitConverter.ToInt32(archive, FileMetaOffset + 0xC) + (awakening ? 0x20 : 0x80);
                byte[] file = new byte[FileDataLength];
                Array.Copy(archive, FileDataOffset, file, 0, FileDataLength);

                string filename = Encoding.UTF8.GetString(archive.Skip(FileNameOffset).TakeWhile(b => b != 0).ToArray());
                Console.WriteLine();
                Console.WriteLine(filename);
                Console.WriteLine("FileIndex:" + FileIndex);
                Console.WriteLine("FileDataLength:" + FileDataLength);
                Console.WriteLine("FileNameOffset:" + FileNameOffset);
                Console.WriteLine("FileMetaOffset:" + FileMetaOffset);

                string outpath = outdir + Encoding.UTF8.GetString(archive.Skip(FileNameOffset).TakeWhile(b => b != 0).ToArray());

                if (!Directory.Exists(Path.GetDirectoryName(outpath))) { Directory.CreateDirectory(Path.GetDirectoryName(outpath)); }
                File.WriteAllBytes(outpath, file);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Complete!");
            Console.ReadKey();
        }
    }
}
