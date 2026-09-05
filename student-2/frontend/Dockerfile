FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
COPY shared/ui-kit ./shared/ui-kit
COPY student-2/frontend ./student-2/frontend
RUN npm ci
ENV VITE_AUTOMATIONS_API_BASE_URL=/api/automations
RUN npm run build --workspace=student-2-frontend

FROM nginx:1.27-alpine AS serve
COPY student-2/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/student-2/frontend/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]