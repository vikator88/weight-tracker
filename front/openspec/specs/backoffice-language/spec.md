# backoffice-language Specification

## Purpose

Lets a backoffice user choose the interface language from the header, and defines which language the backoffice
starts in.

## Requirements

### Requirement: The header offers the supported languages

The language selector SHALL show the current language code and open a menu with "Español" and "English", marking the
current language.

#### Scenario: Open the language menu in Spanish

- **WHEN** the backoffice is in Spanish and the visitor clicks "Cambiar idioma"
- **THEN** the menu shows "Español" and "English", and "Español" is marked as current

### Requirement: Choosing a language switches the interface at once

Choosing a language SHALL switch every visible string of the backoffice to that language without navigating, and SHALL
store it as the preferred language.

#### Scenario: Switch to English

- **WHEN** the visitor chooses "English"
- **THEN** the screen shows its strings in English, for example the placeholder "Type for search..."
- **AND** the visitor stays on the current screen
- **AND** `en` is stored as the preferred language

#### Scenario: Storing the choice fails

- **WHEN** the visitor chooses "English" and storing the preference fails
- **THEN** the screen still switches to English and no error is shown

### Requirement: The backoffice starts in the preferred language

On load, the backoffice SHALL use the stored preferred language when it is a supported language. Otherwise it SHALL use
the browser's first supported language, falling back to Spanish. The document's language SHALL always match the
language in use.

#### Scenario: Reload after choosing English

- **WHEN** the visitor chose English and reloads the page
- **THEN** the backoffice is shown in English

#### Scenario: No stored choice

- **WHEN** no preferred language is stored and the browser prefers English
- **THEN** the backoffice is shown in English

#### Scenario: Unsupported or unreadable stored value

- **WHEN** the stored preferred language is `fr`, or reading it fails
- **THEN** the backoffice uses the browser's language as if nothing were stored
