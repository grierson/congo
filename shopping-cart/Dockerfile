FROM clojure:temurin-17-alpine

RUN apk --no-cache add curl
RUN bash < <(curl -s https://raw.githubusercontent.com/babashka/babashka/master/install)

WORKDIR /app

COPY deps.edn ./
COPY build.clj ./
COPY bb.edn ./
COPY src/ ./src/
COPY resources/ ./resources/

RUN bb build-uber

EXPOSE 9000

ENTRYPOINT java -jar target/app-standalone.jar
