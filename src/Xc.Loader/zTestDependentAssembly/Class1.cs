using zTestInterfaces;

namespace zTestDependentAssembly
{
    public class Class1 : IClass1
    {
        public string Stuff(string input)
        {
            var returnString = new List<string>();
            Fastenshtein.Levenshtein lev = new Fastenshtein.Levenshtein(input);
            foreach (var item in new[] { "in text re", "input text", "inpuere" })
            {
                int levenshteinDistance = lev.DistanceFrom(item);
                returnString.Add(levenshteinDistance.ToString());
            }

            return string.Join(',', returnString);
        }
    }
}