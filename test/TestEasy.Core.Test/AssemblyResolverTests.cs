using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestEasy.Core.Abstractions;
using Xunit;
using Xunit.Extensions;

namespace TestEasy.Core.Test
{
    public class AssemblyResolverTests
    {
        public class ResolvePartialAssemblyName
        {
            [Theory]
            [InlineData("System.Web.Abstractions", "System.Web.Abstractions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
            [InlineData("System.Drawing", "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
            [InlineData("System.Data", "System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
            public void VerifyFrameworkAssemblies(string partialName, string resolvedName)
            {
                var a = new AssemblyResolver();

                string fullName = a.ResolvePartialAssemblyName(partialName);

                Assert.Equal<string>(resolvedName, fullName);
            }

            [Fact]
            public void WhenAssemblyDoesNotExist_ReturnNull()
            {
                var a = new AssemblyResolver();

                string fullName = a.ResolvePartialAssemblyName("ThisAssemblyDoesNotExist");

                Assert.Null(fullName);
            }
        }
    }
}
