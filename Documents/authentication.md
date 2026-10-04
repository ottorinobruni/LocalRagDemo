# Product API Authentication

The Product API uses JWT Bearer authentication.

## JWT Authority

The JWT authority is configured using the environment variable:

`PRODUCT_API_JWT_AUTHORITY`

The value must match the issuer of the access token.

## JWT Audience

The expected audience is configured using:

`PRODUCT_API_JWT_AUDIENCE`

The value must match the audience contained in the access token.

Both values must be configured in the Kubernetes deployment.

## Authentication Failures

HTTP 401 responses can occur when:

- the JWT authority does not match the token issuer
- the configured audience does not match the token audience
- the access token has expired
- the Authorization header is missing or invalid

When authentication problems occur, verify both JWT configuration values before investigating other parts of the application.