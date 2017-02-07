using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class HelpfulFunctions
    {
        public static string GetAllTheNumbersFromComplexString(string input)
        {
            return new String(input.Where(Char.IsDigit).ToArray());
        }
    }
}
