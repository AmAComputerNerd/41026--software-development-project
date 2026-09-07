# Build stage
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
COPY shared/ui-kit ./shared/ui-kit
COPY student-4/frontend ./student-4/frontend
RUN npm ci
# The frontend's API base URL is the path prefix that the shared-shell
# nginx will route. With VITE_ACCOUNT_API_BASE_URL=/api, calls like
# `login(...)` (POST /api/auth/login) and `createUser(...)`
# (POST /api/users) line up with the shared-shell's `/api/auth/`,
# `/api/users/`, `/api/students/`, `/api/teachers/` location blocks.
# Using /api/account (the historical value) would 404 because the
# shared-shell has no such route.
ENV VITE_ACCOUNT_API_BASE_URL=/api
RUN npx --workspace=student-4-frontend vite build

# Serve stage
FROM nginx:1.27-alpine AS serve
COPY student-4/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/student-4/frontend/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]