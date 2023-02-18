using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Interfaces
{
    public interface IFileService
    {
        bool Delete(string path, string fileName);
        bool FileExists(string path, string fileName);
        Task<bool> Overwrite(byte[] file, string path, string fileName);
        Task<byte[]> Read(string path, string fileName);
        Task<bool> Write(byte[] file, string path, string fileName);
    }
}