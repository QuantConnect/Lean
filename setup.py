#!/usr/bin/env python
# -*- coding: utf-8 -*-

# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import os
import sys
from shutil import which, copyfile
from subprocess import check_output, CalledProcessError

MACOS = sys.platform == "darwin"
WIN32 = sys.platform == "win32"
LINUX = not MACOS and not WIN32
LINK = 'https://www.microsoft.com/en-us/download/details.aspx?id=44266'
if MACOS: LINK = 'http://macappstore.org/pkg-config/'

ARCH = 'x64'
VERSION = f"3.6{'.4' if LINUX else ''}"
README = 'https://github.com/QuantConnect/Lean#installation-instructions'
PYTHONNET = 'https://github.com/QuantConnect/pythonnet'
PACKAGES = ['conda', 'pip', 'wheel', 'setuptools', 'pandas']

def _check_output(cmd):
    try:
        output = check_output(cmd)
        output = os.linesep.join([str(x)[2:-1] for x in output.splitlines()])
        cmd.append(output)
    except CalledProcessError as e:
        exit(os.linesep.join([str(x)[2:-1] for x in e.output.splitlines()]))
    return cmd

def check_requirements():
    extra = 'sudo apt-get -y install clang libglib2.0-dev'
    if WIN32:
        extra = f'Visual C++ for Python: {LINK}'
    if MACOS:
        extra = f'pkg-config: {LINK}'

    print(f'''
    Python support in Lean with pythonnet
    =====================================

    Prerequisites:
        - Python {VERSION} {ARCH} with pip
        - LEAN: {README}
        - {extra}
        - git

    It will update {', '.join(PACKAGES)} packages.
    ''')

    version = sys.version[0:(5 if LINUX else 3)]
    arch = "x64" if sys.maxsize > 2**32 else "x86"
    if version != VERSION or arch != ARCH:
        conda_in_linux = LINUX and which('conda') is not None
        print(f'Python {VERSION} {ARCH} is required: version {version} {arch} found.')
        exit(f'Please use "conda install -y python={VERSION}"' if conda_in_linux else '')

    if which('git') is None:
        exit('Git is required and not found in the path. Link to install: https://git-scm.com/downloads')

    if which('pip') is None:
        exit('pip is required and not found in the path.')

    if LINUX:
        if which('clang') is None:
            exit(f'clang is required and not found in the path.{extra}')

        path = '/usr/lib/libpython3.6m.so'
        if not os.path.exists(path):
            print('Add symbolic link to python library in /usr/lib')
            exit(f'sudo ln -s /path/to/miniconda3/lib/libpython3.6m.so {path}')

        _check_output(['pkg-config', '--libs', 'glib-2.0'])

    if WIN32:
        try:
            from winreg import OpenKey, HKEY_LOCAL_MACHINE
            tmp = OpenKey(HKEY_LOCAL_MACHINE, '')
            OpenKey(tmp, r'SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5')
        except:
            tmp = 'https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#windows'
            exit(f'.NET Frameworks 3.5 not installed.{os.linesep}Please visit {tmp} for details')

def install_packages():

    def get_pkgs(cmd):
        cmd.append('list')
        pkgs = _check_output(cmd)[-1]
        pkgs = [pkg[:pkg.find(' ')].strip() for pkg in pkgs.splitlines()]
        return set(PACKAGES) & set(pkgs)

    print('''
    Install/updates required packages
    ---------------------------------
    ''')
    conda = which('conda')
    if conda is not None:
        pkgs = get_pkgs([conda])
        for pkg in PACKAGES:
            cmd = 'update' if pkg in pkgs else 'install'
            _check_output([conda, cmd, '-y', pkg])
        print(f'[conda] Successfully installed/updated: {", ".join(get_pkgs([conda]))}')
    else:
        cmd = [sys.executable, '-m', 'pip', 'install', '-U'] + PACKAGES[1:]
        _check_output(cmd)
        print(f'[ pip ] Successfully installed/updated: {", ".join(get_pkgs(cmd[0:3]))}')

def install_pythonnet():
    print('''
    Install/updates pythonnet
    -------------------------
    ''')
    cmd = [sys.executable, '-m', 'pip', 'install', '-U', 'git+' + PYTHONNET]
    return _check_output(cmd)

def get_enable_shared():

    if WIN32:
        return True

    from sysconfig import get_config_var

    if LINUX:
        return get_config_var('Py_ENABLE_SHARED')

    if which('pkg-config') is None:
        exit(f'pkg-config is required and not found in path. Link to install: {LINK}')

    lib = '/Library/Frameworks/Mono.framework/Versions/Current/lib'

    # Create a symlink of framework lib/mono to python lib/mono
    dst = os.path.join(os.path.dirname(sys.executable)[:-3] + 'lib', 'mono')
    if os.path.exists(dst): os.remove(dst)
    os.symlink(os.path.join(lib, 'mono'), dst)

    paths = [path for path, dirs, files in os.walk(lib) if 'mono-2.pc' in files]
    os.environ['PKG_CONFIG_PATH'] = ':'.join(paths)

    if len(paths) == 0:
       exit(f'Could not find "mono-2.pc" in "{lib}" tree.')

    return get_config_var('Py_ENABLE_SHARED')

def get_target_path():

    for path, dirs, files in os.walk('packages'):
        if 'Python.Runtime.dll' in files:
            path = os.path.join(os.getcwd(), path, 'Python.Runtime.dll')
            ori = path[0:-4] + '.ori'

            # Save the original file
            if not os.path.exists(ori):
                os.rename(path, ori)
                copyfile(ori, path)

            return path

    exit(f'Python.Runtime.dll not found in packages tree.{os.linesep}Please restore Nuget packages ({README})')

def update_package_dll(shared, target):

    try:
        if shared:
            import clr
            path = os.path.dirname(clr.__file__)
            file = os.path.join(path, 'Python.Runtime.dll')
        else:
            suffix = 'mac' if MACOS else 'nux'  
            path = os.path.dirname(os.path.dirname(target))
            file = os.path.join(path, 'build', f'Python.Runtime.{suffix}')

        copyfile(file, target)

        exit('Please REBUILD Lean solution to complete pythonnet setup.')

    except Exception as e:
        exit(f'Python.Runtime.dll not found in site-packages directories. Reason: {e}')

def main():
    check_requirements()

    # Installs/updates packages required for pythonnet and Lean
    install_packages()

    shared = get_enable_shared()
    target = get_target_path()

    # Installs/updates pythonnet
    result = install_pythonnet()

    # If pythonnet is installed, copy the file to Lean packages folder
    if result is not None:
        update_package_dll(shared, target)
    elif WIN32:
        exit(f'Failed to install pythonnet. Please install Visual C++ for Python: {LINK}')

if __name__ == "__main__":
    sys.exit(main())