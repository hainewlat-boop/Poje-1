# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build
RUN npm run build

# Production stage
FROM nginx:alpine AS production
WORKDIR /usr/share/nginx/html

# Remove default nginx static files
RUN rm -rf ./*

# Copy built files
COPY --from=build /app/dist .

# Copy nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Add non-root user
RUN adduser -D -g '' appuser && chown -R appuser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
