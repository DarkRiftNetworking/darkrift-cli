FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine

COPY ./src/bin/Release/netcoreapp3.1/publish/ /darkrift-cli

RUN mkdir /project
VOLUME [ "/project" ]
WORKDIR /project

HEALTHCHECK --interval=10s --timeout=3s \
  CMD curl -f http://localhost:10666/ || exit 1

ENTRYPOINT ["dotnet", "/darkrift-cli/darkrift"]
