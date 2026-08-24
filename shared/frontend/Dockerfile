# Build stage
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
COPY shared/ui-kit ./shared/ui-kit
COPY shared/frontend ./shared/frontend
RUN npm ci
RUN npm run build --workspace=shared-frontend

# Serve stage
FROM nginx:1.27-alpine AS serve
COPY shared/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/shared/frontend/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
