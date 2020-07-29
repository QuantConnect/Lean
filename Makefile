# not necessarily correct but meant to be more helpful than the docs, please kick this along to help new users run things in one step

MKPATH := $(abspath $(lastword $(MAKEFILE_LIST)))
CURDIR := $(shell dirname $(MKPATH))

build_docker_jupyter:
	docker build -t quantconnect/research - < DockerfileJupyter

run_docker_jupyter:
	mkdir -p $(CURDIR)/data
	mkdir -p $(CURDIR)/notebooks
	docker run -it --rm -p 8888:8888 -v $(CURDIR)/data:/home/Data:ro -v $(CURDIR)/notebooks:/home/notebooks --name qc_research quantconnect/research

ssh_docker_jupyter:
	docker exec -it qc_research /bin/bash


setup_local_ubuntu:
	# mono
	sudo apt-get update && sudo rm -rf /var/lib/apt/lists/*
	sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
	echo "deb http://download.mono-project.com/repo/ubuntu stable-xenial/snapshots/5.12.0.226 main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list > /dev/null
	sudo apt-get update
	sudo apt-get install -y binutils mono-complete ca-certificates-mono mono-vbnc nuget referenceassemblies-pcl
	sudo apt-get install -y fsharp
	sudo rm -rf /var/lib/apt/lists/* /tmp/*
	sudo apt-get update
	sudo apt-get install -y binutils mono-complete ca-certificates-mono referenceassemblies-pcl fsharp
	# nuget
	sudo apt-get update
	sudo apt-get install -y nuget

build_local_ubuntu:
	# not working
	nuget restore QuantConnect.Lean.sln
	msbuild QuantConnect.Lean.sln

run:
	# not working
	cd Launcher/bin/Debug
	mono ./QuantConnect.Lean.Launcher.exe

