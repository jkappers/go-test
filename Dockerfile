FROM golang:1.25-alpine AS builder

WORKDIR /app
COPY main.go .

# Initialize go module if not exists
RUN go mod init github.com/example/go-sample || true
RUN go mod tidy

# Build the application
RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o server main.go

FROM alpine:3.21

# Install ca-certificates and wget for health checks
RUN apk --no-cache add ca-certificates wget

WORKDIR /app
COPY --from=builder /app/server .

# Add non-root user
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser && \
    chown -R appuser:appuser /app

USER appuser

EXPOSE 2593

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:2593/health || exit 1

CMD ["./server"]
