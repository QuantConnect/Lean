"""
Code in this module gets loaded into the main clr module.

MIT License

Copyright (c) 2006-2017 the contributors of the "Python for .NET" project

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
"""

__version__ = "1.0.5.20"


class clrproperty(object):
    """
    Property decorator for exposing python properties to .NET.
    The property type must be specified as the only argument to clrproperty.

    e.g.::

        class X(object):
            @clrproperty(string)
            def test(self):
                return "x"

    Properties decorated this way can be called from .NET, e.g.::

        dynamic x = getX(); // get an instance of X declared in Python
        string z = x.test; // calls into python and returns "x"
    """

    def __init__(self, type_, fget=None, fset=None):
        self.__name__ = getattr(fget, "__name__", None)
        self._clr_property_type_ = type_
        self.fget = fget
        self.fset = fset

    def __call__(self, fget):
        return self.__class__(self._clr_property_type_,
                              fget=fget,
                              fset=self.fset)

    def setter(self, fset):
        self.fset = fset
        return self

    def getter(self, fget):
        self.fget = fget
        return self

    def __get__(self, instance, owner):
        return self.fget.__get__(instance, owner)()

    def __set__(self, instance, value):
        if not self.fset:
            raise AttributeError("%s is read-only" % self.__name__)
        return self.fset.__get__(instance, None)(value)


class clrmethod(object):
    """
    Method decorator for exposing python methods to .NET.
    The argument and return types must be specified as arguments to clrmethod.

    e.g.::

        class X(object):
            @clrmethod(int, [str])
            def test(self, x):
                return len(x)

    Methods decorated this way can be called from .NET, e.g.::

        dynamic x = getX(); // get an instance of X declared in Python
        int z = x.test("hello"); // calls into python and returns len("hello")
    """

    def __init__(self, return_type, arg_types, clrname=None, func=None):
        self.__name__ = getattr(func, "__name__", None)
        self._clr_return_type_ = return_type
        self._clr_arg_types_ = arg_types
        self._clr_method_name_ = clrname or self.__name__
        self.__func = func

    def __call__(self, func):
        return self.__class__(self._clr_return_type_,
                              self._clr_arg_types_,
                              clrname=self._clr_method_name_,
                              func=func)

    def __get__(self, instance, owner):
        return self.__func.__get__(instance, owner)
