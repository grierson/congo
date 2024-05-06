FROM clojure:latest

WORKDIR /app

COPY deps.edn ./
COPY build.clj ./
COPY bb.edn ./
COPY src/ ./src/
COPY resources/ ./resources/

RUN clojure -T:build uber

EXPOSE 8080

ENTRYPOINT java -jar target/app-standalone.jar
