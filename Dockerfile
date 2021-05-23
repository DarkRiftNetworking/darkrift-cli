FROM mcr.microsoft.com/dotnet/runtime:3.1-alpine

COPY ./src/bin/Release/netcoreapp3.1/publish/ /darkrift-cli

RUN chmod +x /darkrift-cli/darkrift \
  && apk update \
  && apk add bash

# Install .NET Core 2.0 to run server
RUN dotnet_version=2.0.9 \
    && wget -O dotnet.tar.gz https://dotnetcli.azureedge.net/dotnet/Runtime/$dotnet_version/dotnet-runtime-$dotnet_version-linux-musl-x64.tar.gz \
    && dotnet_sha512='e785b9b488b5570708eb060f9a4cb5cf94597d99a8b0a3ee449d2e5df83771c1ba643a87db17ae6727d0e2acb401eca292fb8c68ad92eeb59d7f0d75eab1c20a' \
    && echo "$dotnet_sha512  dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -C /usr/share/dotnet -oxzf dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm dotnet.tar.gz

VOLUME [ "/project" ]
WORKDIR /project

HEALTHCHECK --interval=10s --timeout=3s \
  CMD curl -f http://localhost:10666/health || exit 1

ENTRYPOINT ["/darkrift-cli/darkrift"]
CMD ["run"]
