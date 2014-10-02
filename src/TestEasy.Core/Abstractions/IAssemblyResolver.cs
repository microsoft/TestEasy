using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestEasy.Core.Abstractions
{
    public interface IAssemblyResolver
    {
        string ResolvePartialAssemblyName(string partialName);
    }
}
