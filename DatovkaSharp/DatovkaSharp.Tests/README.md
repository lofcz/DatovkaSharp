# DatovkaSharp Tests

This directory contains integration tests for the DatovkaSharp library.

## Setup

To run the tests, you need to create test accounts and configure credentials.

### 1. Create Test Accounts

Create two test accounts on the Czech Data Box test environment:
- Visit: https://info.mojedatovaschranka.cz/info/cs/74.html
- Follow the instructions to create test accounts
- You will receive login credentials (username and password) for each account

### 2. Configure Test Credentials

1. Copy the example configuration file:
   ```bash
   cp appCfg.example.json appCfg.json
   ```

2. Edit `appCfg.json` and fill in your test credentials:
   ```json
   {
     "account1": {
       "username": "your-first-test-account-username",
       "password": "your-first-test-account-password"
     },
     "account2": {
       "username": "your-second-test-account-username",
       "password": "your-second-test-account-password"
     }
   }
   ```

3. The `appCfg.json` file is gitignored to prevent committing credentials

### 3. Run Tests

```bash
dotnet test
```

## Test Structure

- **DatovkaClientTests.cs** - Tests for basic client operations (connection, info, stats)
- **MessageOperationsTests.cs** - Tests for sending/receiving messages
- **MessageBuilderTests.cs** - Tests for the fluent message builder
- **SearchTests.cs** - Tests for data box search functionality
- **AttachmentTests.cs** - Tests for attachment handling

## Notes

- Tests use **Account1** as the primary test account
- **Account2** is used as a recipient for message sending tests
- Some tests are skipped by default (marked with `[Ignore]`) if they require specific preconditions
- All tests run against the **test environment**, not production

## Configuration File

The `TestConfiguration` class automatically loads `appCfg.json` on first access and validates that:
- The file exists
- Both accounts have non-empty credentials
- The JSON is properly formatted

If configuration is missing or invalid, tests will fail with a helpful error message pointing you to this documentation.

