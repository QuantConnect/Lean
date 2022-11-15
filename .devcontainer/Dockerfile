# Use QuantConnect Research as the base
FROM quantconnect/research:latest

# Install dos2unix utility for converting pesky windows formatting when needed
RUN apt-get update && apt-get install -y dos2unix

# Install QuantConnect Stubs for Python Autocomplete
RUN pip install --no-cache-dir quantconnect-stubs