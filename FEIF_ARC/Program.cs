using System;
using System.IO;

namespace FEIF_ARC
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage:Drop [floder] to import.");
                Console.WriteLine("Usage:Drop [*.arc] to extract.");
                return;
            }
            if (File.Exists(args[0]) && Path.GetExtension(args[0]).ToLower().Equals(".arc"))
            {
                /// 执行解包操作
                string path = args[0];
                byte[] data = File.ReadAllBytes(path);
                Re_Unpacker.ExtractFireEmblemArchive(Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(path) + Path.DirectorySeparatorChar, data);
                return;
            }
            if (Directory.Exists(args[0]))
            {
                /// 如果是目录，则执行封包操作
                string path = args[0];

                Re_Unpacker.CreateFireEmblemArchive(path, path + ".arc");
                return;
            }
            Console.WriteLine("不支持的文件:" + args[0]);
            Console.ReadKey();
            //if (args[0] == "extract")
            //{
            //    string path = args[1];
            //    byte[] data = File.ReadAllBytes(path);
            //    ExtractFireEmblemArchive(Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(path) + Path.DirectorySeparatorChar, data);
            //    return;
            //}
            //if (args[0] == "import")
            //{
            //    string path = args[1];

            //    CreateFireEmblemArchive(path, path + ".arc");
            //    return;
            //}
            //else
            //{
            //    Console.WriteLine("Unknown usage");
            //    return;
            //}
        }
        
    }
}
