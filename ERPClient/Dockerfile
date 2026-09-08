FROM node:22-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./

# `npm ci` degil: @angular/compiler-cli chokidar ^4, sass ve webpack-dev-server
# ^3 istiyor. npm ikisini de barindiran gecerli bir agac kuruyor ama `npm ci`
# dogrulayicisi bunu tutarsiz sayip EUSAGE ile duruyor. `npm install` ayni
# kilit dosyasini okuyor ve bu agaci kabul ediyor.
RUN npm install --no-audit --no-fund

COPY . .
RUN npm run build -- --configuration production

FROM caddy:2-alpine AS runtime
COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /src/dist/erpclient/browser /srv

EXPOSE 80 443
