using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Python.Runtime
{
    /// <summary>
    /// use PyClrType to convert between python object and clr object.
    /// From: https://raw.githubusercontent.com/yagweb/pythonnetLab/master/pynetLab/PyConverter.cs
    /// </summary>
    public class PyConverter
    {
        public PyConverter()
        {
            this.Converters = new List<PyClrTypeBase>();
            this.PythonConverters = new Dictionary<IntPtr, Dictionary<Type, PyClrTypeBase>>();
            this.ClrConverters = new Dictionary<Type, Dictionary<IntPtr, PyClrTypeBase>>();
        }

        private List<PyClrTypeBase> Converters;

        private Dictionary<IntPtr, Dictionary<Type, PyClrTypeBase>> PythonConverters;

        private Dictionary<Type, Dictionary<IntPtr, PyClrTypeBase>> ClrConverters;

        public void Add(PyClrTypeBase converter)
        {
            this.Converters.Add(converter);

            Dictionary<Type, PyClrTypeBase> py_converters;
            var state = this.PythonConverters.TryGetValue(converter.PythonType.Handle, out py_converters);
            if (!state)
            {
                py_converters = new Dictionary<Type, PyClrTypeBase>();
                this.PythonConverters.Add(converter.PythonType.Handle, py_converters);
            }
            py_converters.Add(converter.ClrType, converter);

            Dictionary<IntPtr, PyClrTypeBase> clr_converters;
            state = this.ClrConverters.TryGetValue(converter.ClrType, out clr_converters);
            if (!this.ClrConverters.ContainsKey(converter.ClrType))
            {
                clr_converters = new Dictionary<IntPtr, PyClrTypeBase>();
                this.ClrConverters.Add(converter.ClrType, clr_converters);
            }
            clr_converters.Add(converter.PythonType.Handle, converter);
        }

        public void AddObjectType<T>(PyObject pyType, PyConverter converter = null)
        {
            if (converter == null)
            {
                converter = this;
            }
            this.Add(new ObjectType<T>(pyType, converter));
        }

        public void AddListType(PyConverter converter = null)
        {
            this.AddListType<object>(converter);
        }

        public void AddListType<T>(PyConverter converter = null)
        {
            if (converter == null)
            {
                converter = this;
            }
            this.Add(new PyListType<T>(converter));
        }

        public void AddDictType<K, V>(PyConverter converter = null)
        {
            if (converter == null)
            {
                converter = this;
            }
            this.Add(new PyDictType<K, V>(converter));
        }

        public T ToClr<T>(PyObject obj)
        {
            return (T)ToClr(obj, typeof(T));
        }

        public object ToClr(PyObject obj, Type t = null)
        {
            if (obj == null)
            {
                return null;
            }
            PyObject type = obj.GetPythonType();
            Dictionary<Type, PyClrTypeBase> converters;
            var state = PythonConverters.TryGetValue(type.Handle, out converters);
            if (!state)
            {
                throw new Exception($"Type {type.ToString()} not recognized");
            }
            if (t == null || !converters.ContainsKey(t))
            {
                return converters.Values.First().ToClr(obj);
            }
            else
            {
                return converters[t].ToClr(obj);
            }
        }

        public PyObject ToPython(object clrObj, IntPtr? t = null)
        {
            if (clrObj == null)
            {
                return null;
            }
            Type type = clrObj.GetType();
            Dictionary<IntPtr, PyClrTypeBase> converters;
            var state = ClrConverters.TryGetValue(type, out converters);
            if (!state)
            {
                throw new Exception($"Type {type.ToString()} not recognized");
            }
            if (t == null || !converters.ContainsKey(t.Value))
            {
                return converters.Values.First().ToPython(clrObj);
            }
            else
            {
                return converters[t.Value].ToPython(clrObj);
            }
        }
    }

    public abstract class PyClrTypeBase
    {
        protected PyClrTypeBase(string pyType, Type clrType)
        {
            this.PythonType = PythonEngine.Eval(pyType);
            this.ClrType = clrType;
        }

        protected PyClrTypeBase(PyObject pyType, Type clrType)
        {
            this.PythonType = pyType;
            this.ClrType = clrType;
        }

        public PyObject PythonType
        {
            get;
            private set;
        }

        public Type ClrType
        {
            get;
            private set;
        }

        public abstract object ToClr(PyObject pyObj);

        public abstract PyObject ToPython(object clrObj);
    }

    public class PyClrType : PyClrTypeBase
    {
        public PyClrType(PyObject pyType, Type clrType,
            Func<PyObject, object> py2clr, Func<object, PyObject> clr2py)
            : base(pyType, clrType)
        {
            this.Py2Clr = py2clr;
            this.Clr2Py = clr2py;
        }

        private Func<PyObject, object> Py2Clr;

        private Func<object, PyObject> Clr2Py;

        public override object ToClr(PyObject pyObj)
        {
            return this.Py2Clr(pyObj);
        }

        public override PyObject ToPython(object clrObj)
        {
            return this.Clr2Py(clrObj);
        }
    }

    public class StringType : PyClrTypeBase
    {
        public StringType()
            : base("str", typeof(string))
        {
        }

        public override object ToClr(PyObject pyObj)
        {
            return pyObj.As<string>();
        }

        public override PyObject ToPython(object clrObj)
        {
            return new PyString(Convert.ToString(clrObj));
        }
    }

    public class BooleanType : PyClrTypeBase
    {
        public BooleanType()
            : base("bool", typeof(bool))
        {
        }

        public override object ToClr(PyObject pyObj)
        {
            return pyObj.As<bool>();
        }

        public override PyObject ToPython(object clrObj)
        {
            //return new PyBoolean(Convert.ToString(clrObj));
            throw new NotImplementedException();
        }
    }

    public class Int32Type : PyClrTypeBase
    {
        public Int32Type()
            : base("int", typeof(int))
        {
        }

        public override object ToClr(PyObject pyObj)
        {
            return pyObj.As<int>();
        }

        public override PyObject ToPython(object clrObj)
        {
            return new PyInt(Convert.ToInt32(clrObj));
        }
    }

    public class Int64Type : PyClrTypeBase
    {
        public Int64Type()
            : base("int", typeof(long))
        {
        }

        public override object ToClr(PyObject pyObj)
        {
            return pyObj.As<long>();
        }

        public override PyObject ToPython(object clrObj)
        {
            return new PyInt(Convert.ToInt64(clrObj));
        }
    }

    public class FloatType : PyClrTypeBase
    {
        public FloatType()
            : base("float", typeof(float))
        {
        }

        public override object ToClr(PyObject pyObj)
        {
            return pyObj.As<float>();
        }

        public override PyObject ToPython(object clrObj)
        {
            return new PyFloat(Convert.ToSingle(clrObj));
        }
    }

    public class DoubleType : PyClrTypeBase
    {
        public DoubleType()
            : base("float", typeof(double))
        {
        }

        public override object ToClr(PyObject pyObj)
        {
            return pyObj.As<double>();
        }

        public override PyObject ToPython(object clrObj)
        {
            return new PyFloat(Convert.ToDouble(clrObj));
        }
    }

    public class PyPropetryAttribute : Attribute
    {
        public PyPropetryAttribute()
        {
            this.Name = null;
        }

        public PyPropetryAttribute(string name, string py_type = null)
        {
            this.Name = name;
            this.PythonTypeName = py_type;
        }

        public string Name
        {
            get;
            private set;
        }

        public string PythonTypeName
        {
            get;
            private set;
        }

        public IntPtr? PythonType
        {
            get;
            set;
        }
    }

    abstract class ClrMemberInfo
    {
        public string PyPropertyName;

        public IntPtr? PythonType;

        public string ClrPropertyName;

        public Type ClrType;

        public PyConverter Converter;

        public abstract void SetPyObjAttr(PyObject pyObj, object clrObj);

        public abstract void SetClrObjAttr(object clrObj, PyObject pyObj);
    }

    class ClrPropertyInfo : ClrMemberInfo
    {
        public ClrPropertyInfo(PropertyInfo info, PyPropetryAttribute py_info, PyConverter converter)
        {
            this.PropertyInfo = info;
            this.ClrPropertyName = info.Name;
            this.ClrType = info.PropertyType;
            this.PyPropertyName = py_info.Name;
            if (string.IsNullOrEmpty(this.PyPropertyName))
            {
                this.PyPropertyName = info.Name;
            }
            //this.PythonType = converter.Get();
            this.Converter = converter;
        }

        public PropertyInfo PropertyInfo
        {
            get;
            private set;
        }

        public override void SetPyObjAttr(PyObject pyObj, object clrObj)
        {
            var clr_value = this.PropertyInfo.GetValue(clrObj, null);
            var py_value = this.Converter.ToPython(clr_value);
            pyObj.SetAttr(this.PyPropertyName, py_value);
        }

        public override void SetClrObjAttr(object clrObj, PyObject pyObj)
        {
            var py_value = pyObj.GetAttr(this.PyPropertyName);
            var clr_value = this.Converter.ToClr(py_value);
            this.PropertyInfo.SetValue(clrObj, clr_value, null);
        }
    }

    class ClrFieldInfo : ClrMemberInfo
    {
        public ClrFieldInfo(FieldInfo info, PyPropetryAttribute py_info, PyConverter converter)
        {
            this.FieldInfo = info;
            this.ClrPropertyName = info.Name;
            this.ClrType = info.FieldType;
            this.PyPropertyName = py_info.Name;
            if (string.IsNullOrEmpty(this.PyPropertyName))
            {
                this.PyPropertyName = info.Name;
            }
            //this.PythonType = converter.Get();
            this.Converter = converter;
        }

        public FieldInfo FieldInfo;

        public override void SetPyObjAttr(PyObject pyObj, object clrObj)
        {
            var clr_value = this.FieldInfo.GetValue(clrObj);
            var py_value = this.Converter.ToPython(clr_value);
            pyObj.SetAttr(this.PyPropertyName, py_value);
        }

        public override void SetClrObjAttr(object clrObj, PyObject pyObj)
        {
            var py_value = pyObj.GetAttr(this.PyPropertyName);
            var clr_value = this.Converter.ToClr(py_value);
            this.FieldInfo.SetValue(clrObj, clr_value);
        }
    }

    /// <summary>
    /// Convert between Python object and clr object
    /// </summary>
    public class ObjectType<T> : PyClrTypeBase
    {
        public ObjectType(PyObject pyType, PyConverter converter)
            : base(pyType, typeof(T))
        {
            this.Converter = converter;
            this.Properties = new List<ClrMemberInfo>();

            // Get all attributes
            foreach (var property in this.ClrType.GetProperties())
            {
                var attr = property.GetCustomAttributes(typeof(PyPropetryAttribute), true);
                if (attr.Length == 0)
                {
                    continue;
                }
                var py_info = attr[0] as PyPropetryAttribute;
                this.Properties.Add(new ClrPropertyInfo(property, py_info, this.Converter));
            }

            foreach (var field in this.ClrType.GetFields())
            {
                var attr = field.GetCustomAttributes(typeof(PyPropetryAttribute), true);
                if (attr.Length == 0)
                {
                    continue;
                }
                var py_info = attr[0] as PyPropetryAttribute;
                this.Properties.Add(new ClrFieldInfo(field, py_info, this.Converter));
            }
        }

        private PyConverter Converter;

        private List<ClrMemberInfo> Properties;

        public override object ToClr(PyObject pyObj)
        {
            var clrObj = Activator.CreateInstance(this.ClrType);
            foreach (var item in this.Properties)
            {
                item.SetClrObjAttr(clrObj, pyObj);
            }
            return clrObj;
        }

        public override PyObject ToPython(object clrObj)
        {
            var pyObj = this.PythonType.Invoke();
            foreach (var item in this.Properties)
            {
                item.SetPyObjAttr(pyObj, clrObj);
            }
            return pyObj;
        }
    }

    public class PyListType<T> : PyClrTypeBase
    {
        public PyListType(PyConverter converter)
            : base("list", typeof(List<T>))
        {
            this.Converter = converter;
        }

        private PyConverter Converter;

        public override object ToClr(PyObject pyObj)
        {
            var dict = this._ToClr(new PyList(pyObj));
            return dict;
        }

        private object _ToClr(PyList pyList)
        {
            var list = new List<T>();
            foreach (PyObject item in pyList)
            {
                var _item = this.Converter.ToClr<T>(item);
                list.Add(_item);
            }
            return list;
        }

        public override PyObject ToPython(object clrObj)
        {
            return this._ToPython(clrObj as List<T>);
        }

        public PyObject _ToPython(List<T> clrObj)
        {
            var pyList = new PyList();
            foreach (var item in clrObj)
            {
                PyObject _item = this.Converter.ToPython(item);
                pyList.Append(_item);
            }
            return pyList;
        }
    }

    public class PyDictType<K, V> : PyClrTypeBase
    {
        public PyDictType(PyConverter converter)
            : base("dict", typeof(Dictionary<K, V>))
        {
            this.Converter = converter;
        }

        private PyConverter Converter;

        public override object ToClr(PyObject pyObj)
        {
            var dict = this._ToClr(new PyDict(pyObj));
            return dict;
        }

        private object _ToClr(PyDict pyDict)
        {
            var dict = new Dictionary<K, V>();
            foreach (PyObject key in pyDict.Keys())
            {
                var _key = this.Converter.ToClr<K>(key);
                var _value = this.Converter.ToClr<V>(pyDict[key]);
                dict.Add(_key, _value);
            }
            return dict;
        }

        public override PyObject ToPython(object clrObj)
        {
            return this._ToPython(clrObj as Dictionary<K, V>);
        }

        public PyObject _ToPython(Dictionary<K, V> clrObj)
        {
            var pyDict = new PyDict();
            foreach (var item in clrObj)
            {
                PyObject _key = this.Converter.ToPython(item.Key);
                PyObject _value = this.Converter.ToPython(item.Value);
                pyDict[_key] = _value;
            }
            return pyDict;
        }
    }
}