#!/bin/bash

echo "Starting 3 Radical servers"

mono bin/Debug/server.exe server.config &
SERVER1=$(echo $!)

mono bin/Debug/server.exe server1.config &
SERVER2=$(echo $!)

mono bin/Debug/server.exe server2.config &
SERVER3=$(echo $!)

echo "Server1: $SERVER1, Server2: $SERVER2, Server3: $SERVER3"
echo "Press Enter to exit."

read -a foo
kill $SERVER1
kill $SERVER2
kill $SERVER3

echo "Stopping all servers."
