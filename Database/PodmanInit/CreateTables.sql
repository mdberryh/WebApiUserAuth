-- Enable pgcrypto extension for cryptographic functions
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- USERS TABLE
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username TEXT UNIQUE NOT NULL,
    email TEXT UNIQUE NOT NULL,
    salt TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    failed_attempts INTEGER DEFAULT 0,
    account_locked BOOLEAN DEFAULT FALSE,
    last_failed_login TIMESTAMPTZ,
    reset_token TEXT,
    reset_token_expiration TIMESTAMPTZ
);

-- LOGIN ATTEMPTS LOG TABLE
CREATE TABLE IF NOT EXISTS login_attempts (
    id SERIAL PRIMARY KEY,
    username TEXT,
    success BOOLEAN NOT NULL,
    attempt_time TIMESTAMPTZ DEFAULT NOW(),
    ip_address TEXT,
    user_agent TEXT
);

-- REFRESH TOKENS TABLE (hashed tokens + IP and user agent)
DROP TABLE IF EXISTS refresh_tokens CASCADE;

CREATE TABLE refresh_tokens (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash TEXT NOT NULL UNIQUE,
    ip_address TEXT,
    user_agent TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    revoked BOOLEAN DEFAULT FALSE
);

-- FUNCTION: Generate random salt
CREATE OR REPLACE FUNCTION generate_salt()
RETURNS TEXT AS $$
BEGIN
    RETURN encode(gen_random_bytes(16), 'hex');
END;
$$ LANGUAGE plpgsql;

-- FUNCTION: Password complexity check
CREATE OR REPLACE FUNCTION is_password_valid(p_password TEXT)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN LENGTH(p_password) >= 8
       AND p_password ~ '[0-9]'
       AND p_password ~ '[A-Z]'
       AND p_password ~ '[a-z]'
       AND p_password ~ '[!@#$%^&*()]';
END;
$$ LANGUAGE plpgsql;

-- FUNCTION: Add user with password validation and salted hash
CREATE OR REPLACE FUNCTION add_user(p_username TEXT, p_email TEXT, p_password TEXT)
RETURNS TEXT AS $$
DECLARE
    new_salt TEXT := generate_salt();
    hashed_password TEXT;
BEGIN
    IF NOT is_password_valid(p_password) THEN
        RETURN 'Password does not meet complexity requirements.';
    END IF;

    hashed_password := encode(digest(p_password || new_salt, 'sha256'), 'hex');

    INSERT INTO users (username, email, salt, password_hash)
    VALUES (p_username, p_email, new_salt, hashed_password);

    RETURN 'User created successfully.';
EXCEPTION
    WHEN unique_violation THEN
        RETURN 'Username or email already exists.';
END;
$$ LANGUAGE plpgsql;

-- FUNCTION: Authenticate user with logging, lockout, random delay
CREATE OR REPLACE FUNCTION authenticate_user(
    p_username TEXT,
    p_password TEXT,
    p_ip TEXT DEFAULT NULL,
    p_user_agent TEXT DEFAULT NULL
)
RETURNS BOOLEAN AS $$
DECLARE
    stored_hash TEXT;
    stored_salt TEXT;
    account_locked BOOLEAN;
    computed_hash TEXT;
    login_success BOOLEAN := FALSE;
BEGIN
    -- Random delay: 0–500ms to mitigate timing attacks
    PERFORM pg_sleep(random() / 2.0);

    SELECT u.password_hash, u.salt, u.account_locked
    INTO stored_hash, stored_salt, account_locked
    FROM users u
    WHERE u.username = p_username;

    IF NOT FOUND THEN
        INSERT INTO login_attempts (username, success, ip_address, user_agent)
        VALUES (p_username, FALSE, p_ip, p_user_agent);
        RETURN FALSE;
    END IF;

    IF account_locked THEN
        INSERT INTO login_attempts (username, success, ip_address, user_agent)
        VALUES (p_username, FALSE, p_ip, p_user_agent);
        RETURN FALSE;
    END IF;

    computed_hash := encode(digest(p_password || stored_salt, 'sha256'), 'hex');
    login_success := (computed_hash = stored_hash);

    INSERT INTO login_attempts (username, success, ip_address, user_agent)
    VALUES (p_username, login_success, p_ip, p_user_agent);

    IF login_success THEN
        UPDATE users
        SET failed_attempts = 0
        WHERE username = p_username;
        RETURN TRUE;
    ELSE
        UPDATE users
        SET failed_attempts = failed_attempts + 1,
            last_failed_login = NOW(),
            account_locked = (failed_attempts + 1 >= 5)
        WHERE username = p_username;
        RETURN FALSE;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- FUNCTION: Generate password reset token
CREATE OR REPLACE FUNCTION generate_password_reset_token(p_email TEXT)
RETURNS TEXT AS $$
DECLARE
    token TEXT := encode(gen_random_bytes(32), 'hex');
    expires_at TIMESTAMPTZ := NOW() + INTERVAL '1 hour';
BEGIN
    UPDATE users
    SET reset_token = token,
        reset_token_expiration = expires_at
    WHERE email = p_email;

    IF FOUND THEN
        RETURN token;
    ELSE
        RETURN 'Email not found';
    END IF;
END;
$$ LANGUAGE plpgsql;

-- FUNCTION: Reset password using token and auto revoke refresh tokens
CREATE OR REPLACE FUNCTION reset_password(p_token TEXT, p_new_password TEXT)
RETURNS TEXT AS $$
DECLARE
    new_salt TEXT := generate_salt();
    hashed_password TEXT;
    uid INTEGER;
BEGIN
    IF NOT is_password_valid(p_new_password) THEN
        RETURN 'Password does not meet complexity requirements.';
    END IF;

    hashed_password := encode(digest(p_new_password || new_salt, 'sha256'), 'hex');

    UPDATE users
    SET password_hash = hashed_password,
        salt = new_salt,
        reset_token = NULL,
        reset_token_expiration = NULL
    WHERE reset_token = p_token
      AND reset_token_expiration > NOW();

    IF NOT FOUND THEN
        RETURN 'Invalid or expired token.';
    END IF;

    -- Get user id to revoke refresh tokens
    SELECT id INTO uid FROM users WHERE reset_token IS NULL AND password_hash = hashed_password;

    IF uid IS NOT NULL THEN
        UPDATE refresh_tokens
        SET revoked = TRUE
        WHERE user_id = uid;
    END IF;

    RETURN 'Password reset successful.';
END;
$$ LANGUAGE plpgsql;

-- FUNCTION: Generate refresh token (hashed), store with IP and user-agent
CREATE OR REPLACE FUNCTION generate_refresh_token(
    p_user_id INTEGER,
    p_ip TEXT DEFAULT NULL,
    p_user_agent TEXT DEFAULT NULL
)
RETURNS TEXT AS $$
DECLARE
    raw_token TEXT := encode(gen_random_bytes(32), 'hex');
    hashed_token TEXT := encode(digest(raw_token, 'sha256'), 'hex');
    expiry TIMESTAMPTZ := NOW() + INTERVAL '30 days';
BEGIN
    INSERT INTO refresh_tokens (user_id, token_hash, ip_address, user_agent, expires_at)
    VALUES (p_user_id, hashed_token, p_ip, p_user_agent, expiry);

    RETURN raw_token;
END;
$$ LANGUAGE plpgsql;

-- FUNCTION: Validate refresh token (hashed)
CREATE OR REPLACE FUNCTION validate_refresh_token(p_token TEXT)
RETURNS INTEGER AS $$
DECLARE
    hashed_token TEXT := encode(digest(p_token, 'sha256'), 'hex');
    uid INTEGER;
BEGIN
    SELECT user_id INTO uid
    FROM refresh_tokens
    WHERE token_hash = hashed_token
      AND revoked = FALSE
      AND expires_at > NOW();

    RETURN uid; -- NULL if not found
END;
$$ LANGUAGE plpgsql;

-- FUNCTION: Revoke refresh token
CREATE OR REPLACE FUNCTION revoke_refresh_token(p_token TEXT)
RETURNS TEXT AS $$
DECLARE
    hashed_token TEXT := encode(digest(p_token, 'sha256'), 'hex');
BEGIN
    UPDATE refresh_tokens
    SET revoked = TRUE
    WHERE token_hash = hashed_token;

    IF FOUND THEN
        RETURN 'Token revoked.';
    ELSE
        RETURN 'Token not found.';
    END IF;
END;
$$ LANGUAGE plpgsql;

-- FUNCTION: Manually unlock a locked user account
CREATE OR REPLACE FUNCTION unlock_user(p_username TEXT)
RETURNS TEXT AS $$
BEGIN
    UPDATE users
    SET account_locked = FALSE,
        failed_attempts = 0
    WHERE username = p_username;

    IF FOUND THEN
        RETURN 'User unlocked.';
    ELSE
        RETURN 'User not found.';
    END IF;
END;
$$ LANGUAGE plpgsql;
