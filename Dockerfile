FROM microsoft/dotnet:2.0.0-sdk-2.0.2

ADD ./ /usr/local/
WORKDIR /usr/local/src/Sino.Extensions.EventBus/

RUN cd /usr/local/src/
RUN dotnet restore -s http://sinonuget.chinacloudsites.cn/nuget
RUN dotnet build -c Release
RUN cd /usr/local/src/Sino.Extensions.EventBus/
RUN dotnet pack -c Release
RUN cd /bin/Release/
RUN dotnet nuget push *.nupkg -s http://sinonuget.chinacloudsites.cn/nuget/ -k sino5802486..

ENV TZ=Asia/Shanghai
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezoness

EXPOSE 5005

CMD ["dotnet","run","-c","Release"]