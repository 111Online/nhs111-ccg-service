FROM microsoft/aspnetcore:2.0.0
ENTRYPOINT["powershell", "./start.ps1"]

WORKDIR /app

COPY./bin/Release/netcoreapp2.0/publish.
COPY start.ps1.

EXPOSE 91
ENV ASPNETCORE_URLS http://0.0.0.0:91
