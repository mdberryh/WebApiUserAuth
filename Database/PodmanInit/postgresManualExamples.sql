-- Create a user
SELECT add_user('alice', 'alice@example.com', 'Secure!Pass1');

-- Attempt to log in
SELECT authenticate_user('alice', 'Secure!Pass1', '127.0.0.1', 'Postman');

-- Trigger failed logins (and lockout after 5)
SELECT authenticate_user('alice', 'wrongpass', '127.0.0.1', 'Postman');

-- Gen refresh tokens
SELECT generate_refresh_token(1, '127.0.0.1', 'MyUserAgent/1.0'); -- we could change this to use the id rather than name.
SELECT validate_refresh_token('<raw-token-here>');
SELECT revoke_refresh_token('<raw-token-here>');


-- Generate a reset token
SELECT generate_password_reset_token('alice@example.com');

-- Reset password using token (replace <token> with actual one)
SELECT reset_password('<token>', 'New!Pass2');

-- Manually unlock
SELECT unlock_user('alice');
