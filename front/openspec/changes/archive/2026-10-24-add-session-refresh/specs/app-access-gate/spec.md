# Spec Delta

## MODIFIED Requirements

### Requirement: The gate follows the session without navigation

When the session changes from signed out to signed in, the app SHALL show the home screen. When the session changes
from signed in to signed out, including when a refresh token is rejected, the app SHALL show the login screen and
SHALL NOT render any signed-in content. Screens SHALL NOT navigate to grant or deny access.

#### Scenario: Session appears after signup

- **WHEN** the session becomes signed in while the signup screen is shown
- **THEN** the home screen is shown and the signup screen is removed from the history

#### Scenario: Session appears after login

- **WHEN** the session becomes signed in while the login screen is shown
- **THEN** the home screen is shown and the login screen is removed from the history

#### Scenario: Session ends after a rejected refresh

- **WHEN** the session becomes signed out because the refresh token was rejected while a signed-in screen is shown
- **THEN** the login screen is shown and no signed-in content is rendered
- **AND** no screen called a navigation function to get there
