# DatovkaSharp

A `netstandard 2.0` library for communicating with the Czech Data Box (ISDS - Informační systém datových schránek).

## Features

- Full support for Czech Data Box API (v3.0.9)
- Username/Password and Certificate authentication
- Test and Production environment support
- Send and receive messages with attachments
- Download signed messages (ZFO format)
- Search for data boxes
- Password management (change password, get password info)
- Mark messages as read

## Installation

### NuGet
```bash
dotnet add package Datovka
```

### From Source
```bash
git clone https://github.com/yourusername/DatovkaSharp.git
cd DatovkaSharp
dotnet build
```

## Quick Start

### Authentication with Username and Password

```csharp
using DatovkaSharp;

// Create client for test environment
var client = new DatovkaClient(DataBoxEnvironment.Test);

// Login
client.LoginWithUsernameAndPassword("your-username", "your-password");

// Access API
var api = client.Api;
```

### Authentication with Certificate

```csharp
var client = new DatovkaClient(DataBoxEnvironment.Production);
client.LoginWithCertificate("path/to/certificate.pfx", "certificate-password");
```

## Result Wrapper

All API methods return a `DatovkaResult<T>` or `DatovkaListResult<T>` wrapper that provides:
- **Data**: The typed result data
- **StatusCode**: ISDS status code ("0000" = success)
- **StatusMessage**: Human-readable status message
- **IsSuccess**: Boolean indicating success (StatusCode == "0000")
- **RawResponse**: The complete SOAP response object for debugging

### Basic Result Handling

```csharp
var result = await client.Api.GetDataBoxInfoAsync();

// Check success
if (result.IsSuccess)
{
    Console.WriteLine($"Data Box ID: {result.Data.dbID}");
}
else
{
    Console.WriteLine($"Error {result.StatusCode}: {result.StatusMessage}");
}

// Access raw response if needed
var rawResponse = result.RawResponse;
```

### List Results

List operations return `DatovkaListResult<T>` with additional properties:

```csharp
var result = await client.Api.GetListOfReceivedMessagesAsync(days: 90);

Console.WriteLine($"Found {result.Count} messages");

if (result.HasItems)
{
    foreach (var message in result.Data)
    {
        Console.WriteLine($"Subject: {message.dmAnnotation}");
    }
}
```

### Helper Methods

```csharp
// Throw exception if failed
var result = await client.Api.GetDataBoxInfoAsync();
result.ThrowIfFailed(); // Throws DataBoxException if not successful

// Get data or default value
var data = result.OrDefault(fallbackValue);
```

## Usage Examples

### Get Data Box Information

```csharp
// Get information about your data box
var result = await client.Api.GetDataBoxInfoAsync();
if (result.IsSuccess)
{
    Console.WriteLine($"Data Box ID: {result.Data.dbID}");
}

// Get user information
var userResult = await client.Api.GetUserInfoAsync();
if (userResult.IsSuccess)
{
    Console.WriteLine($"User ID: {userResult.Data.userID}");
}

// Get password expiration date
var pwdResult = await client.Api.GetPasswordExpiresAsync();
if (pwdResult.IsSuccess && pwdResult.Data.HasValue)
{
    Console.WriteLine($"Password expires: {pwdResult.Data}");
}
```

### List Received Messages

```csharp
// Get received messages from last 90 days, max 1000 messages
var result = await client.Api.GetListOfReceivedMessagesAsync(days: 90, limit: 1000);

if (result.IsSuccess)
{
    Console.WriteLine($"Found {result.Count} messages");
    
    foreach (var message in result.Data)
    {
        Console.WriteLine($"Message ID: {message.dmID}");
        Console.WriteLine($"Subject: {message.dmAnnotation}");
        Console.WriteLine($"Sender: {message.dmSender}");
        Console.WriteLine($"Delivery Time: {message.dmDeliveryTime}");
    }
}
```

### Download Messages

```csharp
// Download signed received message (ZFO format)
var messageId = "123456789";
var result = await client.Api.DownloadSignedReceivedMessageAsync(messageId);

if (result.IsSuccess)
{
    File.WriteAllBytes("message.zfo", result.Data);
    Console.WriteLine("Message downloaded successfully");
}

// Download delivery info
var deliveryResult = await client.Api.DownloadDeliveryInfoAsync(messageId);
if (deliveryResult.IsSuccess)
{
    File.WriteAllBytes("delivery.zfo", deliveryResult.Data);
}
```

### Download Attachments

```csharp
var result = await client.Api.GetReceivedDataMessageAttachmentsAsync(messageId);

if (result.IsSuccess && result.HasItems)
{
    foreach (var attachment in result.Data)
    {
        Console.WriteLine($"Attachment: {attachment.FileName}");
        Console.WriteLine($"MIME Type: {attachment.MimeType}");
        Console.WriteLine($"Size: {attachment.Content.Length} bytes");
        
        // Save attachment
        attachment.SaveToFile($"downloads/{attachment.FileName}");
    }
}
```

### Send Messages

#### Using the Fluent Message Builder (Recommended)

```csharp
using DatovkaSharp;

// Build a message using the fluent API
var message = new DatovkaMessageBuilder()
    .To("recipient-databox-id")
    .WithSubject("Important Document")
    .WithSenderRefNumber("REF-2024-001")
    .AsPersonalDelivery(true)
    .AddAttachment("path/to/document.pdf")
    .AddAttachment("path/to/invoice.pdf")
    .AddTextContent("readme.txt", "Please review the attached documents.")
    .Build(); // Validates automatically

// Send the message with automatic validation
var result = await client.Api.SendDataMessageAsync(message);

if (result.IsSuccess)
{
    Console.WriteLine($"✓ Message sent successfully!");
    Console.WriteLine($"  Message ID: {result.Data.dmID}");
    Console.WriteLine($"  Status: {result.StatusCode} - {result.StatusMessage}");
}
else
{
    Console.WriteLine($"✗ Error {result.StatusCode}: {result.StatusMessage}");
}
```

#### Using the Simple API

```csharp
// Create a message with attachments
var attachmentPaths = new List<string>
{
    "path/to/document.pdf",
    "path/to/image.jpg"
};

var message = client.Api.CreateBasicDataMessage(
    recipientDataBoxId: "recipient-id",
    subject: "Test message",
    attachmentPaths: attachmentPaths
);

// Send the message (automatically validated)
var result = await client.Api.SendDataMessageAsync(message);
```

### Search for Data Boxes

```csharp
// Search by ID
var result = await client.Api.FindDataBoxByIdAsync("databox-id");

if (result.IsSuccess && result.Data != null)
{
    Console.WriteLine("Data box found!");
}

// Search with custom criteria (name, address, etc.)
var searchCriteria = new Services.Search.tDbOwnerInfo
{
    // Set search criteria here
    dbID = "partial-id"
};
var searchResult = await client.Api.FindPersonalDataBoxAsync(searchCriteria);
```

### Password Management

```csharp
// Get enhanced password information
var result = await client.Api.GetEnhancedPasswordInfoAsync();
if (result.IsSuccess)
{
    Console.WriteLine($"Password expires: {result.Data.pswExpDate}");
}

// Change password
var changeResult = await client.Api.ChangePasswordAsync("currentPassword", "newPassword");
if (changeResult.IsSuccess && changeResult.Data)
{
    Console.WriteLine("Password changed successfully!");
}
```

### Mark Message as Read

```csharp
var result = await client.Api.MarkMessageAsDownloadedAsync(messageId);
if (result.IsSuccess && result.Data)
{
    Console.WriteLine("Message marked as downloaded");
}
```

## Message Validation

The library includes automatic validation when sending messages:

```csharp
using DatovkaSharp.Exceptions;

try
{
    var message = new DatovkaMessageBuilder()
        .To("recipient-id")
        .WithSubject("Test")
        .AddAttachment("large-file.pdf") // Validates size
        .Build(); // Validates on Build()
        
    var result = await client.Api.SendDataMessageAsync(message); // Also validates before sending
    
    if (!result.IsSuccess)
    {
        Console.WriteLine($"Send failed: {result.StatusMessage}");
    }
}
catch (MissingRequiredFieldException ex)
{
    Console.WriteLine($"Missing required field: {ex.FieldName}");
}
catch (FileSizeOverflowException ex)
{
    Console.WriteLine($"File too large: {ex.CurrentSize} bytes (max: {ex.MaxSize})");
}
catch (RecipientCountOverflowException ex)
{
    Console.WriteLine($"Too many recipients: {ex.Count} (max: {ex.MaxCount})");
}
catch (MissingMainFileException ex)
{
    Console.WriteLine($"At least one attachment is required");
}
```

### Validation Rules

- **Maximum 50 recipients** per message
- **Maximum 25 MB total** attachment size
- **At least 1 attachment** required (Czech Data Box requirement)
- **Subject/annotation is required**
- **At least one recipient** is required

## Advanced Usage

### Checking Message Builder Status

```csharp
var builder = new DatovkaMessageBuilder()
    .To("recipient-id")
    .WithSubject("Test");

// Check current size before adding more attachments
long currentSize = builder.GetCurrentTotalSize();
Console.WriteLine($"Current size: {currentSize} bytes");

// Check attachment count
int count = builder.GetAttachmentCount();
Console.WriteLine($"Attachments: {count}");

// Add attachment conditionally
if (currentSize + fileSize < MessageValidator.MaxTotalSizeBytes)
{
    builder.AddAttachment(filePath);
}
```

### Error Handling

```csharp
var result = await client.Api.SendDataMessageAsync(message);

// Multiple ways to handle errors
if (!result.IsSuccess)
{
    // Option 1: Check status code
    switch (result.StatusCode)
    {
        case "0000":
            Console.WriteLine("Success");
            break;
        case "1216":
            Console.WriteLine("Cannot send to own mailbox");
            break;
        case "1205":
            Console.WriteLine("Insufficient credits");
            break;
        default:
            Console.WriteLine($"Error: {result.StatusMessage}");
            break;
    }
}

// Option 2: Throw exception on failure
try
{
    result.ThrowIfFailed();
    Console.WriteLine($"Message sent: {result.Data.dmID}");
}
catch (DataBoxException ex)
{
    Console.WriteLine($"Failed: {ex.Message}");
}

// Option 3: Get data with fallback
var messageId = result.OrDefault(null)?.dmID;
```

## API Reference

### DatovkaClient

Main client class for connecting to the Data Box service.

- `LoginWithUsernameAndPassword(string username, string password)` - Authenticate with credentials
- `LoginWithCertificate(string certificatePath, string password)` - Authenticate with certificate
- `TestConnection()` - Test the connection to the service
- `Dispose()` - Clean up resources

### DatovkaApi

Simplified API for common operations. All methods return `DatovkaResult<T>` or `DatovkaListResult<T>`.

#### Information Methods
- `GetDataBoxInfoAsync()` - Get data box information
- `GetUserInfoAsync()` - Get user information
- `GetPasswordExpiresAsync()` - Get password expiration date
- `GetEnhancedPasswordInfoAsync()` - Get enhanced password information
- `GetStatsAsync()` - Get ISDS statistics

#### Message Operations
- `GetListOfReceivedMessagesAsync(int days, int limit)` - List received messages
- `GetListOfSentMessagesAsync(int days, int limit)` - List sent messages
- `SendDataMessageAsync(tMessageCreateInput message)` - Send a message
- `MarkMessageAsDownloadedAsync(string messageId)` - Mark message as read

#### Download Operations
- `DownloadSignedReceivedMessageAsync(string messageId)` - Download signed received message
- `DownloadSignedSentMessageAsync(string messageId)` - Download signed sent message
- `DownloadDeliveryInfoAsync(string messageId)` - Download delivery receipt
- `GetReceivedDataMessageAttachmentsAsync(string messageId)` - Get message attachments

#### Search Operations
- `FindDataBoxByIdAsync(string dataBoxId)` - Search data box by ID
- `FindPersonalDataBoxAsync(tDbOwnerInfo searchCriteria)` - Search with custom criteria

#### Password Management
- `ChangePasswordAsync(string currentPassword, string newPassword)` - Change password

### DatovkaMessageBuilder

Fluent API for building messages.

- `To(string recipientDataBoxId)` - Set recipient
- `WithSubject(string subject)` - Set subject
- `WithSenderRefNumber(string refNumber)` - Set sender reference number
- `WithRecipientRefNumber(string refNumber)` - Set recipient reference number
- `AsPersonalDelivery(bool isPersonalDelivery)` - Set personal delivery flag
- `AddAttachment(string filePath)` - Add file attachment
- `AddTextContent(string fileName, string content)` - Add text content as attachment
- `GetCurrentTotalSize()` - Get current total attachment size
- `GetAttachmentCount()` - Get attachment count
- `Build()` - Build and validate the message

### Result Types

#### DatovkaResult<T>
- `Data` - The typed result data
- `StatusCode` - ISDS status code
- `StatusMessage` - Status message
- `IsSuccess` - True if StatusCode == "0000"
- `RawResponse` - Raw SOAP response
- `ThrowIfFailed()` - Throw exception if not successful
- `OrDefault(T defaultValue)` - Get data or default value

#### DatovkaListResult<T>
Extends `DatovkaResult<List<T>>` with additional properties:
- `Count` - Number of items
- `HasItems` - True if count > 0

## Exception Types

- `DataBoxException` - Base exception class
- `FileSizeOverflowException` - Attachment size exceeds limit
- `RecipientCountOverflowException` - Too many recipients
- `MissingRequiredFieldException` - Required field is missing
- `MissingMainFileException` - No attachments provided
- `ConnectionException` - Connection error

## Project Structure

```
DatovkaSharp/
├── DatovkaSharp/           # Main library
│   ├── DataBoxEnvironment.cs
│   ├── DataBoxException.cs
│   ├── DatovkaClient.cs    # Main client
│   ├── DatovkaApi.cs       # Simplified API
│   ├── DatovkaResult.cs    # Result wrapper
│   ├── DatovkaListResult.cs # List result wrapper
│   ├── DatovkaMessageBuilder.cs # Fluent message builder
│   ├── DataBoxAttachment.cs
│   ├── DataBoxHelper.cs
│   ├── MessageValidator.cs # Message validation
│   ├── Exceptions/         # Custom exceptions
│   ├── Services/           # Generated SOAP clients
│   └── Resources/          # WSDL files
└── DatovkaSharp.Tests/     # NUnit tests
    ├── DatovkaClientTests.cs
    ├── MessageOperationsTests.cs
    ├── MessageBuilderTests.cs
    ├── AttachmentTests.cs
    └── SearchTests.cs
```

## Requirements

- .NET Standard 2.0 or higher
- System.ServiceModel.Http (>= v4.10.3)
- System.ServiceModel.Primitives (>= v4.10.3)
- System.ServiceModel.Security (>= v4.10.3)

## Testing

```bash
dotnet test
```

Test credentials (for test environment) are required. Create two test accounts as described here: https://info.mojedatovaschranka.cz/info/cs/74.html.

## License

This library is licensed under the [MIT](https://github.com/lofcz/DatovkaSharp/blob/master/LICENSE) license. 💜
