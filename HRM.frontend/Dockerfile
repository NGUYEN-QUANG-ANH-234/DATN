# syntax=docker/dockerfile:1

FROM node:22-alpine AS deps
WORKDIR /app

COPY HRM.frontend/package*.json ./
RUN npm ci

FROM deps AS build
WORKDIR /app

ARG VITE_BACKEND_URL=
ARG VITE_API_BASE_URL=/api/v1
ARG VITE_GOOGLE_CLIENT_ID=

ENV VITE_BACKEND_URL=${VITE_BACKEND_URL}
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_GOOGLE_CLIENT_ID=${VITE_GOOGLE_CLIENT_ID}

COPY HRM.frontend/ .
RUN npm run build

FROM nginx:1.27-alpine AS final

COPY HRM.frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1
