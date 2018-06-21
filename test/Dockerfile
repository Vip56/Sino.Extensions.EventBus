FROM microsoft/dotnet:2.0.0-sdk-jessie

ADD ./EventBusUnitTest /usr/local/EventBusUnitTest
WORKDIR /usr/local/EventBusUnitTest/

RUN cd /usr/local/EventBusUnitTest/
RUN dotnet restore -s https://api.nuget.org/v3/index.json
RUN dotnet test