using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FEIF_ARC
{
    class FEIF_ARC_Reader : IDisposable
    {
#region Static Function
        /// <summary>
        /// 获取arc归档中的指定文件数据
        /// </summary>
        /// <param name="arcPath">arc文件路径</param>
        /// <param name="filename">arc归档中的文件</param>
        /// <returns></returns>
        public static byte[] GetFileData(string arcPath, string filename)
        {
            byte[] archive = File.ReadAllBytes(arcPath);

            return GetFileData(archive, filename);
        }

        public static byte[] GetFileData(byte[] archive, string filename)
        {
            //MetaOffset 0x4
            uint MetaOffset = BitConverter.ToUInt32(archive, 4) + 0x20;
            //FileCount 0x8
            uint FileCount = BitConverter.ToUInt32(archive, 0x8);
            //In case we're using awakening archive type
            bool awakening = (BitConverter.ToUInt32(archive, 0x20) != 0);

            for (var i = 0; i < FileCount; i++)
            {
                int FileMetaOffset = 0x20 + BitConverter.ToInt32(archive, (int)MetaOffset + 4 * i);

                int FileNameOffset = BitConverter.ToInt32(archive, FileMetaOffset) + 0x20;

                string filename_in_arc = Encoding.UTF8.GetString(archive.Skip(FileNameOffset).TakeWhile(b => b != 0).ToArray());

                if (filename_in_arc != filename)
                    continue;

                int FileIndex = BitConverter.ToInt32(archive, FileMetaOffset + 4);

                uint FileDataLength = BitConverter.ToUInt32(archive, FileMetaOffset + 8);

                int FileDataOffset = BitConverter.ToInt32(archive, FileMetaOffset + 0xC) + (awakening ? 0x20 : 0x80);

                byte[] filefound = new byte[FileDataLength];

                Array.Copy(archive, FileDataOffset, filefound, 0, FileDataLength);

                return filefound;
            }

            return null;
        }
#endregion

        private uint MetaOffset;
        private uint FileCount;
        private bool awakening;

        private byte[] _archive;
        private Dictionary<string, fileinfo> _fileList;
        /// <summary>
        /// 实例一个Arc归档读取器
        /// </summary>
        /// <param name="arcPath"></param>
        public FEIF_ARC_Reader(string arcPath)
        {
            _archive = File.ReadAllBytes(arcPath);
            //MetaOffset 0x4
            MetaOffset = BitConverter.ToUInt32(_archive, 4) + 0x20;
            //FileCount 0x8
            FileCount = BitConverter.ToUInt32(_archive, 0x8);
            //In case we're using awakening archive type
            awakening = (BitConverter.ToUInt32(_archive, 0x20) != 0);

            InitFileList();
        }
        /// <summary>
        /// 初始化arc归档中的文件列表
        /// </summary>
        private void InitFileList()
        {
            _fileList = new Dictionary<string, fileinfo>();

            for (var i = 0; i < FileCount; i++)
            {
                int FileMetaOffset = 0x20 + BitConverter.ToInt32(_archive, (int)MetaOffset + 4 * i);

                int FileNameOffset = BitConverter.ToInt32(_archive, FileMetaOffset) + 0x20;

                string filename_in_arc = Encoding.UTF8.GetString(_archive.Skip(FileNameOffset).TakeWhile(b => b != 0).ToArray());

                _fileList.Add(filename_in_arc, new fileinfo(_archive, FileMetaOffset));
            }
        }
        /// <summary>
        /// 获取arc归档中的指定文件数据
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <returns></returns>
        public byte[] GetFile(string filename)
        {
            if(_fileList.ContainsKey(filename))
            {
                byte[] filefound = new byte[_fileList[filename].FileDataLength];

                Array.Copy(_archive, _fileList[filename].FileDataOffset, filefound, 0, _fileList[filename].FileDataLength);

                return filefound;
            }
            return null;
        }
        /// <summary>
        /// arc归档中是否存在指定文件
        /// </summary>
        /// <param name="filename">指定文件名</param>
        public bool HasFile(string filename)
        {
            return _fileList.ContainsKey(filename);
        }
        /// <summary>
        /// 获取Arc归档中的所有文件
        /// </summary>
        /// <returns>(文件名，文件数据)键值对数组</returns>
        public Dictionary<string, byte[]> GetAllFile()
        {
            var allFiles = new Dictionary<string, byte[]>();

            foreach(var file in _fileList)
            {
                byte[] filefound = new byte[file.Value.FileDataLength];

                Array.Copy(_archive, file.Value.FileDataOffset, filefound, 0, file.Value.FileDataLength);

                allFiles.Add(file.Key, filefound);
            }

            return allFiles;
        }
        /// <summary>
        /// 释放该实例占用内存
        /// </summary>
        public void Dispose()
        {
            _archive = null;
            _fileList = null;
        }
        /// <summary>
        /// 描述arc归档内的子文件信息
        /// </summary>
        struct fileinfo
        {
            int FileMetaOffset;
            public int FileNameOffset;
            public int FileIndex;
            public uint FileDataLength;
            public int FileDataOffset;

            public fileinfo(byte[] archive, int FileMetaOffset)
            {
                this.FileMetaOffset = FileMetaOffset;

                FileNameOffset = BitConverter.ToInt32(archive, FileMetaOffset) + 0x20;

                FileIndex = BitConverter.ToInt32(archive, FileMetaOffset + 4);

                FileDataLength = BitConverter.ToUInt32(archive, FileMetaOffset + 8);

                FileDataOffset = BitConverter.ToInt32(archive, FileMetaOffset + 0xC) + ((BitConverter.ToUInt32(archive, 0x20) != 0) ? 0x20 : 0x80);
            }
        }
    }
}
