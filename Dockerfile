FROM mono:6.6

WORKDIR /usr/src/app

COPY . .

RUN apt-get -y update
RUN apt-get -y install mono-xsp4

RUN nuget restore
RUN msbuild -p:Configuration=Release OnBaseDocsApi

CMD cd OnBaseDocsApi && xsp4
