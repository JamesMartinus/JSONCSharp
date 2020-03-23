using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JSONExample
{
    public class ClassForConversion : JSONsharp.IJSONValueConverter
    {
        // Use an alias for conversion
        [JSONsharp.JSONProperty("NewAlias")]
        public string GiveMeANewName { get; set; }

        // This property will not be converted at all//
        [JSONsharp.JSONProperty(JSONsharp.IgnoreDirection.Both)]
        public string IgnoreMe { get; set; }

        // This property will be ignored during conversion FROM JSON
        [JSONsharp.JSONProperty(JSONsharp.IgnoreDirection.FromJSON)]
        public string IgnoreMeFromJSON { get; set; }

        // This property will be ignored When converted TO JSON
        [JSONsharp.JSONProperty(JSONsharp.IgnoreDirection.ToJSON)]
        public string IgnoreMeToJSON { get; set; }

        // This property value will be converted through the converter Methods (IJSONValueConverter Required)
        [JSONsharp.JSONProperty(JSONsharp.ConversionDirection.Both)]
        public string PropertyUsingConverterBothWays { get; set; }

        // This property value will be converted through the converter but only from JSON (IJSONValueConverter Required)
        [JSONsharp.JSONProperty(JSONsharp.ConversionDirection.FromJSON)]
        public string PropertyUsingConverterFromJSON { get; set; }

    // This property will be converted through the converter but only to JSON (IJSONValueConverter Required)
    [JSONsharp.JSONProperty(JSONsharp.ConversionDirection.ToJSON)]
        public string PropertyUsingConverterToJSON { get; set; }

        // Generated from IJSONValueConverter
        public object ConvertFromJSON(string Name, object Value, PropertyInfo Property)
        {
            // make everything capitalised
            return Value.ToString().ToUpper();
        }
        // Generated from IJSONValueConverter
        public string ConvertToJSON(string Name, object Value, PropertyInfo Property)
        {
            // reverse the value
            return Value.ToString().Reverse().Aggregate("", (a, c) => a + c); 
        }
    }
}
