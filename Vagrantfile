# -*- mode: ruby -*-
# vi: set ft=ruby :

# Vagrantfile API/syntax version. Don't touch unless you know what you're doing!
VAGRANTFILE_API_VERSION = "2"

Vagrant.configure(VAGRANTFILE_API_VERSION) do |config|
  config.vm.box = "generic/ubuntu1604"

  config.vm.provider "virtualbox" do |vb|
    #
    # The conversion utility appears to use a lot of CPU, so lets give
    # it some more resources to work with.
    #
    vb.memory = "2048"
    vb.cpus = "2"
  end

  # Create a private network, which allows host-only access to the machine
  # using a specific IP.
  config.vm.network "private_network", ip: "192.168.33.11"

  # Share an additional folder to the guest VM. The first argument is
  # the path on the host to the actual folder. The second argument is
  # the path on the guest to mount the folder. And the optional third
  # argument is a set of non-required options.
  # config.vm.synced_folder "../data", "/vagrant_data"
  config.vm.synced_folder ".", "/vagrant", :nfs => true,
  :mount_options => ['rw', 'tcp', 'nordirplus', 'nolock', 'local_lock=none']

  config.vm.provision "shell", path: "vagrant-provision.sh"

end
