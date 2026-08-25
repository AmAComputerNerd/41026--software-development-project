# Build stage
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
COPY shared/ui-kit ./shared/ui-kit
COPY student-3/frontend ./student-3/frontend
RUN npm ci
ENV VITE_DEADLINES_API_BASE_URL=/api/deadlines
RUN npm run build --workspace=student-3-frontend

# Serve stage
FROM nginx:1.27-alpine AS serve
COPY student-3/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/student-3/frontend/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
