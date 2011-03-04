CSC=mcs
%.exe: %.cs
	$(CSC) /r:System.Runtime.Remoting /r:ServerRemotingServicesSample.dll $<
%.dll: %.cs
	$(CSC) /r:System.Runtime.Remoting /target:library $<
all: ServerRemotingServicesSample.dll ServerSample.exe ClientSample.exe
ServerRemotingServicesSample.dll: ServerRemotingServicesSample.cs
Server.exe: ServerSample.cs ServerRemotingServicesSample.dll
Client.exe: ClientSample.cs ServerRemotingServicesSample.dll
clean:
	rm -f *.dll *.exe
