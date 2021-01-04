using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JSONExample
{
    public class JSONSharp
    {
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
        public static T FromString<T>(string str) => (T)FromString(typeof(T), str);
        public static object FromString(Type type, string str)
        {
            var _values = new List<object>();
            string _add(object item) { _values.Add(item); return (_values.Count - 1).ToString(); }
            var _char = new Dictionary<string, string>() { { "[", "]" }, { "{", "}" } };
            object _convert(Type _type, object target)
            {
                if (target == null || _type == target.GetType() || _type == typeof(object)) return target;
                var _target = target.GetType();
                if (_target == typeof(JSONiser)) return _establish((JSONiser)target, _type);
                else if (_type.IsEnum && _target == typeof(string))
                    return _type.GetEnumValues().OfType<Enum>().FirstOrDefault(x => x.ToString() == (string)target);
                else if (typeof(TypeCode).GetEnumNames().Skip(5).Take(11).Contains(Type.GetTypeCode(_type).ToString()))
                    return Convert.ChangeType(target, _type);
                else return null;
            }
            JSONiser _extract(List<string> _list)
            {
                var _subject = _list.Skip(1).Take(_list.Count() - 2).ToList();
                while (_subject.Contains("{") || _subject.Contains("["))
                {
                    var _listDeformed = _subject.Select((x, i) => (Key: i, Value: x)).ToList();
                    var opener = _listDeformed.FirstOrDefault(x => _char.Keys.Contains(x.Value));
                    var _openers = _listDeformed.Where(x => x.Value == opener.Value).ToList();
                    var _closers = _listDeformed.Where(x => x.Value == _char[opener.Value]).ToList();
                    var ender = _closers.FirstOrDefault(x =>
                    {
                        bool _test((int Key, string) item) => item.Key > opener.Key && item.Key < x.Key;
                        return _openers.Count(_test) == _closers.Count(_test);
                    });
                    _subject = _subject.Take(opener.Key).Concat(new List<string>() { _add(_subject.Skip(opener.Key).Take(ender.Key - opener.Key + 1).ToList()) }).Concat(_subject.Skip(ender.Key + 1)).ToList();
                }
                return _subject.Aggregate(new List<List<object>>() { new List<object>() }, (a, c) =>
                {
                    if (c.GetType() == typeof(string) && c.ToString() == ",") a.Add(new List<object>()); else a.Last().Add(int.TryParse(c, out var ind) ? (_values[ind] != null && _values[ind].GetType() == typeof(List<string>)) ? _extract((List<string>)_values[ind]) : _values[ind] : c);
                    return a;
                }, acu => acu.Count > 0 ? new JSONiser(acu.First().Count == 3 ? acu.ToDictionary(x => (string)x.First(), x => x.Last()) : acu.SelectMany(x => x).Select((x, i) => (x, i)).ToDictionary(x => x.i.ToString(), x => x.x)) : new JSONiser());
            }
            object _establish(JSONiser _list, Type _type)
            {
                var _iscollection = (_type.IsArray || _type.GetInterface("ICollection") != null || _type.GetInterface("IEnumerable") != null);
                var _innerType = _iscollection ? _type.IsArray ? _type.GetElementType() : _type.GenericTypeArguments.Length == 1 ? _type.GenericTypeArguments[0] : null : null;
                var _adder = _type.GetMethods().FirstOrDefault(x => x.Name == "Add" || x.Name == "SetValue");
                if (_iscollection && _innerType != null)
                {
                    return _list.Values.Select((value, index) => (index, value)).Aggregate(Activator.CreateInstance(_type, _type.IsArray ? new object[] { _list.Values.Count } : null), (a, c) =>
                    {
                        var conv = _convert(_innerType, c.value);
                        _adder.Invoke(a, _type.IsArray ? new object[] { conv, c.index } : new object[] { conv });
                        return a;
                    });
                }
                else
                {
                    return GetAttributeDefinitions(_type).Where(x => !x.IgnoreFrom).Aggregate(Activator.CreateInstance(_type), (a, c) =>
                    {
                        Func<Dictionary<string, object>, Type, object> func = (x, y) => _establish(new JSONiser(x), y);
                        if (_list.ContainsKey(c.Name))
                        {
                            if (c.CustomConvertFrom)
                            {
                                var v = _values;
                                var n = _list[c.Name];

                                var fromConverter = ((IJSONValueConverter)a).GetType().GetMethod("ConvertFromJSON").Invoke(a, new object[] { c.Name, _list[c.Name], c.property, func });
                                if (fromConverter != null) c.property.SetValue(a, fromConverter);
                            }
                            else
                            {
                                if (c.property.SetMethod != null) c.property.SetValue(a, _convert(c.property.PropertyType, _list[c.Name]));
                            }
                        }
                        return a;
                    });
                }
            }
            var est = Regex.Split(str, @"((?<!\\)"".*?(?<!\\)"")|([\d.-]+)|(null)|(true|false)").SelectMany(x => !x.StartsWith("\"") && Regex.IsMatch(x, @"[{}:,[\]]") ? x.ToCharArray().Where(y => !char.IsWhiteSpace(y)).Select(y => "" + y) : new string[] { x }).
                Select(value =>
                {
                    if (Regex.IsMatch(value, @"^"".*""$|null|[\d.-]+|^(true|false)$"))
                    {
                        if (double.TryParse(value, out var number)) return _add(number);
                        else if (bool.TryParse(value, out var boolean)) return _add(boolean);
                        else if (value.StartsWith("\"") && value.EndsWith("\"")) return _add(string.Join("", value.Skip(1).Take(value.Length - 2)));
                        else return _add(null);
                    }
                    else return value;
                }).ToList();
            var Stuff = _establish(_extract(est), type);
            return Stuff;
        }
        public static string ToJSON(object target)
        {
            if (target == null) return "null";
            var type = target.GetType();
            var _attribute = type.GetCustomAttributes(false).OfType<JSONClassAttribute>().FirstOrDefault(x => !string.IsNullOrEmpty(x.Advocate));
            if (_attribute != null)
            {
                var _property = type.GetProperty(_attribute.Advocate);
                if (_property == null) throw new Exception("The Advocate (" + _attribute.Advocate + ") doesn't exist on class (" + type.Name + ")");
                return ToJSON(_property.GetValue(target));
            }
            var _typecode = Type.GetTypeCode(type);
            var isQuoted = type.IsEnum || _typecode == TypeCode.String;
            var _string = "";
            if (_typecode == TypeCode.Object)
            {
                if (target is DynamicObject dio)
                {
                    var dict = dio.GetDynamicMemberNames().ToDictionary(x => x, y => dio.TryGetMember(new DynamicAccess(y, true), out var result) ? result : null);
                    return ToJSON(dict);
                }
                else if (target is IDictionary dict)
                {
                    var KeyPairs = (Keys: new object[dict.Count], Values: new object[dict.Count]);
                    dict.Keys.CopyTo(KeyPairs.Keys, 0);
                    dict.Values.CopyTo(KeyPairs.Values, 0);
                    return "{" + string.Join(",", KeyPairs.Keys.Zip(KeyPairs.Values, (k, v) => "\"" + k.ToString() + "\":" + ToJSON(v)).ToArray()) + "}";
                }
                else if (type.IsArray || type.GetInterface("ICollection") != null || type.GetInterface("IEnumerable") != null)
                {
                    var _l = new JSONBuilder();
                    foreach (var item in (ICollection)target) _l.Add(ToJSON(item));
                    _string += "[" + string.Join(",", _l) + "]";
                }
                else
                {
                    return "{" + string.Join(",", GetAttributeDefinitions(type).Where(x => !x.IgnoreTo).Select(x =>
                    {
                        var Value = x.CustomConvertTo ? ((IJSONValueConverter)target).ConvertToJSON(x.Name, x.property.GetValue(target), x.property) : ToJSON(x.property.GetValue(target));
                        return "\"" + x.Name + "\":" + Value;
                    })) + "}";
                }
            }
            else
                return (isQuoted ? "\"" : "") + (_typecode == TypeCode.Boolean ? target.ToString().ToLower() : target.ToString()) + (isQuoted ? "\"" : "");
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
            public string Advocate { get; set; }
            public JSONClassAttribute(bool AllConvert) => CustomConvertOnAllProperties = AllConvert;
            public JSONClassAttribute(string advocate) => Advocate = advocate;
            public JSONClassAttribute() { }
        }
        public interface IJSONValueConverter
        {
            object ConvertFromJSON(string Name, object Value, PropertyInfo Property, Func<Dictionary<string, object>, Type, object> ConvertToType);
            string ConvertToJSON(string Name, object Value, PropertyInfo Property);
        }
        private class JSONBuilder : List<string>
        {
            public string Value { get; set; }
            public bool IsList { get => Count > 0; }
            public JSONBuilder() { }
            public JSONBuilder(IEnumerable<string> collection) : base(collection) { }
        }
        public class JSONiser : Dictionary<string, object>
        {
            public JSONiser() { }
            public JSONiser(IDictionary<string, object> collection) : base(collection) { }
        }
        private class DynamicAccess : GetMemberBinder
        {
            public DynamicAccess(string name, bool ignorecase) : base(name, ignorecase) { }

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }
        }
    }
}
