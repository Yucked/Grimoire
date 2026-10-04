FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:22-alpine
WORKDIR /app
COPY --from=build /app/build .
COPY --from=build /app/package.json .
EXPOSE 3000
ENV NODE_ENV=production
CMD ["node", "index.js"]
