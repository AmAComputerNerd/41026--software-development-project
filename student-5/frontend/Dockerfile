# Build stage
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
COPY shared/ui-kit ./shared/ui-kit
COPY student-5/frontend ./student-5/frontend
RUN npm ci
ENV VITE_GRADES_API_BASE_URL=/api/grades
ENV VITE_NOTIFICATIONS_API_BASE_URL=/api/notifications
RUN npm run build --workspace=student-5-frontend

# Serve stage
FROM nginx:1.27-alpine AS serve
COPY student-5/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/student-5/frontend/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
