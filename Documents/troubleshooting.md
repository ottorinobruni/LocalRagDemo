# Product API Troubleshooting

## HTTP 401 After Deployment

If requests start returning HTTP 401 after a deployment, verify the JWT authentication configuration first.

Check the following environment variables:

- `PRODUCT_API_JWT_AUTHORITY`
- `PRODUCT_API_JWT_AUDIENCE`

Compare the configured values with the issuer and audience contained in the access token.

If one of these values was recently changed, verify that the Product API pods were restarted after the configuration update.

## Diagnostics

Authentication failures can be investigated using logs from the category:

`ProductApi.Authentication`

The logs can help identify problems such as:

- invalid issuer
- invalid audience
- expired access tokens
- missing Authorization headers

The `/health` endpoint can be used to confirm that the Product API itself is running.

A successful health check does not mean that authentication is configured correctly.