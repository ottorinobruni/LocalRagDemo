# Product API Observability

The Product API writes structured application logs to standard output.

These logs can be collected by the logging infrastructure used by the Kubernetes environment.

## Authentication Logs

Authentication failures are logged using the category:

`ProductApi.Authentication`

JWT validation failures can include information about:

- invalid issuer
- invalid audience
- expired access tokens
- missing authentication headers

These logs should be inspected when requests return HTTP 401.

## Health Endpoint

The Product API exposes:

`/health`

This endpoint can be used by Kubernetes readiness and liveness checks.

The health endpoint confirms that the application is running, but it does not validate that JWT authentication settings are correct.