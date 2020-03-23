# JSONsharp

A class I wrote to handle the conversion of objects in C# to JSON and back

Included is an interface that allows the developer to choose specific properties to convert or ignore and which direction the properties are being converted (to JSON or from JSON)

# How to Use

## Basic Usage

### Test Class

```csharp
public class TestConversion
{
    public string Name { get; set; }
    public string Value { get; set; }
}
```

### Simple conversion to JSON

```csharp
var test = new TestConversion(){ Name="My Name", Value = "Some sort of value" };

string JsonString = JSONSharp.FromJSON(test);

Console.WriteLine(JsonString);
```
### Result

```json
{"Name" : "My Name" , "Value" : "Some sort of value"}
```

### Simple conversion from JSON

```csharp
string JsonString= "{\"Name\" : \"Different Name\" , \"Value\" : \"Another kind of value\"}"; 

TestConversion test = JSONSharp.FromJSON<TestConversion>(JsonString);

Console.WriteLine(test.Name);
Console.WriteLine(test.Value);
```

### Result

```powershell
    Different Name
    Another kind of value
```

## Using the Attributes and IJSONConverter
Here are some examples of how to convert with attributes and the IJSONConverter for custom conversions

### Test Class:

```csharp
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
    public string PropertyUsingConverterFromJSON { get; set; }  (The IJSONValueConverter Interface must be used)

    // This property will be converted through the converter but only to JSON (IJSONValueConverter Required)
    [JSONsharp.JSONProperty(JSONsharp.ConversionDirection.ToJSON)]
    public string PropertyUsingConverterToJSON { get; set; } 

    // Generated from IJSONValueConverter
    public object ConvertFromJSON(string Name, object Value, PropertyInfo Property)
    {
        return Value;
    }
    // Generated from IJSONValueConverter
    public string ConvertToJSON(string Name, object Value, PropertyInfo Property)
    {
        return Value.ToString().Reverse().Aggregate("", (a, c) => a + c) ; // reverse the value
    }
}
```
Converting the object to a JSON string

```csharp
// Creating the object
var _test = new ClassForConversion()
{
    GiveMeANewName = "foo",
    IgnoreMe = "You, won't see this",
    IgnoreMeToJSON = "You won't see this either",
    IgnoreMeFromJSON = "Converting from JSON Won't see this",
    PropertyUsingConverterBothWays = "This was Converted using the methods",
    PropertyUsingConverterFromJSON = "This was only converted from the string",
    PropertyUsingConverterToJSON = "This was only converted from the object"
};

// Converting the Object
var str = JSONsharp.ToJSON(_test);

```

Result (in JSON)
```json
{
    "NewAlias" : "foo",
    "IgnoreMeFromJSON" : "Converting from JSON Won't see this",
    "PropertyUsingConverterBothWays" : "sdohtem eht gnisu detrevnoC saw sihT",
    "PropertyUsingConverterFromJSON" : "This was only converted from the string",
    "PropertyUsingConverterToJSON" : "tcejbo eht morf detrevnoc ylno saw sihT"
}
```

Example using the same class but Converting from JSON
```csharp
// Some Test JSON
string JsonString = "{"
    + "\"NewAlias\" : \"Some Value\","
    + "\"IgnoreMe\" : \"This will be ignored completely\","
    + "\"IgnoreMeToJSON\" : \"this won't be ignored\","
    + "\"IgnoreMeFromJSON\" : \"This is going to be ignored\","
    + "\"PropertyUsingConverterBothWays\" : \"This was passed through the ConvertFromJSON Method\","
    + "\"PropertyUsingConverterFromJSON\" : \"This was also passed through the ConvertFromJSON Method\","
    + "\"PropertyUsingConverterToJSON\" : \"This won't be converted\""
    + "}";

ClassForConversion ConvertedObject = JSONsharp.FromJSON<ClassForConversion>(JsonString);

Console.WriteLine(string.Join("\n", ConvertedObject.GetType().GetProperties().Select(x=>x.Name + " : " + x.GetValue(ConvertedObject))));
```

Result
```DOS
GiveMeANewName : Some Value
IgnoreMe : 
IgnoreMeFromJSON : 
IgnoreMeToJSON : this won't be ignored
PropertyUsingConverterBothWays : THIS WAS PASSED THROUGH THE CONVERTFROMJSON METHOD
PropertyUsingConverterFromJSON : THIS WAS ALSO PASSED THROUGH THE CONVERTFROMJSON METHOD
PropertyUsingConverterToJSON : This won't be converted
```
