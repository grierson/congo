FROM clojure:temurin-17-alpine

WORKDIR /app

COPY deps.edn ./
COPY build.clj ./
COPY bb.edn ./
COPY src/ ./src/
COPY resources/ ./resources/

RUN clojure -T:build uber

EXPOSE 80

ENTRYPOINT java -jar target/app-standalone.jar
