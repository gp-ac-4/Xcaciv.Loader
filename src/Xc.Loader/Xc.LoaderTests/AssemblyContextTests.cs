using Xunit;
using Xc.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zTestInterfaces;

namespace Xc.Loader.Tests
{
    public class AssemblyContextTests
    {
        private string filePath = @"..\..\..\..\TestAssembly\bin\Debug\net6.0\zTestAssembly.dll";

        [Fact()]
        public void VerifyPath_Test()
        {
            var restrictedPath = System.IO.Path.Combine("C:", "some", "folder", "path");
            var filePath = System.IO.Path.Combine(restrictedPath, "subpath");
            var actualpath = AssemblyContext.VerifyPath(filePath);

            Assert.Equal(filePath, actualpath);
        }

        [Fact()]
        public void LoadAssembly_Test()
        {
            var expectedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath));
            using (var context = new AssemblyContext(filePath))
            {
                Assert.Equal(expectedPath, context.FilePath);
            }            
        }


        [Fact()]
        public void GetInstance_OutputTest()
        {
            var actual = String.Empty;

            using (var context = new AssemblyContext(filePath))
            {
                IClass1? class1 = context.GetInstance("Class1") as IClass1;
                actual = class1?.Stuff("input text here") ?? String.Empty;
                context.Unload();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.Equal("input text here output", actual);
        }

    }
}