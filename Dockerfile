FROM mcr.microsoft.com/dotnet/runtime:5.0

COPY ./src/bin/Release/netcoreapp3.1/publish/ /darkrift-cli
WORKDIR /darkrift-cli
ENTRYPOINT ["dotnet", "/darkrift-cli/darkrift"]
