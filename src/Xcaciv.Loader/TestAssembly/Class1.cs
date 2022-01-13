using zTestInterfaces;

namespace TestAssembly
{
    public class Class1 : IClass1
    {
        public string Stuff(string input)
        {
            return input + " output";
        }
    }
}