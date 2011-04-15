#!/bin/bash

echo "Starting 3 Radical servers"

mono bin/Debug/server.exe server.config &
SERVER1=$(echo $!)

mono bin/Debug/server.exe server1.config &
SERVER2=$(echo $!)

mono bin/Debug/server.exe server2.config &
SERVER3=$(echo $!)

read -a foo

kill $SERVER1
kill $SERVER2
kill $SERVER3

