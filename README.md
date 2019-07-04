# JSONCSharp

A class I wrote to handle the conversion of objects in C# to JSON and back

Included is an interface that allows the developer to choose specific properties to convert or ignore and which direction the properties are being converted (to JSON or from JSON)

## Basic Usage:

Test class:

```csharp
public class SomeData
{
  public string Name {get;set;}
  public int Age {get;set}
}
```
Usage:

```csharp
SomeData datatoconvert = new SomeData(){Name = "Foo", Age = 58};

string json = JSON.toJSONString(data);

SomeData datatoconvertback = JSON.Parse<SomeData>(json);
```
