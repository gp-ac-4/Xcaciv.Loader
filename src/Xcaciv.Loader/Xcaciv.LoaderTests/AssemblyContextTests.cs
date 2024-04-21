using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zTestInterfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xcaciv.Loader.Tests
{
    public class AssemblyContextTests
    {
        private TestContext _testContext;
        private string simpleDllPath = @"..\..\..\..\TestAssembly\bin\{1}\net8.0\zTestAssembly.dll";
        private string dependentDllPath = @"..\..\..\..\zTestDependentAssembly\bin\{1}\net8.0\zTestDependentAssembly.dll";

        public AssemblyContextTests(TestContext testContext)
        {
            this._testContext = testContext;
#if DEBUG
            this._testContext.WriteLine("Tests in Debug mode");
            simpleDllPath = simpleDllPath.Replace("{1}", "Debug");
            dependentDllPath = dependentDllPath.Replace("{1}", "Debug");
#else
            this._testContext.WriteLine("Tests in Release mode");
            simpleDllPath = simpleDllPath.Replace("{1}", "Release");
            dependentDllPath = dependentDllPath.Replace("{1}", "Release");
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
            var context = new Xcaciv.Loader.AssemblyContext(simpleDllPath);

            Xunit.Assert.Equal(expectedPath, context.FilePath);       
        }


        [Fact()]
        public void GetInstance_OutputTest()
        {
            var actual = String.Empty;

            using (var context = new Xcaciv.Loader.AssemblyContext(simpleDllPath))
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

            var classType = assembly.GetTypes().FirstOrDefault(t => typeof(IClass1).IsAssignableFrom(t));
            IClass1? class1 = assembly.CreateInstance(classType.FullName) as IClass1;
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