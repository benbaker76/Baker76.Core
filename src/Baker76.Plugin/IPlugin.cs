using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baker76.Plugin
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        void Execute();
        Task<List<object>> Import(List<IFileSource> fileList, Dictionary<string, string> options);
    }
}
