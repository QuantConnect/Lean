using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace Python.Runtime
{
    public static class ConverterExtension
    {
        public static PyObject ToPython(this object o)
        {
            return new PyObject(IntPtr.Zero);
        }
    }

    public interface IPyDisposable : IDisposable
    {
        IntPtr[] GetTrackedHandles();
    }

    public class PyObject : DynamicObject, IEnumerable, IPyDisposable
    {
        protected internal IntPtr obj = IntPtr.Zero;
        public PyObject(IntPtr ptr)
        {
            obj = ptr;
        }
        protected PyObject()
        {
        }
        ~PyObject()
        {
        }
        public IntPtr Handle
        {
            get { return IntPtr.Zero; }
        }

        public T As<T>()
        {
            return (T)(this as object);
        }

        public string Repr()
        {
            return "";
        }

        public bool HasAttr(PyObject name)
        {
            return false;
        }
        public bool HasAttr(string name)
        {
            return false;
        }
        public PyObject GetAttr(string name)
        {
            return new PyObject();
        }

        public PyObject GetPythonType()
        {
            return new PyObject();
        }

        public PyObject Invoke(params PyObject[] args)
        {
            return new PyObject();
        }

        public PyObject InvokeMethod(string name, params PyObject[] args)
        {
            return new PyObject();
        }
        public object AsManagedObject(Type t)
        {
            return new PyObject();
        }

        public PyObject GetIterator()
        {
            throw new PythonException();
        }

        public bool IsCallable()
        {
            return false;
        }

        public bool IsIterable()
        {
            return false;
        }

        public static PyObject Import(string name)
        {
            return PythonEngine.ImportModule(name);
        }

        public virtual void SetItem(PyObject key, PyObject value)
        {

        }
        public virtual void SetItem(string key, PyObject value)
        {

        }
        public virtual void SetItem(int index, PyObject value)
        {

        }

        public virtual PyObject GetItem(PyObject key)
        {
            return new PyObject();
        }

        public virtual PyObject GetItem(string key)
        {
            return new PyObject();
        }

        public virtual PyObject GetItem(int index)
        {
            return new PyObject();
        }


        public IEnumerator GetEnumerator()
        {
            return new PyIter(this);
        }

        public IntPtr[] GetTrackedHandles()
        {
            return new IntPtr[] { obj };
        }

        public PyList Dir()
        {
            throw new PythonException();
        }
        public void Dispose()
        {
        }
    }

    public class PyIter : PyObject, IEnumerator<object>
    {
        public PyIter(IntPtr ptr) : base(ptr)
        {

        }

        public PyIter(PyObject iterable)
        {

        }

        public object Current => throw new NotImplementedException();

        public bool MoveNext()
        {
            return true;
        }

        public void Reset()
        {

        }
    }
    public class PyList : PyObject, IDisposable
    {
        public int Length()
        {
            return 0;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public PyList(PyObject o)
        {
        }
    }

    public class PythonException : System.Exception
    {
        public PythonException()
        {

        }

        ~PythonException()
        {
        }
    }

    public static class Py
    {
        public static GILState GIL()
        {
            return new GILState();
        }

        public class GILState : IDisposable
        {
            internal GILState()
            {
            }

            public void Dispose()
            {
            }

            ~GILState()
            {
                Dispose();
            }
        }
        public static PyObject Import(string name)
        {
            return new PyObject(IntPtr.Zero);
        }

        public class KeywordArguments : PyDict
        {
        }

        public static KeywordArguments kw(params object[] kv)
        {
            var dict = new KeywordArguments();
            return dict;
        }

    }


    public class PythonEngine : IDisposable
    {
        public static string Version
        {
            get { return "5"; }
        }

        public PythonEngine()
        {
        }

        public PythonEngine(params string[] args)
        {
        }

        public void Dispose()
        {
        }
        public static void Initialize()
        {

        }

        public static IntPtr BeginAllowThreads()
        {
            return IntPtr.Zero;
        }

        public static PyObject ImportModule(string name)
        {
            return new PyObject(IntPtr.Zero);
        }
        public static PyObject ModuleFromString(string name, string code)
        {
            return new PyObject(IntPtr.Zero);
        }
        public static void Exec(string code, IntPtr? globals = null, IntPtr? locals = null)
        {

        }

    }

    public class PyDict : PyObject
    {
        public static bool IsDictType(PyObject value)
        {
            return false;
        }
        public PyDict(IntPtr ptr) : base(ptr)
        {
        }
        public PyDict()
        {
        }
        public PyDict(PyObject o)
        {
        }
    }

    public class PyTuple
    {
        public PyTuple(IntPtr ptr) 
        {
        }
        public PyTuple(PyObject o)
        {
        }

        public PyTuple()
        {
        }
        public PyTuple(PyObject[] items)
        {
        }

        public static bool IsTupleType(PyObject value)
        {
                return false;
        }

        public static PyTuple AsTuple(PyObject value)
        {
            return new PyTuple(IntPtr.Zero);
        }
    }

    public class PyString : PyObject, IEnumerable
    {
        public PyString(IntPtr ptr)
        {
        }
        public PyString(PyObject o)
        {
        }
        public PyString(string s)
        {
        }
        public static bool IsStringType(PyObject value)
        {
            return false;
        }
    }
}
