using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Interfaces;
using Microsoft.Extensions.Configuration;

namespace API.Services
{
    public class FileService : IFileService
    {
        private readonly IConfiguration _config;
        public FileService(IConfiguration config)
        {
            _config = config;
        }

        public bool Delete(string path, string fileName)
        {
            try{
                if (this.FileExists(path, fileName)){
                    System.IO.File.Delete(_config["DocumentRepository"] + path + fileName);
                    return true;
                }
                return false;
            }
            catch(Exception){
                return false;
            }
        }

        public bool FileExists(string path, string fileName)
        {
            return System.IO.File.Exists(_config["DocumentRepository"] + path + fileName);
        }

        public async Task<byte[]> Read(string path, string fileName)
        {
            return await System.IO.File.ReadAllBytesAsync(_config["DocumentRepository"] + path + fileName);
        }

        public async Task<bool> Overwrite(byte[] file, string path, string fileName)
        {
            try{
                var filePath = _config["DocumentRepository"] + path + fileName;
                await System.IO.File.WriteAllBytesAsync(filePath, file);
                return true;
            } catch(Exception) {
                return false;
            }
        }

        public async Task<bool> Write(byte[] file, string path, string fileName)
        {
            if (!this.FileExists(path, fileName)){
                return await this.Overwrite(file, path, fileName);
            }
            return false;
        }
    }
}