# Spec Delta

## REMOVED Requirements

### Requirement: The app session lasts for the running process

**Reason**: The app now stores its tokens in persistent on-device storage, so a session no longer ends when the app
restarts.

**Migration**: None. Nothing outlived the process before, so there is no stored session to move. Replaced by "The app
session survives a restart".

## ADDED Requirements

### Requirement: The app session survives a restart

In the app, the stored tokens SHALL be kept in persistent on-device storage. They SHALL still be there after the app
is closed and opened again.

#### Scenario: Restart after login

- **WHEN** an athlete logs in and the app restarts
- **THEN** reading the session returns the tokens from that login

#### Scenario: Restart after signup

- **WHEN** an athlete signs up and the app restarts
- **THEN** reading the session returns the tokens from that signup
