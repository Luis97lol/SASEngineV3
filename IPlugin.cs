using System;
using System.Threading.Tasks;

namespace SASEngine
{
    public interface IPlugin: IDisposable
    {
        Task start();
    }
}
