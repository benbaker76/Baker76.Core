using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baker76.Plugin
{
    public class DiskFileSource : IFileSource
    {
        private readonly string _filePath;

        public DiskFileSource(string filePath)
        {
            _filePath = filePath;
        }

        public string Name => Path.GetFileName(_filePath);

        public Stream OpenReadStream(long maxAllowedSize = 512000)
        {
            return new FileStream(_filePath, FileMode.Open, FileAccess.Read);
        }

        public string ContentType => "application/octet-stream";
    }
}
