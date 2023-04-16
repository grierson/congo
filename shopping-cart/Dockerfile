FROM clojure:temurin-17-alpine

# RUN apk --no-cache add curl
# RUN bash < <(curl -s https://raw.githubusercontent.com/babashka/babashka/master/install)

WORKDIR /app

COPY deps.edn ./
COPY src/ ./src/
COPY resources/ ./resources/
COPY Makefile ./

EXPOSE 3000

ENTRYPOINT [""]
