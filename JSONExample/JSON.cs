using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JSONExample
{
    public class JSONsharp
    {
        private static object _FromString(string str)
        {
            var _values = new Dictionary<string, object>();
            var _count = 0;
            var _char = new Dictionary<string, string>() { { "[", "]" }, { "{", "}" } };
            Func<string, MatchCollection> _m = s => Regex.IsMatch(s, @"[[\]{}]") ? Regex.Matches(s, @"[[\]{}]") : null;
            str = Regex.Replace(str, @"(?<!\\)""(.*?)(?<!\\)""|(\d+)|(null)", m =>
            {
                var name = "V" + _count++;
                if (!string.IsNullOrEmpty(m.Groups[1].Value)) _values.Add(name, m.Groups[1].Value);
                if (!string.IsNullOrEmpty(m.Groups[2].Value) && double.TryParse(m.Groups[2].Value, out var result)) _values.Add(name, result);
                if (!string.IsNullOrEmpty(m.Groups[3].Value) && m.Groups[3].Value == "null") _values.Add(name, null);
                return name;
            });
            T _TakeFromValues<T>(string index)
            {
                var _value = _values[index];
                _values.Remove(index);
                return (T)_value;
            }
            string _Extract(string json)
            {
                for (var reg = _m(json); reg != null; reg = _m(json))
                {
                    if (reg.Count % 2 != 0) throw new Exception("JSON is not formatted properly (missing a closing bracket?)");
                    var start = reg[0];
                    var openers = reg.OfType<Match>().Where(x => x.Value == start.Value).ToList();
                    var closers = reg.OfType<Match>().Where(x => x.Value == _char[start.Value]).ToList();
                    var end = closers.FirstOrDefault(x =>
                    {
                        var _counter = new Func<Match, bool>(a => a.Index > start.Index && a.Index < x.Index);
                        return openers.Count(_counter) == closers.Count(_counter);
                    });
                    if (end == null) throw new Exception("Couldn't find the closing bracket for some reason");
                    var _extraction = _Extract(json.Substring(start.Index + 1, end.Index - 1 - start.Index));
                    var _OName = "V" + _count++;
                    if (start.Value == "{")
                    {
                        _values.Add(_OName, Regex.Matches(_extraction, @"(V[0-9]+)\s*:\s*(V[0-9]+)").OfType<Match>()
                            .Select(x => new { Key = _TakeFromValues<string>(x.Groups[1].Value), Value = _TakeFromValues<object>(x.Groups[2].Value) }).ToDictionary(x => x.Key, x => x.Value));
                    }
                    else if (start.Value == "[")
                        _values.Add(_OName, _extraction.Split(',').Select((x, i) => new { Key = i, Value = _TakeFromValues<object>(x.Replace(" ", "")) }).ToDictionary(x => x.Key, x => x.Value));
                    json = json.Substring(0, start.Index) + _OName + json.Substring(end.Index + 1);
                }
                return json;
            }
            _Extract(str);
            if (_values.Count > 1) throw new Exception("Not properly formatted, JSON requires a containing Object or Array");
            return _values.FirstOrDefault().Value;
        }
        private static List<(string Name, PropertyInfo property, bool IgnoreTo, bool IgnoreFrom, bool CustomConvertTo, bool CustomConvertFrom)> GetAttributeDefinitions(Type type)
        {
            var _class = type.GetCustomAttributes(false).OfType<JSONClassAttribute>().FirstOrDefault() ?? new JSONClassAttribute();
            return type.GetProperties().Select(x =>
            {

                var _property = x.GetCustomAttributes(false).OfType<JSONPropertyAttribute>().FirstOrDefault() ?? new JSONPropertyAttribute(ConversionDirection.Default);
                var _name = string.IsNullOrEmpty(_property.Alias) ? x.Name : _property.Alias;
                var _ignoreFrom = _class.IgnoreAll ? !(_property.Ignore == IgnoreDirection.Neither || _property.Ignore == IgnoreDirection.ToJSON) : (_property.Ignore == IgnoreDirection.Both || _property.Ignore == IgnoreDirection.FromJSON);
                var _ignoreTo = _class.IgnoreAll ? !(_property.Ignore == IgnoreDirection.Neither || _property.Ignore == IgnoreDirection.FromJSON) : (_property.Ignore == IgnoreDirection.Both || _property.Ignore == IgnoreDirection.ToJSON);
                var _hasInterface = type.GetInterface("IJSONValueConverter") != null;
                var _convertFrom = _hasInterface ? _class.CustomConvertOnAllProperties ? !(_property.CustomConverterDirection == ConversionDirection.Neither || _property.CustomConverterDirection == ConversionDirection.ToJSON) : (_property.CustomConverterDirection == ConversionDirection.Both || _property.CustomConverterDirection == ConversionDirection.FromJSON) : false;
                var _convertTo = _hasInterface ? _class.CustomConvertOnAllProperties ? !(_property.CustomConverterDirection == ConversionDirection.Neither || _property.CustomConverterDirection == ConversionDirection.FromJSON) : (_property.CustomConverterDirection == ConversionDirection.Both || _property.CustomConverterDirection == ConversionDirection.ToJSON) : false;
                return (_name, x, _ignoreTo, _ignoreFrom, _convertTo, _convertFrom);
            }).ToList();
        }
        public static T FromJSON<T>(string json)
        {
            object _convert(Type type, object target)
            {
                if (target == null) return null;
                var _target = target.GetType();
                if (type != _target)
                {
                    if (_target.GetInterface("IDictionary") != null)
                    {
                        var _type = _target.GetGenericArguments()[0];
                        var _iscollection = (type.IsArray || type.GetInterface("ICollection") != null || type.GetInterface("IEnumerable") != null);
                        if (_type == typeof(int) && _iscollection)
                        {
                            var innerType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                            var list = ((Dictionary<int, object>)target).Values.Select((x, i) => new { index = i, value = _convert(innerType, x) }).ToList();
                            var _ctor = Activator.CreateInstance(type, new object[] { list.Count });
                            var _add = _ctor.GetType().GetMethods().FirstOrDefault(x => x.Name == "Add" || x.Name == "SetValue");
                            var _params = _add.GetParameters().Length;
                            foreach (var item in list) _add.Invoke(_ctor, _params > 1 ? new object[] { item.value, item.index } : new object[] { item.value });
                            return _ctor;
                        }
                        else if (_type == typeof(string) && !_iscollection)
                        {
                            var _ctor = Activator.CreateInstance(type);
                            var _jsonobject = (Dictionary<string, object>)target;
                            foreach (var p in GetAttributeDefinitions(type).Where(x => !x.IgnoreFrom && _jsonobject.ContainsKey(x.Name)))
                            {
                                var _value = p.CustomConvertFrom ? ((IJSONValueConverter)_ctor).ConvertFromJSON(p.Name, _jsonobject[p.Name], p.property) : _convert(p.property.PropertyType, _jsonobject[p.Name]);
                                p.property.SetValue(_ctor, _value);
                            }
                            return _ctor;
                        }
                        else return null;
                    }
                    else if (typeof(TypeCode).GetEnumNames().Skip(5).Take(11).Contains(Type.GetTypeCode(type).ToString()))
                        return Convert.ChangeType(target, type);
                    else if (type.IsEnum && _target == typeof(string))
                        return type.GetEnumValues().OfType<Enum>().FirstOrDefault(x => x.ToString() == (string)target);
                    else return null;
                }
                else
                    return target;
            }
            return (T)_convert(typeof(T), _FromString(json));
        }
        public static string ToJSON(object target)
        {
            if (target == null) return "null";
            var type = target.GetType();
            var _typecode = Type.GetTypeCode(type);
            var _string = "";
            if (_typecode == TypeCode.Object)
            {
                if (type.IsArray || type.GetInterface("ICollection") != null || type.GetInterface("IEnumerable") != null)
                {
                    var _l = new List<string>();
                    foreach (var item in (ICollection)target) _l.Add(ToJSON(item));
                    _string += "[" + string.Join(",", _l) + "]";
                }
                else
                {
                    return "{" + string.Join(",", GetAttributeDefinitions(type).Where(x => !x.IgnoreTo).Select(x =>
                    {
                        var Value = x.CustomConvertTo ? "\"" + ((IJSONValueConverter)target).ConvertToJSON(x.Name, x.property.GetValue(target), x.property) + "\"" : ToJSON(x.property.GetValue(target));
                        return "\"" + x.Name + "\":" + Value;
                    })) + "}";
                }
            }
            else
            {
                return "\"" + (
                    (type == typeof(bool)) ? (((bool)target) ? "true" : "false") :
                    (type == typeof(string)) ? (string)target :
                    (type.IsEnum) ? type.GetEnumName(target) : target.ToString()
                    ) + "\"";
            }
            return _string;
        }
        public enum IgnoreDirection { Default, Neither, Both, ToJSON, FromJSON }
        public enum ConversionDirection { Default, Both, ToJSON, FromJSON, Neither }
        [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
        public class JSONPropertyAttribute : Attribute
        {
            public ConversionDirection CustomConverterDirection { get; set; }
            public IgnoreDirection Ignore { get; set; }
            public string Alias { get; set; }
            public JSONPropertyAttribute(IgnoreDirection ignore, ConversionDirection customconversiondirection, string alias)
            {
                Ignore = ignore;
                CustomConverterDirection = customconversiondirection;
                Alias = alias;
            }
            public JSONPropertyAttribute(IgnoreDirection ignore) => Ignore = ignore;
            public JSONPropertyAttribute(ConversionDirection customconversiondirection) => CustomConverterDirection = customconversiondirection;
            public JSONPropertyAttribute(string alias) => Alias = alias;
        }

        [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
        public class JSONClassAttribute : Attribute
        {
            public bool CustomConvertOnAllProperties { get; set; }
            public bool IgnoreAll { get; set; }
            public JSONClassAttribute(bool AllConvert) => CustomConvertOnAllProperties = AllConvert;
            public JSONClassAttribute() { }
        }
        public interface IJSONValueConverter
        {
            object ConvertFromJSON(string Name, object Value, PropertyInfo Property);
            string ConvertToJSON(string Name, object Value, PropertyInfo Property);
        }
    }
}
