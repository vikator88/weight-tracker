# Spec Delta

## ADDED Requirements

### Requirement: A session that ends sends the user to login

When the session ends while a guarded screen is shown, because the refresh token was rejected or the user logged
out, the backoffice SHALL replace the current route with `/login` in the history and SHALL stop rendering the guarded
content. This is the same for `TRAINER` and `ADMIN`.

#### Scenario: Refresh token rejected on a guarded screen

- **WHEN** a trainer is on a guarded screen and the session ends because the refresh token was rejected
- **THEN** the trainer ends on `/login`
- **AND** no content of the guarded screen, header or navigation drawer is rendered

#### Scenario: Logout

- **WHEN** an administrator logs out from a guarded screen
- **THEN** the administrator ends on `/login`

### Requirement: A session that starts opens the guarded screens

After a login or signup stores a session, the guarded screens SHALL show their content without a page reload.

#### Scenario: Login then home

- **WHEN** a trainer logs in successfully
- **THEN** the trainer ends on `/` and the home heading "Weight Tracker" is shown
