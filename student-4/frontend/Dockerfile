# Build stage
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
COPY shared/ui-kit ./shared/ui-kit
COPY student-4/frontend ./student-4/frontend
RUN npm ci
ENV VITE_ACCOUNT_API_BASE_URL=/api/account
RUN npx --workspace=student-4-frontend vite build

# Serve stage
FROM nginx:1.27-alpine AS serve
COPY student-4/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/student-4/frontend/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]