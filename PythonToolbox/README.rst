QuantConnect.com Interaction via API (python edition)
=====================================================

What is it
----------

**quantconnect** is a Python package providing interaction via API with `QuantConnect.com <https://www.quantconnect.com>`_.

Installation Instructions
-------------------------

This package can be installed with pip:

   >>> pip install quantconnect -U

Local installation:

   >>> pip install -e Lean/PythonToolbox

Enter Python's interpreter and type the following commands:

   >>> from quantconnect.api import Api
   >>> api = Api(your-user-id, your-token)
   >>> p = api.list_projects()
   >>> print(len(p['projects']))

For your user id and token, please visit `your account page <https://www.quantconnect.com/account>`_.

Create the package
------------------

Edit setup.py to set the desired package version. Then, create the distribution and upload it with `twine <https://pypi.python.org/pypi/twine>`_.:

   >>> python setup.py sdist
   >>> twine upload  dist/*

Lean Report Creator
-------------------
Create beautiful HTML/PDF reports for sharing your LEAN backtest results in Python.
For more information see the [Tutorial](https://www.quantconnect.com/tutorials/open-source/lean-report-creator)
