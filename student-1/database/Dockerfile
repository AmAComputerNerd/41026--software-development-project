FROM postgres:16-alpine

# Set default environment variables
ENV POSTGRES_DB=notifications_db
ENV POSTGRES_USER=postgres
ENV POSTGRES_PASSWORD=postgres

# Copy initial schema & seed SQL script into PostgreSQL init directory
COPY init.sql /docker-entrypoint-initdb.d/01-init.sql

EXPOSE 5432

HEALTHCHECK --interval=10s --timeout=5s --retries=5 --start-period=10s \
    CMD pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB} || exit 1
