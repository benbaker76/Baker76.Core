using System.Collections.Generic;
using System.Threading.Tasks;
using Baker76.Core.IO;

namespace RetroStudio.Core.Plugin
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        void Execute();
        Task<List<object>> Import(List<IFileSource> fileList, Dictionary<string, string> options);
    }
}
