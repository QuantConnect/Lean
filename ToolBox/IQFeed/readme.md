# Setup IQFeed 

## 1. Sign up for [IQFeed](https://www.iqfeed.net/trent/index.cfm?displayaction=start&promo=1996499)  

## 2. Install [IQFeed Client](http://www.iqfeed.net/index.cfm?displayaction=support&section=download)

## 3. Configuration

### 3.1. Docker for Windows

#### Edit Launcher/config.json

1. Set `"environment"` to `"live-interactive-iqfeed"`
2. Set `"iqfeed-host"` to `"host.docker.internal"`

#### Edit run_docker_iqfeed.bat

1. Set `"iqfeed_product_name"`
2. Set `"iqfeed_version"`
3. Set `"iqfeed_login"` (optional)
4. Set `"iqfeed_password"` (optional)

#### Start IQConnect and Lean docker image
Simply by running `run_docker_iqfeed.bat` this will automatically start IQConnect with your provided settings and start the Lean docker image.

### 3.2. Standard on Windows

#### Edit Launcher/config.json
1. Set `"environment"` to `"live-interactive-iqfeed"`
2. Set `"iqfeed-username"`
3. Set `"iqfeed-password"`
4. Set `"iqfeed-productName"`