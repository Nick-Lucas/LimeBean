#!/bin/bash -e
sudo add-apt-repository -y ppa:yjwong/libunwind
sudo apt-get -y update
sudo apt-get install libunwind8 libssl-dev unzip  