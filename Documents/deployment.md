# Product API Deployment

The Product API is deployed as a container in Kubernetes.

## Configuration

Application configuration is provided through environment variables defined in the Kubernetes Deployment manifest.

The authentication configuration includes:

- `PRODUCT_API_JWT_AUTHORITY`
- `PRODUCT_API_JWT_AUDIENCE`

These values are passed to the Product API container when the pod starts.

## Configuration Changes

After changing authentication environment variables, the Product API pods must be restarted so that the new configuration is loaded.

If authentication problems begin immediately after a deployment, verify that the expected environment variables are present in the deployment configuration and that the running pods contain the updated values.

## Health Checks

Kubernetes can use the Product API `/health` endpoint to verify that the application is running.

A healthy application can still return HTTP 401 when JWT authentication is configured incorrectly.