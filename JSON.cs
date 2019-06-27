public class Recursive
    {
        public Dictionary<string, List<string>> Values { get; set; } = new Dictionary<string, List<string>>();
        public string Value { get; set; }
        public Recursive Replace(string name, string pattern) => Replace(this, name, pattern);
        public static Recursive Replace(string name, string pattern, string text) => Replace(new Recursive() { Value = text }, name, pattern);
        private static Recursive Replace(Recursive r,string name,string pattern)
        {
            List<string> vals = new List<string>();
            r.Value = Regex.Replace(r.Value, pattern, (m) =>
            {
                vals.Add(m.Groups[1].Value);
                return name + (vals.Count-1);
            });
            r.Values.Add(name, vals);
            return r;
        }
    }
    public class JSON
    {
        public static JSONObject Parse(string text)
        {
            text = Regex.Replace(text, @"[\t\r\n]", "");

            var Properties = Recursive.Replace("P", @"(?<=[{,\[]\s*)(?<!\\)""([\w-]+)(?<!\\)""(?=\s*:)",text).
                Replace("S", @"(?<!\\)""(.*?)(?<!\\)""(?=,|}|])");

            Match getMatch(string subject, string pattern) => Regex.IsMatch(subject, pattern) ? Regex.Match(subject, pattern) : null;
            IEnumerable<Match> getMatches(string subject, string pattern) => Regex.IsMatch(subject, pattern) ? Regex.Matches(subject, pattern).OfType<Match>() : null;
            JSONSequencer extractBrackets(string subject)
            {
                var sequencer = new JSONSequencer();
                var r = @"[{\[]";
                for (var e = getMatch(subject, r); e != null; e = getMatch(subject, r))
                {
                    var brackets = getMatches(subject.Substring(e.Index + 1), e.Value == "{" ? @"[{}]" : @"[\[\]]").ToList();
                    var end = brackets.Where(x => Regex.IsMatch(x.Value, @"[}\]]")).
                        FirstOrDefault(x => brackets.Where(y => Regex.IsMatch(y.Value, @"[{\[]") && y.Index < x.Index).Count() == brackets.Where(y => Regex.IsMatch(y.Value, @"[}\]]") && y.Index < x.Index).Count());
                    if (end == null) throw new JSONException(e.Value,text,subject);
                    var subst = subject.Substring(e.Index + 1).Substring(0, end.Index);
                    if (e.Value == "{") sequencer.Objects.Add(extractBrackets(subst)); else sequencer.Arrays.Add(extractBrackets(subst));
                    subject = subject.Substring(0, e.Index) + (e.Value == "{" ? "O" + (sequencer.Objects.Count - 1) : "L" + (sequencer.Arrays.Count - 1)) + subject.Substring(end.Index + e.Index + 2);
                }
                sequencer.SequenceString = subject;
                return sequencer;
            }

            var ObjectPoly = extractBrackets(Regex.Replace(Properties.Value, @"\s", ""));
            object getValue(JSONSequencer obj ,string type, string number)
            {
                if (String.IsNullOrEmpty(type) && String.IsNullOrEmpty(number)) return null;
                switch (type)
                {
                    case "O": return formObject(obj.Objects[int.Parse(number)]);
                    case "S": return Properties.Values["S"][int.Parse(number)];
                    case "L": return formList(obj.Arrays[int.Parse(number)]);
                    case "null": return null;
                    case "true": return true;
                    case "false": return false;
                    default: return double.Parse(number);
                }
            }
            JSONObject formObject(JSONSequencer obj)=>new JSONObject(getMatches(obj.SequenceString, @"P([0-9]+):([A-z]*)([0-9]*)").ToDictionary(x => Properties.Values["P"][int.Parse(x.Groups[1].Value)], x => getValue(obj, x.Groups[2].Value, x.Groups[3].Value)));
            JSONList formList(JSONSequencer obj) => new JSONList(getMatches(obj.SequenceString, @"(?:^|,)([A-z]*)([0-9]*)").Select(x => getValue(obj, x.Groups[1].Value, x.Groups[2].Value)).ToList());
            return formObject(ObjectPoly.Objects[0]);
        }

        public static T Parse<T>(string text)
        {
            return JSONConverter.ConvertFromJSONType<T>(Parse(text));
        }
        public static JSONObject ConvertTo(object target)
        {
            var c = JSONConverter.ConvertToJSONType(target);
            if (c.GetType() == typeof(JSONObject)) return (JSONObject)c; else return null;
        }
        public static JSONList ConvertTo(IEnumerable target)
        {
            var c = JSONConverter.ConvertToJSONType(target);
            if (c.GetType() == typeof(JSONList)) return (JSONList)c; else return null;
        }
        public static string ToJSONString(object target)
        {
            var c = JSONConverter.ConvertToJSONType(target);
            if (c.GetType() == typeof(JSONObject)) return ((JSONObject)c).ToJSONString();
            if (c.GetType() == typeof(JSONList)) return ((JSONList)c).ToJSONString();
            return null;
        }
        public static string ToJSONCommandSchema<T>()
        {
            string getAsserted(Type type)
            {
                var m = Type.GetTypeCode(type).ToString();
                if (typeof(TypeCode).GetEnumNames().Skip(5).TakeWhile((x, a) => a < 11).Contains(m)) return "number";
                if (m == "String") return "string";
                if (m == "Boolean") return "boolean";
                if (m == "Object" && type.GetInterfaces().FirstOrDefault(x => x.Name == "ICollection" || x.Name == "IEnumerable") != null)return "Array";
                return "object";
            }
            return typeof(T).GetProperties().Aggregate("", (a, c) => a  + " -"+ c.Name + " " + getAsserted(c.PropertyType));
        }

    }


    public class JSONConverter
    {
        public static string ToJSONString(object target)
        {
            if (target == null) return "null";
            else
            {
                var itemType = target.GetType();
                if (itemType == typeof(string)) return "\"" + target + "\"";
                else if (itemType == typeof(JSONObject)) return ((JSONObject)target).ToJSONString();
                else if (itemType == typeof(JSONList)) return ((JSONList)target).ToJSONString();
                else if (itemType == typeof(bool)) return ((bool)target) ? "true" : "false";
                else return target.ToString();
            }
        }
        public static object ConvertFromJSONType(Type ConvertTo, object target)
        {
            if (target != null && ConvertTo != target.GetType())
                if (ConvertTo.IsValueType && target.GetType() == typeof(double)) return Convert.ChangeType(target, ConvertTo);
                else if ((ConvertTo.IsArray || ConvertTo.GetInterface("ICollection") != null || ConvertTo.GetInterface("IEnumerable") != null) && target.GetType() == typeof(JSONList))
                {
                    Type innerType = ConvertTo.IsArray ? ConvertTo.GetElementType() : ConvertTo.GetGenericArguments()[0];
                    var enumerable = ((JSONList)target).Select(x => ConvertFromJSONType(innerType, x)).ToList();
                    var lst = Activator.CreateInstance(ConvertTo, new object[] { enumerable.Count });
                    var mths = lst.GetType().GetMethods().FirstOrDefault(x => x.Name == "Add" || x.Name == "SetValue");
                    int count = 0;
                    if (mths != null)
                        foreach (var i in enumerable)
                            if (mths.Name == "Add") mths.Invoke(lst, new object[] { i });
                            else if (mths.Name == "SetValue") mths.Invoke(lst, new object[] { i, count++ });
                    return lst;
                }
                else if (target.GetType() == typeof(JSONObject))
                {
                    var instance = Activator.CreateInstance(ConvertTo);
                    var table = ConvertTo.GetInterface("IJSONConverter") != null ? ((IJSONConverter)instance).ConversionTable : new JSONConversionTable(instance);
                    table.FromJSON((JSONObject)target);
                    return instance;
                }
                else if (ConvertTo.IsEnum && target.GetType() == typeof(string))
                    return ConvertTo.GetEnumValues().OfType<Enum>().FirstOrDefault(x => x.ToString() == (string)target);
                else return null;
            return target;
        }
        public static T ConvertFromJSONType<T>(object target){
            var converted = ConvertFromJSONType(typeof(T), target);
            return (converted != null && converted.GetType() == typeof(T)) ? (T)converted: default(T);
        }
        public static object ConvertToJSONType(object target)
        {
            if (target == null) return null;
            var t = target.GetType();
            if (t == typeof(string) || t == typeof(bool)) return target;
            if (t.IsEnum) return t.GetEnumName(target);
            if(typeof(TypeCode).GetEnumNames().Skip(5).Take(11).Contains(Type.GetTypeCode(t).ToString())) return Convert.ToInt32(target);

            if (t.IsArray || target.GetType().GetInterface("IEnumerable") != null || target.GetType().GetInterface("ICollection") != null)
            {
                var list = new JSONList();
                foreach (var item in (ICollection)target) list.Add(ConvertToJSONType(item));
                return list;
            }
            return (t.GetInterface("IJSONConverter") != null ? ((IJSONConverter)target).ConversionTable : new JSONConversionTable(target)).ToJSON();
        }
        public static JSONObject FromXML(XDocument xdoc, string TagKey = "tag", string ChildKey = "children", string TextKey = "value")
        {
            JSONObject makeJSONObject(XElement element)
            {
                if (element == null) return null;
                var jsonbject = new JSONObject { [TagKey] = element.Name.LocalName};
                element.Attributes().ToList().ForEach(x => jsonbject.Add(x.Name.LocalName, x.Value));
                var ChildList = new JSONList();
                foreach (var child in element.Elements())
                {
                    if (!child.HasAttributes && child.HasElements) // list
                    {
                        var lst = new JSONList();
                        foreach (var el in child.Elements().Select(x => makeJSONObject(x))) lst.Add(el);
                        jsonbject.Add(child.Name.LocalName,  lst);
                    }
                    else ChildList.Add(makeJSONObject(child));
                }
                if (!string.IsNullOrEmpty(element.Value)) jsonbject[TextKey] = element.Value;
                if (ChildList.Count > 0) jsonbject[ChildKey] = ChildList;
                return jsonbject;
            }
            return makeJSONObject(xdoc.Elements().FirstOrDefault());
        }
    }
    public class JSONSequencer
    {
        public List<JSONSequencer> Objects { get; set; } = new List<JSONSequencer>();
        public List<JSONSequencer> Arrays { get; set; } = new List<JSONSequencer>();
        public string SequenceString { get; set; }
    }
    public class JSONObject : Dictionary<string, object>{
        public JSONObject() { }
        public JSONObject(Dictionary<string,object> pass) => pass.Keys.ToList().ForEach(x => Add(x, pass[x]));
        public string ToJSONString()
        {
            var jsonstring = "{";
            foreach(var key in Keys)
                jsonstring += "\""+key + "\":" + JSONConverter.ToJSONString(this[key]) + ",";
            return jsonstring.TrimEnd(',') + "}";
        }
        public XDocument ToXDocument(string TagKey = "tag",string ChildKey = "children")
        {
            XElement fromObject(JSONObject obj)
            {
                if (obj == null) return null;
                var tag = obj.ContainsKey(TagKey)? obj[TagKey].ToString() : "Object";
                var el = new XElement(tag);
                var sorted = obj.GroupBy(x => x.Value.GetType() == typeof(JSONList) ? "List" : x.Value.GetType() == typeof(JSONObject) ? "Object" : "Attribute").ToDictionary(x=>x.Key,x=>x.ToList());
                if(sorted.ContainsKey("Attribute"))sorted["Attribute"].ForEach(x => { if (x.Key != TagKey) el.SetAttributeValue(x.Key, x.Value); });
                if (sorted.ContainsKey("List")) sorted["List"].ForEach(x =>
                {
                    var lst = fromList((JSONList)x.Value);
                    if (x.Key == ChildKey)
                    {
                        lst.ForEach(y => el.Add(y));
                    }else
                    {
                        var chld = new XElement(x.Key);
                        lst.ForEach(y => chld.Add(y));
                        el.Add(chld);
                    }
                });
                if (sorted.ContainsKey("Object")) sorted["Object"].ForEach(x =>
                {
                    var childEl = new XElement(x.Key);
                    childEl.Add(fromObject((JSONObject)x.Value));
                    el.Add(childEl);
                });
                return el;
            }
            List<XElement> fromList(JSONList list)
            {
                if (list == null) return null;
                return list.Where(y=>y != null).Select(y =>
                {
                    if (y.GetType() == typeof(JSONList))
                    {
                        var childList = new XElement("List");
                        fromList((JSONList)y).ForEach(x=>childList.Add(x));
                        return childList;
                    }
                    else if (y.GetType() == typeof(JSONObject)) return fromObject((JSONObject)y);
                    else return new XElement(y.GetType().Name + ":" +y.GetType().AssemblyQualifiedName) { Value = y.ToString() };
                }).ToList();
            }
            var doc = new XDocument();
            doc.Add(fromObject(this));
            return doc;
        }
        public T ConvertTo<T>() => JSONConverter.ConvertFromJSONType<T>(this);
    }
    public class JSONList : List<object>{
        public JSONList() {  }
        public JSONList(List<object> pass) => AddRange(pass);
        public string ToJSONString()
        {
            var jsonstring = "[";
            foreach (var item in this)
                jsonstring += JSONConverter.ToJSONString(item) + ",";
            return jsonstring.TrimEnd(',') + "]";
        }
    }

    public interface IJSONConverter { JSONConversionTable ConversionTable { get; set; } }

    public enum JSONConversionDirection { toJSON, fromJSON, Both}

    public class JSONConversion
    {
        public Func<JSONConversionDirection, object, object> ConversionDelegate { get; set; }
        public Action<JSONConversionDirection, object> ApplyAction { get; set; }
        public string JSONName { get; set; }
        public string PropertyName { get; set; }
        public bool Ignore { get; set; } = false;
        public JSONConversionDirection ConversionDirection { get; set; } = JSONConversionDirection.Both;
        public JSONConversion() { }
        public JSONConversion(string propertyname) => PropertyName = propertyname;
    }

    public class JSONDefaultConversion
    {
        public Func<string, JSONConversionDirection, object, object> GeneralConverter { get; set; }
        public Action<string, JSONConversionDirection, object> GeneralAction { get; set; }
        public bool Ignore { get; set; }
        public JSONConversion AsJSONConversion(string name)
        {
            var conversion = new JSONConversion(name) {Ignore = Ignore };
            if (GeneralAction != null) conversion.ApplyAction = (A, B) => GeneralAction(name, A, B);
            if (GeneralConverter != null) conversion.ConversionDelegate = (A, B) => GeneralConverter(name, A, B);
            return conversion;
        }
    }

    public class JSONConversionTable : List<JSONConversion>
    {
        public JSONDefaultConversion DefaultConversion { get; set; } = new JSONDefaultConversion();
        private object OwningObject { get; set; }
        public JSONConversionTable(object owningobject)=> OwningObject = owningobject;
        public JSONObject ToJSON()
        {
            var type = OwningObject.GetType();
            var j = new JSONObject();
            foreach(var property in type.GetProperties().Where(x=>x.Name != "ConversionTable"))
            {
                var conversion =  this.FirstOrDefault(x=>x.ConversionDirection != JSONConversionDirection.fromJSON && x.PropertyName == property.Name) ?? DefaultConversion.AsJSONConversion(property.Name);
                conversion.ApplyAction?.Invoke(JSONConversionDirection.toJSON, property.GetValue(OwningObject));
                if (conversion.Ignore) continue;
                var name = conversion.JSONName ?? property.Name;
                var value = conversion.ConversionDelegate != null ? conversion.ConversionDelegate(JSONConversionDirection.toJSON, property.GetValue(OwningObject)) : property.GetValue(OwningObject);
                j.Add(name, JSONConverter.ConvertToJSONType(value));
            }
            return j;
        }
        public void FromJSON(JSONObject jsonobject)
        {
            foreach (var key in jsonobject.Keys.Where(x=>x != "ConversionTable"))
            {
                var conversion = this.FirstOrDefault(x => x.ConversionDirection!= JSONConversionDirection.toJSON &&( x.JSONName == key || x.PropertyName == key)) ?? DefaultConversion.AsJSONConversion(key);
                 conversion.ApplyAction?.Invoke(JSONConversionDirection.fromJSON, jsonobject[key]);
                if (conversion.Ignore) continue;
                var property = OwningObject.GetType().GetProperty(conversion.PropertyName ?? key);
                property?.SetValue(OwningObject,JSONConverter.ConvertFromJSONType(property.PropertyType, conversion.ConversionDelegate != null ? conversion.ConversionDelegate(JSONConversionDirection.fromJSON, jsonobject[key]) : jsonobject[key]));
            }
        }

    }


    public class JSONException : Exception
    {
        public string WholeString { get; set; }
        public string SubString { get; set; }
        public string BracketType { get; set; }
        public JSONException(string type, string wholestring, string substring) : base("Parsing Error")
        {
            WholeString = wholestring;
            SubString = substring;
            BracketType = type;
        }
    }
