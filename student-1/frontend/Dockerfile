# Build stage
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
COPY shared/ui-kit ./shared/ui-kit
COPY student-1/frontend ./student-1/frontend
RUN npm ci
ENV VITE_NOTIFICATIONS_API_BASE_URL=/api/notifications
ENV VITE_DEADLINES_API_BASE_URL=/api/deadlines
RUN npm run build --workspace=student-1-frontend

# Serve stage
FROM nginx:1.27-alpine AS serve
COPY student-1/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/student-1/frontend/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
