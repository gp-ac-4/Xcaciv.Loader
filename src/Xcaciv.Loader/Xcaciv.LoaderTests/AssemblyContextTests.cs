using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zTestInterfaces;
using Xunit.Abstractions;

namespace Xcaciv.Loader.Tests
{
    public class AssemblyContextTests
    {
        private ITestOutputHelper _testOutput;
        private string simpleDllPath = @"..\..\..\..\TestAssembly\bin\{1}\net8.0\zTestAssembly.dll";
        private string dependentDllPath = @"..\..\..\..\zTestDependentAssembly\bin\{1}\net8.0\zTestDependentAssembly.dll";

        public AssemblyContextTests(ITestOutputHelper output)
        {
            this._testOutput = output;
#if DEBUG
            this._testOutput.WriteLine("Tests in Debug mode");
            this.simpleDllPath = simpleDllPath.Replace("{1}", "Debug");
            this.dependentDllPath = dependentDllPath.Replace("{1}", "Debug");
#else
            this._testContext.WriteLine("Tests in Release mode??");
            this.simpleDllPath = simpleDllPath.Replace("{1}", "Release");
            this.dependentDllPath = dependentDllPath.Replace("{1}", "Release");
#endif
        }

        [Fact()]
        public void VerifyPath_Test()
        {
            var restrictedPath = System.IO.Path.Combine("C:", "some", "folder", "path");
            var filePath = System.IO.Path.Combine(restrictedPath, "subpath");
            var actualpath = Xcaciv.Loader.AssemblyContext.VerifyPath(filePath);

            Xunit.Assert.Equal(filePath, actualpath);
        }

        [Fact()]
        public void LoadAssembly_Test()
        {
            var expectedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, simpleDllPath));
            var basePath = System.IO.Path.GetDirectoryName(expectedPath) ?? String.Empty;
            var context = new Xcaciv.Loader.AssemblyContext(simpleDllPath, basePathRestriction:basePath);

            Xunit.Assert.Equal(expectedPath, context.FilePath);       
        }

        [Fact()]
        public void LoadOutOfRangeAssembly_Test()
        {
            var expectedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, simpleDllPath));
            

            Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new Xcaciv.Loader.AssemblyContext(simpleDllPath));
        }

        [Fact()]
        public void LoadDoesNotExistAssembly_Test()
        {
            var expectedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "does\\not\\exist.dll"));
            var context = new Xcaciv.Loader.AssemblyContext(expectedPath, basePathRestriction: "*");

            Xunit.Assert.Throws<System.IO.FileNotFoundException>(() => context.GetInstance("Class1"));
        }


        [Fact()]
        public void GetInstance_OutputTest()
        {
            var actual = String.Empty;

            using (var context = new Xcaciv.Loader.AssemblyContext(simpleDllPath, basePathRestriction:"*"))
            {
                IClass1? class1 = context.GetInstance("Class1") as IClass1;
                actual = class1?.Stuff("input text here") ?? String.Empty;
                context.Unload();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Xunit.Assert.Equal("input text here output", actual);
        }

        private string UseFactory(string path)
        {
            using (var context = Xcaciv.Loader.AssemblyContext.LoadFromPath(path))
            {
                IClass1? class1 = context.GetInstance("Class1") as IClass1;
                return class1?.Stuff("input text here") ?? String.Empty;
            }
        }

        [Fact()]
        public void UsingFactory_FromPath_Unloads()
        {
            var actual = UseFactory(simpleDllPath);
            
            // collect to demonstrate unload
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Xunit.Assert.Equal("input text here output", actual);
        }

        public string DoManual(string aPath)
        {
            var context = new System.Runtime.Loader.AssemblyLoadContext(null, true);
            var assembly = context.LoadFromAssemblyPath(aPath);
            var location = assembly.Location;

            var classTypeName = assembly.GetTypes().FirstOrDefault(t => typeof(IClass1).IsAssignableFrom(t))?.FullName ?? String.Empty;
            IClass1? class1 = assembly.CreateInstance(classTypeName) as IClass1;
            var actual = class1?.Stuff("input text here") ?? String.Empty;

            context.Unload();

            return actual;
        }

        [Fact()]
        public void ManualTest()
        {
            var actual = DoManual(Xcaciv.Loader.AssemblyContext.VerifyPath(simpleDllPath));

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Xunit.Assert.Equal("input text here output", actual);
        }

        [Fact()]
        public void UsingFactory_WithDependency_Unloads()
        {
            var actual = String.Empty;
            using (var context = Xcaciv.Loader.AssemblyContext.LoadFromPath(dependentDllPath))
            {
                IClass1? class1 = context.GetInstance("Class1") as IClass1;
                actual = class1?.Stuff("input text here") ?? String.Empty;
            }

            Xunit.Assert.Equal("5,5,8", actual);
        }

        [Fact()]
        public void UsingStrongTypedFactory_Unloads()
        {
            var actual = String.Empty;
            using (var context = Xcaciv.Loader.AssemblyContext.LoadFromPath(dependentDllPath))
            {
                var class1 = context.GetInstance<IClass1>("Class1");
                actual = class1?.Stuff("input text here") ?? String.Empty;
            }

            Xunit.Assert.Equal("5,5,8", actual);
        }

    }
}