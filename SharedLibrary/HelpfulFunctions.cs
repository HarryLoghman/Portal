using System;
using System.Collections.Generic;
using System.Dynamic;
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

        public static bool IsPropertyExist(dynamic dynamicObject, string propertyName)
        {
            if (dynamicObject is ExpandoObject)
                return ((IDictionary<string, object>)dynamicObject).ContainsKey(propertyName);

            return dynamicObject.GetType().GetProperty(propertyName) != null;
        }

    }
}
