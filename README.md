# Introduction

**Disclaimer:** It's generally not recommended to implement your own user authentication system. This project is purely experimental. If you choose to proceed, ensure your implementation complies with the [OWASP Secure Coding Practices](https://owasp.org/www-project-secure-coding-practices-quick-reference-guide/stable-en/01-introduction/05-introduction).

This example does **not** use HTTPS by default, as it's intended to run behind an NGINX reverse proxy. In a production environment, you must ensure all traffic is encrypted.

Currently, password hashing is handled by the database. This should be refactored so that password hashing occurs within the web application itself—ensuring that the user's plaintext password never leaves the application.

This Web API uses **JWT tokens** for authentication, implementing both access and refresh tokens. Access tokens are short-lived, requiring the client to frequently refresh them using the refresh token. Refresh tokens are stored in the database and are periodically expired by a background thread running in the API.

---

## Prerequisites

1. Use Podman or Docker to run PostgreSQL and pgAdmin4.
2. *(Optional)* Create a new database to use with the web application.
3. Run `CreateTables.sql` to create the necessary tables and functions.
4. Update the web application's configuration file with the appropriate database connection string.
5. Add some users for testing and experimentation.

---

## Planned Improvements

1. Move password hashing logic from the database to the web API layer.
2. Add support for password "peppers" (random secret values) stored in a `secrets.json` file on the web application.
