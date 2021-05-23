FROM mcr.microsoft.com/dotnet/runtime:3.1-alpine

COPY ./src/bin/Release/netcoreapp3.1/publish/ /darkrift-cli

RUN chmod +x /darkrift-cli/darkrift \
  && apk update \
  && apk add bash

VOLUME [ "/project" ]
WORKDIR /project

HEALTHCHECK --interval=10s --timeout=3s \
  CMD curl -f http://localhost:10666/health || exit 1

ENTRYPOINT ["/darkrift-cli/darkrift"]
CMD ["run"]
