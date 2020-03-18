using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JSONExample
{
    public class JSONsharp
    {
        private static object _FromString(string str)
        {
            var _values = new Dictionary<string, object>();
            var _char = new Dictionary<string, string>() { { "[", "]" }, { "{", "}" } };
            Func<string, MatchCollection> _m = s => Regex.IsMatch(s, @"[[\]{}]") ? Regex.Matches(s, @"[[\]{}]") : null;
            str = Regex.Replace(str, @"(?<!\\)""(.*?)(?<!\\)""|(\d+)", m =>
            {
                var name = "V" + _values.Count;
                var _value = String.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[2].Value : m.Groups[1].Value;
                if (double.TryParse(_value, out var result)) _values.Add(name, result); else _values.Add(name, _value);
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
                    var _OName = "V" + _values.Count;
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
        public static T FromJSON<T>(string json)
        {
            object _convert(Type type, object target)
            {
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
                            foreach (var x in type.GetProperties())
                            {
                                var _attribute = x.GetCustomAttributes(typeof(ConvertAttribute), true).OfType<ConvertAttribute>().FirstOrDefault();
                                var Name = _attribute != null && _attribute.NewName != null ? _attribute.NewName : x.Name;
                                if (!_jsonobject.ContainsKey(Name) || (_attribute != null && (_attribute.Ignore == ConvertAttribute.ConvertIgnore.Both || _attribute.Ignore == ConvertAttribute.ConvertIgnore.In))) continue;
                                var Method = _attribute != null && _attribute.ConvertIn != null ? type.GetMethod(_attribute.ConvertIn) : null;
                                if (Method != null)
                                {
                                    if (Method.GetParameters().Length < 1) throw new Exception("Method requires at least one parameter");
                                    if (Method.GetParameters()[0].ParameterType != typeof(string)) throw new Exception("Parameter must be of type string");
                                    if (Method.ReturnType != typeof(object)) throw new Exception("Return Type must be of type object");
                                }
                                var Value = Method != null ? Method.Invoke(_ctor, new object[] { _convert(x.PropertyType, _jsonobject[Name]) }) : _convert(x.PropertyType, _jsonobject[Name]);
                                x.SetValue(_ctor, Value);
                            }
                            return _ctor;
                        }
                        else return null;
                    }
                    else if (type.IsValueType && _target == typeof(double))
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
                    _string += "[";
                    foreach (var item in (ICollection)target) _string += ToJSON(item) + (_string != "[" ? "," : "");
                    _string += "]";
                }
                else
                {
                    return "{" + string.Join(",", type.GetProperties().Select(x =>
                    {
                        var toot = x.GetCustomAttributes(typeof(ConvertAttribute), true).OfType<ConvertAttribute>().FirstOrDefault();
                        if (toot != null && (toot.Ignore == ConvertAttribute.ConvertIgnore.Both || toot.Ignore == ConvertAttribute.ConvertIgnore.Out)) return "";
                        var method = toot != null && toot.ConvertOut != null ? type.GetMethods().FirstOrDefault(y => y.Name == toot.ConvertOut) : null;
                        if (method != null)
                        {
                            if (method.GetParameters().Length < 1) throw new Exception("Method must have a parameter");
                            if (method.GetParameters()[0].ParameterType != typeof(object)) throw new Exception("First parameter must be of type object");
                            if (method.ReturnType != typeof(string)) throw new Exception("Method needs to have a return type of string");
                        }
                        var Name = toot != null && !string.IsNullOrEmpty(toot.NewName) ? toot.NewName : x.Name;
                        var Value = method != null ? (string)method.Invoke(target, new object[] { x.GetValue(target) }) : ToJSON(x.GetValue(target));
                        return "\"" + Name + "\"" + ":" + Value;
                    }).Where(x => !string.IsNullOrEmpty(x))) + "}";
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
        [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
        public class ConvertAttribute : Attribute
        {
            public enum ConvertIgnore { Both, Out, In }
            public string ConvertIn { get; set; }
            public string ConvertOut { get; set; }
            public ConvertIgnore Ignore { get; set; }
            public string NewName { get; set; }
            public ConvertAttribute() { }
        }
    }
}
