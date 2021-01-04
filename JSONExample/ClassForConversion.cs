using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JSONExample
{
    public class ClassForConversion : JSONSharp.IJSONValueConverter
    {
        // Use an alias for conversion
        [JSONSharp.JSONProperty("NewAlias")]
        public string GiveMeANewName { get; set; }

        // This property will not be converted at all//
        [JSONSharp.JSONProperty(JSONSharp.IgnoreDirection.Both)]
        public string IgnoreMe { get; set; }

        // This property will be ignored during conversion FROM JSON
        [JSONSharp.JSONProperty(JSONSharp.IgnoreDirection.FromJSON)]
        public string IgnoreMeFromJSON { get; set; }

        // This property will be ignored When converted TO JSON
        [JSONSharp.JSONProperty(JSONSharp.IgnoreDirection.ToJSON)]
        public string IgnoreMeToJSON { get; set; }

        // This property value will be converted through the converter Methods (IJSONValueConverter Required)
        [JSONSharp.JSONProperty(JSONSharp.ConversionDirection.Both)]
        public string PropertyUsingConverterBothWays { get; set; }

        // This property value will be converted through the converter but only from JSON (IJSONValueConverter Required)
        [JSONSharp.JSONProperty(JSONSharp.ConversionDirection.FromJSON)]
        public string PropertyUsingConverterFromJSON { get; set; }

    // This property will be converted through the converter but only to JSON (IJSONValueConverter Required)
    [JSONSharp.JSONProperty(JSONSharp.ConversionDirection.ToJSON)]
        public string PropertyUsingConverterToJSON { get; set; }
        public object ConvertFromJSON(string Name, object Value, PropertyInfo Property, Func<Dictionary<string, object>, Type, object> ConvertToType)
        {
            // make everything capitalised
            return Value.ToString().ToUpper();
        }

        // Generated from IJSONValueConverter
        public string ConvertToJSON(string Name, object Value, PropertyInfo Property)
        {
            // reverse the value
            return "\"" +Value.ToString().Reverse().Aggregate("", (a, c) => a + c) + "\""; 
        }
    }
}
