FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS builder

WORKDIR /app
COPY src/*.csproj ./
RUN dotnet restore

COPY src/ ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine

# Install wget for health checks
RUN apk --no-cache add wget

WORKDIR /app
COPY --from=builder /app/out/sample .

# Add non-root user
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser && \
    chown -R appuser:appuser /app

USER appuser

EXPOSE 2593

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:2593/health || exit 1

ENTRYPOINT ["./sample"]
