using System;

namespace GridMvc.Filtering.Types
{
    internal sealed class ByteFilterType : FilterTypeBase
    {
        public override Type TargetType
        {
            get { return typeof (Byte); }
        }

        public override GridFilterType GetValidType(GridFilterType type)
        {
            switch (type)
            {
                case GridFilterType.Equals:
                case GridFilterType.GreaterThan:
                case GridFilterType.GreaterThanOrEquals:
                case GridFilterType.LessThan:
                case GridFilterType.LessThanOrEquals:
                    return type;
                default:
                    return GridFilterType.Equals;
            }
        }

        public override object GetTypedValue(string value)
        {
            byte bt;
            if (!byte.TryParse(value, out bt))
                return null;
            return bt;
        }
        
        public virtual object DeserializeObject(object value, Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            string str = value as string;

            if (type == typeof (Guid) && string.IsNullOrEmpty(str))
                return default(Guid);

            if (value == null)
                return null;
            
            object obj = null;

            if (str != null)
            {
                if (str.Length != 0) // We know it can't be null now.
                {
                    if (type == typeof(DateTime) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(DateTime)))
                        return DateTime.ParseExact(str, Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    if (type == typeof(DateTimeOffset) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(DateTimeOffset)))
                        return DateTimeOffset.ParseExact(str, Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    if (type == typeof(Guid) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(Guid)))
                        return new Guid(str);
                    if (type == typeof(Uri))
                    {
                        bool isValid =  Uri.IsWellFormedUriString(str, UriKind.RelativeOrAbsolute);

                        Uri result;
                        if (isValid && Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out result))
                            return result;

												return null;
                    }
                  
									if (type == typeof(string))  
										return str;

									return Convert.ChangeType(str, type, CultureInfo.InvariantCulture);
                }
                else
                {
                    if (type == typeof(Guid))
                        obj = default(Guid);
                    else if (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(Guid))
                        obj = null;
                    else
                        obj = str;
                }
                // Empty string case
                if (!ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(Guid))
                    return str;
            }
            else if (value is bool)
                return value;
            
            bool valueIsLong = value is long;
            bool valueIsDouble = value is double;
            if ((valueIsLong && type == typeof(long)) || (valueIsDouble && type == typeof(double)))
                return value;
            if ((valueIsDouble && type != typeof(double)) || (valueIsLong && type != typeof(long)))
            {
                obj = type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(float) || type == typeof(bool) || type == typeof(decimal) || type == typeof(byte) || type == typeof(short)
                            ? Convert.ChangeType(value, type, CultureInfo.InvariantCulture)
                            : value;
            }
            else
            {
                IDictionary<string, object> objects = value as IDictionary<string, object>;
                if (objects != null)
                {
                    IDictionary<string, object> jsonObject = objects;

                    if (ReflectionUtils.IsTypeDictionary(type))
                    {
                        // if dictionary then
                        Type[] types = ReflectionUtils.GetGenericTypeArguments(type);
                        Type keyType = types[0];
                        Type valueType = types[1];

                        Type genericType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);

                        IDictionary dict = (IDictionary)ConstructorCache[genericType]();

                        foreach (KeyValuePair<string, object> kvp in jsonObject)
                            dict.Add(kvp.Key, DeserializeObject(kvp.Value, valueType));

                        obj = dict;
                    }
                    else
                    {
                        if (type == typeof(object))
                            obj = value;
                        else
                        {
                            obj = ConstructorCache[type]();
                            foreach (KeyValuePair<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>> setter in SetCache[type])
                            {
                                object jsonValue;
                                if (jsonObject.TryGetValue(setter.Key, out jsonValue))
                                {
                                    jsonValue = DeserializeObject(jsonValue, setter.Value.Key);
                                    setter.Value.Value(obj, jsonValue);
                                }
                            }
                        }
                    }
                }
                else
                {
                    IList<object> valueAsList = value as IList<object>;
                    if (valueAsList != null)
                    {
                        IList<object> jsonObject = valueAsList;
                        IList list = null;

                        if (type.IsArray)
                        {
                            list = (IList)ConstructorCache[type](jsonObject.Count);
                            int i = 0;
                            foreach (object o in jsonObject)
                                list[i++] = DeserializeObject(o, type.GetElementType());
                        }
                        else if (ReflectionUtils.IsTypeGenericeCollectionInterface(type) || ReflectionUtils.IsAssignableFrom(typeof(IList), type))
                        {
                            Type innerType = ReflectionUtils.GetGenericListElementType(type);
                            list = (IList)(ConstructorCache[type] ?? ConstructorCache[typeof(List<>).MakeGenericType(innerType)])(jsonObject.Count);
                            foreach (object o in jsonObject)
                                list.Add(DeserializeObject(o, innerType));
                        }
                        obj = list;
                    }
                }
                return obj;
            }
            if (ReflectionUtils.IsNullableType(type))
                return ReflectionUtils.ToNullableType(obj, type);
            return obj;
        }        
    }
}
