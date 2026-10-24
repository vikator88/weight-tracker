# language-preference Specification

## Purpose

Lets a front remember the language the user chose on this device and recall it later, without calling the API.

## Requirements

### Requirement: Storing the preferred language

Storing a preferred language SHALL keep the given language code in key-value storage and return a successful result.
It SHALL NOT call the API.

#### Scenario: Store English

- **WHEN** the preferred language is stored as `en`
- **THEN** the result is successful
- **AND** reading the preferred language afterwards returns `en`

### Requirement: Reading the preferred language

Reading the preferred language SHALL return a successful result holding the stored code, or no value when none is
stored. A missing value SHALL NOT be a failure. It SHALL NOT call the API.

#### Scenario: A language is stored

- **WHEN** key-value storage holds the preferred language `en`
- **THEN** the result is successful and its value is `en`

#### Scenario: Nothing stored

- **WHEN** key-value storage holds no preferred language
- **THEN** the result is successful and its value is empty

### Requirement: The preference outlives the session

Logging out SHALL NOT remove the preferred language.

#### Scenario: Logout keeps the language

- **WHEN** the preferred language is `en` and the user logs out
- **THEN** reading the preferred language still returns `en`

### Requirement: A storage failure is returned as an unexpected error

If key-value storage fails, reading or storing the preferred language SHALL NOT throw. It SHALL return a failed result
with code `UNEXPECTED`.

#### Scenario: Storage rejects the read

- **WHEN** key-value storage fails to read
- **THEN** reading the preferred language fails with code `UNEXPECTED`

#### Scenario: Storage rejects the write

- **WHEN** key-value storage fails to write
- **THEN** storing the preferred language fails with code `UNEXPECTED`
