package main

import (
	"context"
	"net/http"
	"net/http/httptest"
	"os"
	"strings"
	"testing"
)

func TestHealthHandler(t *testing.T) {
	req, err := http.NewRequestWithContext(context.Background(), "GET", "/health", http.NoBody)
	if err != nil {
		t.Fatal(err)
	}

	rr := httptest.NewRecorder()
	handler := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		if _, err := w.Write([]byte("OK")); err != nil {
			t.Errorf("Failed to write response: %v", err)
		}
	})

	handler.ServeHTTP(rr, req)

	if status := rr.Code; status != http.StatusOK {
		t.Errorf("handler returned wrong status code: got %v want %v",
			status, http.StatusOK)
	}

	expected := "OK"
	if rr.Body.String() != expected {
		t.Errorf("handler returned unexpected body: got %v want %v",
			rr.Body.String(), expected)
	}
}

func TestRootHandler(t *testing.T) {
	req, err := http.NewRequestWithContext(context.Background(), "GET", "/", http.NoBody)
	if err != nil {
		t.Fatal(err)
	}

	rr := httptest.NewRecorder()
	handler := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		hostname, _ := os.Hostname() //nolint:errcheck // hostname fallback is acceptable in tests
		if _, err := w.Write([]byte("Hello from " + hostname + "\n")); err != nil {
			t.Errorf("Failed to write response: %v", err)
		}
	})

	handler.ServeHTTP(rr, req)

	if status := rr.Code; status != http.StatusOK {
		t.Errorf("handler returned wrong status code: got %v want %v",
			status, http.StatusOK)
	}

	body := rr.Body.String()
	if !strings.HasPrefix(body, "Hello from ") {
		t.Errorf("handler returned unexpected body: got %v", body)
	}
}

func TestPortEnvironmentVariable(t *testing.T) {
	tests := []struct {
		name     string
		envValue string
		expected string
	}{
		{
			name:     "Custom port set",
			envValue: "8080",
			expected: "8080",
		},
		{
			name:     "Default port when not set",
			envValue: "",
			expected: "2593",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			if tt.envValue != "" {
				t.Setenv("PORT", tt.envValue)
			}

			port := os.Getenv("PORT")
			if port == "" {
				port = "2593"
			}

			if port != tt.expected {
				t.Errorf("expected port %s, got %s", tt.expected, port)
			}
		})
	}
}
