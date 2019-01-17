:

sudo apt-get update && sudo rm -rf /var/lib/apt/lists/*
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://download.mono-project.com/repo/ubuntu xenial main" | sudo tee /etc/apt/sources.list.d/mono-official.list

sudo apt-get update
sudo apt-get install -y binutils mono-complete ca-certificates-mono referenceassemblies-pcl fsharp

sudo apt-get update && sudo apt-get install -y nuget

cd /vagrant/ && nuget restore QuantConnect.Lean.sln
cd /vagrant/ && xbuild QuantConnect.Lean.sln

