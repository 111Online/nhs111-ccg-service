FROM microsoft/aspnetcore:2.0.0
ENTRYPOINT ["powershell", "./start.ps1"]

WORKDIR /inetpub/wwwroot

COPY ./bin/Release/netcoreapp2.0 .
COPY start.ps1 .

EXPOSE 92
ENV ASPNETCORE_URLS http://0.0.0.0:92