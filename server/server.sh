#!/bin/bash -x

echo "Starting 3 Radical servers"

(mono bin/Debug/server.exe /home/archie/repos/radical/server/server.config)& 
SERVER1=$(echo $!)

echo "Give time to start up"
sleep 4

(mono bin/Debug/server.exe /home/archie/repos/radical/server/server1.config)&
SERVER2=$(echo $!)

echo "Give time to start up"
sleep 4

(mono bin/Debug/server.exe /home/archie/repos/radical/server/server2.config)&
SERVER3=$(echo $!)

echo "Give time to start up"
sleep 4

echo "Started servers. Ready to rock! \o/"
read -a foo

kill $SERVER1
kill $SERVER2
kill $SERVER3

