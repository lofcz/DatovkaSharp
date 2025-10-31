using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatovkaSharp.Services.Access;
using DatovkaSharp.Services.Info;
using DatovkaSharp.Services.Operations;
using DatovkaSharp.Services.Search;
using DatovkaSharp.Services.Stat;
using tDbOwnerInfo = DatovkaSharp.Services.Search.tDbOwnerInfo;
using tIDMessInput = DatovkaSharp.Services.Operations.tIDMessInput;

namespace DatovkaSharp
{
    /// <summary>
    /// Simplified API for common Czech Data Box operations.
    /// All methods return wrapped results with status information.
    /// </summary>
    public class DatovkaApi
    {
        private readonly DatovkaClient _client;

        public DatovkaApi(DatovkaClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Get information about the current data box
        /// </summary>
        public async Task<DatovkaResult<Services.Access.tDbOwnerInfo>> GetDataBoxInfoAsync()
        {
            try
            {
                DataBoxAccessPortTypeClient client = _client.GetAccessClient();
                tDummyInput input = new tDummyInput();
                GetOwnerInfoFromLoginResponse? response = await client.GetOwnerInfoFromLoginAsync(input);
                
                var output = response.GetOwnerInfoFromLoginResponse1;
                var status = output?.dbStatus;
                
                return new DatovkaResult<Services.Access.tDbOwnerInfo>
                {
                    Data = output?.dbOwnerInfo,
                    StatusCode = status?.dbStatusCode ?? "0000",
                    StatusMessage = status?.dbStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<Services.Access.tDbOwnerInfo>.FromException(ex);
            }
        }

        /// <summary>
        /// Get information about the current user
        /// </summary>
        public async Task<DatovkaResult<tDbUserInfo>> GetUserInfoAsync()
        {
            try
            {
                DataBoxAccessPortTypeClient client = _client.GetAccessClient();
                tDummyInput input = new tDummyInput();
                GetUserInfoFromLoginResponse? response = await client.GetUserInfoFromLoginAsync(input);
                
                var output = response.GetUserInfoFromLoginResponse1;
                var status = output?.dbStatus;
                
                return new DatovkaResult<tDbUserInfo>
                {
                    Data = output?.dbUserInfo,
                    StatusCode = status?.dbStatusCode ?? "0000",
                    StatusMessage = status?.dbStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<tDbUserInfo>.FromException(ex);
            }
        }

        /// <summary>
        /// Get password expiration date
        /// </summary>
        public async Task<DatovkaResult<DateTime?>> GetPasswordExpiresAsync()
        {
            try
            {
                DataBoxAccessPortTypeClient client = _client.GetAccessClient();
                tDummyInput input = new tDummyInput();
                GetPasswordInfoResponse? response = await client.GetPasswordInfoAsync(input);
                
                var output = response.GetPasswordInfoResponse1;
                var status = output?.dbStatus;
                
                return new DatovkaResult<DateTime?>
                {
                    Data = output?.pswExpDate,
                    StatusCode = status?.dbStatusCode ?? "0000",
                    StatusMessage = status?.dbStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<DateTime?>.FromException(ex);
            }
        }

        /// <summary>
        /// Get list of received messages
        /// </summary>
        public async Task<DatovkaListResult<tRecord>> GetListOfReceivedMessagesAsync(int days = 90, int limit = 1000)
        {
            try
            {
                if (days < 0 || limit < 1)
                    throw new DataBoxException("Invalid parameters: days must be >= 0 and limit must be >= 1");

                dmInfoPortTypeClient client = _client.GetInfoClient();
                tListOfFReceivedInput input = new tListOfFReceivedInput
                {
                    dmFromTime = DateTime.Now.AddDays(-days),
                    dmToTime = DateTime.Now,
                    dmLimit = limit.ToString(),
                    dmOffset = "0",
                    dmStatusFilter = "-1"
                };

                GetListOfReceivedMessagesResponse? response = await client.GetListOfReceivedMessagesAsync(input);
                
                var output = response.GetListOfReceivedMessagesResponse1;
                var status = output?.dmStatus;
                tRecord[]? records = output?.dmRecords?.dmRecord;
                
                return new DatovkaListResult<tRecord>
                {
                    Data = records?.ToList() ?? new List<tRecord>(),
                    StatusCode = status?.dmStatusCode ?? "0000",
                    StatusMessage = status?.dmStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return new DatovkaListResult<tRecord>
                {
                    Data = new List<tRecord>(),
                    StatusCode = "9999",
                    StatusMessage = ex.Message,
                    RawResponse = ex
                };
            }
        }

        /// <summary>
        /// Get list of sent messages
        /// </summary>
        public async Task<DatovkaListResult<tRecord>> GetListOfSentMessagesAsync(int days = 90, int limit = 1000)
        {
            try
            {
                if (days < 0 || limit < 1)
                    throw new DataBoxException("Invalid parameters: days must be >= 0 and limit must be >= 1");

                dmInfoPortTypeClient client = _client.GetInfoClient();
                tListOfSentInput input = new tListOfSentInput
                {
                    dmFromTime = DateTime.Now.AddDays(-days),
                    dmToTime = DateTime.Now,
                    dmLimit = limit.ToString(),
                    dmOffset = "0",
                    dmStatusFilter = "-1"
                };

                GetListOfSentMessagesResponse? response = await client.GetListOfSentMessagesAsync(input);
                
                var output = response.GetListOfSentMessagesResponse1;
                var status = output?.dmStatus;
                tRecord[]? records = output?.dmRecords?.dmRecord;
                
                return new DatovkaListResult<tRecord>
                {
                    Data = records?.ToList() ?? new List<tRecord>(),
                    StatusCode = status?.dmStatusCode ?? "0000",
                    StatusMessage = status?.dmStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return new DatovkaListResult<tRecord>
                {
                    Data = new List<tRecord>(),
                    StatusCode = "9999",
                    StatusMessage = ex.Message,
                    RawResponse = ex
                };
            }
        }

        /// <summary>
        /// Download signed received message (ZFO format)
        /// </summary>
        public async Task<DatovkaResult<byte[]>> DownloadSignedReceivedMessageAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                    throw new ArgumentNullException(nameof(messageId));

                dmOperationsPortTypeClient client = _client.GetOperationsClient();
                tIDMessInput input = new tIDMessInput { dmID = messageId };
                SignedMessageDownloadResponse? response = await client.SignedMessageDownloadAsync(input);
                
                var output = response.SignedMessageDownloadResponse1;
                var status = output?.dmStatus;
                
                return new DatovkaResult<byte[]>
                {
                    Data = output?.dmSignature ?? Array.Empty<byte>(),
                    StatusCode = status?.dmStatusCode ?? "0000",
                    StatusMessage = status?.dmStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<byte[]>.FromException(ex);
            }
        }

        /// <summary>
        /// Download signed sent message (ZFO format)
        /// </summary>
        public async Task<DatovkaResult<byte[]>> DownloadSignedSentMessageAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                    throw new ArgumentNullException(nameof(messageId));

                dmOperationsPortTypeClient client = _client.GetOperationsClient();
                tIDMessInput input = new tIDMessInput { dmID = messageId };
                SignedSentMessageDownloadResponse? response = await client.SignedSentMessageDownloadAsync(input);
                
                var output = response.SignedSentMessageDownloadResponse1;
                var status = output?.dmStatus;
                
                return new DatovkaResult<byte[]>
                {
                    Data = output?.dmSignature ?? Array.Empty<byte>(),
                    StatusCode = status?.dmStatusCode ?? "0000",
                    StatusMessage = status?.dmStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<byte[]>.FromException(ex);
            }
        }

        /// <summary>
        /// Download delivery info (receipt)
        /// </summary>
        public async Task<DatovkaResult<byte[]>> DownloadDeliveryInfoAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                    throw new ArgumentNullException(nameof(messageId));

                dmInfoPortTypeClient client = _client.GetInfoClient();
                Services.Info.tIDMessInput input = new Services.Info.tIDMessInput { dmID = messageId };
                GetSignedDeliveryInfoResponse? response = await client.GetSignedDeliveryInfoAsync(input);
                
                var output = response.GetSignedDeliveryInfoResponse1;
                var status = output?.dmStatus;
                
                return new DatovkaResult<byte[]>
                {
                    Data = output?.dmSignature ?? Array.Empty<byte>(),
                    StatusCode = status?.dmStatusCode ?? "0000",
                    StatusMessage = status?.dmStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<byte[]>.FromException(ex);
            }
        }

        /// <summary>
        /// Get attachments from a received message
        /// </summary>
        public async Task<DatovkaListResult<DataBoxAttachment>> GetReceivedDataMessageAttachmentsAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                    throw new ArgumentNullException(nameof(messageId));

                dmOperationsPortTypeClient client = _client.GetOperationsClient();
                tIDMessInput input = new tIDMessInput { dmID = messageId };
                MessageDownloadResponse? response = await client.MessageDownloadAsync(input);
                
                var output = response.MessageDownloadResponse1;
                var status = output?.dmStatus;
                List<DataBoxAttachment> attachments = new List<DataBoxAttachment>();
                tFilesArrayDmFile[]? dmFiles = output?.dmReturnedMessage?.dmDm?.dmFiles;
                
                if (dmFiles != null && dmFiles.Length > 0)
                {
                    foreach (var file in dmFiles)
                    {
                        if (file == null) continue;
                        
                        DataBoxAttachment attachment = new DataBoxAttachment
                        {
                            FileName = file.dmFileDescr ?? "unnamed",
                            Content = file.Item as byte[] ?? Array.Empty<byte>(),
                            MimeType = file.dmMimeType ?? "application/octet-stream"
                        };
                        attachments.Add(attachment);
                    }
                }

                return new DatovkaListResult<DataBoxAttachment>
                {
                    Data = attachments,
                    StatusCode = status?.dmStatusCode ?? "0000",
                    StatusMessage = status?.dmStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return new DatovkaListResult<DataBoxAttachment>
                {
                    Data = new List<DataBoxAttachment>(),
                    StatusCode = "9999",
                    StatusMessage = ex.Message,
                    RawResponse = ex
                };
            }
        }

        /// <summary>
        /// Find data box by ID
        /// </summary>
        public async Task<DatovkaResult<tDbOwnersArray>> FindDataBoxByIdAsync(string dataBoxId)
        {
            try
            {
                if (string.IsNullOrEmpty(dataBoxId))
                    throw new ArgumentNullException(nameof(dataBoxId));

                DataBoxSearchPortTypeClient client = _client.GetSearchClient();
                tDbOwnerInfo ownerInfo = new tDbOwnerInfo { dbID = dataBoxId };
                tFindDBInput input = new tFindDBInput { dbOwnerInfo = ownerInfo };
                FindDataBoxResponse? response = await client.FindDataBoxAsync(input);
                
                var output = response.FindDataBoxResponse1;
                var status = output?.dbStatus;
                
                return new DatovkaResult<tDbOwnersArray>
                {
                    Data = output?.dbResults,
                    StatusCode = status?.dbStatusCode ?? "0000",
                    StatusMessage = status?.dbStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<tDbOwnersArray>.FromException(ex);
            }
        }

        /// <summary>
        /// Create a basic data message
        /// </summary>
        public tMessageCreateInput CreateBasicDataMessage(
            string recipientDataBoxId, 
            string subject, 
            List<string>? attachmentPaths = null)
        {
            if (string.IsNullOrEmpty(recipientDataBoxId))
                throw new ArgumentNullException(nameof(recipientDataBoxId));
            if (string.IsNullOrEmpty(subject))
                throw new ArgumentNullException(nameof(subject));

            // Create envelope
            tMessageCreateInputDmEnvelope envelope = new tMessageCreateInputDmEnvelope
            {
                dbIDRecipient = recipientDataBoxId,
                dmAnnotation = subject
            };

            // Create message input
            tMessageCreateInput messageInput = new tMessageCreateInput
            {
                dmEnvelope = envelope
            };

            // Create files array (always required, even if empty)
            List<tFilesArrayDmFile> files = new List<tFilesArrayDmFile>();
            
            // Add attachments if provided
            if (attachmentPaths != null && attachmentPaths.Count > 0)
            {
                foreach (string? path in attachmentPaths)
                {
                    if (!System.IO.File.Exists(path))
                        continue;

                    DataBoxAttachment attachment = DataBoxAttachment.FromFile(path);
                    tFilesArrayDmFile file = new tFilesArrayDmFile
                    {
                        dmFileDescr = attachment.FileName,
                        dmMimeType = attachment.MimeType,
                        Item = attachment.Content
                    };
                    files.Add(file);
                }
            }

            // Always set dmFiles, even if empty (required by the API)
            messageInput.dmFiles = files.ToArray();

            return messageInput;
        }

        /// <summary>
        /// Send a data message
        /// </summary>
        public async Task<DatovkaResult<tMessageCreateOutput>> SendDataMessageAsync(tMessageCreateInput message)
        {
            try
            {
                if (message == null)
                    throw new ArgumentNullException(nameof(message));

                // Validate message before sending
                MessageValidator.ValidateMessageForSending(message);

                dmOperationsPortTypeClient client = _client.GetOperationsClient();
                CreateMessageResponse? response = await client.CreateMessageAsync(message);
                
                var output = response.CreateMessageResponse1;
                
                return new DatovkaResult<tMessageCreateOutput>
                {
                    Data = output,
                    StatusCode = output?.dmStatus?.dmStatusCode ?? "0000",
                    StatusMessage = output?.dmStatus?.dmStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<tMessageCreateOutput>.FromException(ex);
            }
        }

        /// <summary>
        /// Get ISDS statistics (total number of messages in the system)
        /// </summary>
        public async Task<DatovkaResult<string>> GetStatsAsync()
        {
            try
            {
                IsdsStatPortTypeClient client = _client.GetStatClient();
                tNumOfMessagesInput input = new tNumOfMessagesInput();
                NumOfMessagesResponse? response = await client.NumOfMessagesAsync(input);
                
                var output = response.NumOfMessagesResponse1;
                var status = output?.dbStatus;
                
                return new DatovkaResult<string>
                {
                    Data = output?.statResult ?? string.Empty,
                    StatusCode = status?.dbStatusCode ?? "0000",
                    StatusMessage = status?.dbStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<string>.FromException(ex);
            }
        }

        /// <summary>
        /// Mark a message as downloaded/read
        /// </summary>
        public async Task<DatovkaResult<bool>> MarkMessageAsDownloadedAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                    throw new ArgumentNullException(nameof(messageId));

                dmInfoPortTypeClient client = _client.GetInfoClient();
                Services.Info.tIDMessInput input = new Services.Info.tIDMessInput { dmID = messageId };
                MarkMessageAsDownloadedResponse? response = await client.MarkMessageAsDownloadedAsync(input);
                
                var output = response.MarkMessageAsDownloadedResponse1;
                var status = output?.dmStatus;
                bool success = status?.dmStatusCode == "0000";
                
                return new DatovkaResult<bool>
                {
                    Data = success,
                    StatusCode = status?.dmStatusCode ?? "9999",
                    StatusMessage = status?.dmStatusMessage ?? "Unknown error",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<bool>.FromException(ex);
            }
        }

        /// <summary>
        /// Change ISDS password
        /// </summary>
        public async Task<DatovkaResult<bool>> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(currentPassword))
                    throw new ArgumentNullException(nameof(currentPassword));
                if (string.IsNullOrEmpty(newPassword))
                    throw new ArgumentNullException(nameof(newPassword));

                DataBoxAccessPortTypeClient client = _client.GetAccessClient();
                tChngPasswInput input = new tChngPasswInput
                {
                    dbOldPassword = currentPassword,
                    dbNewPassword = newPassword
                };
                ChangeISDSPasswordResponse? response = await client.ChangeISDSPasswordAsync(input);
                
                var output = response.ChangeISDSPasswordResponse1;
                var status = output?.dbStatus;
                bool success = status?.dbStatusCode == "0000";
                
                return new DatovkaResult<bool>
                {
                    Data = success,
                    StatusCode = status?.dbStatusCode ?? "9999",
                    StatusMessage = status?.dbStatusMessage ?? "Unknown error",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<bool>.FromException(ex);
            }
        }

        /// <summary>
        /// Get enhanced password information including expiration
        /// </summary>
        public async Task<DatovkaResult<tGetPasswInfoOutput>> GetEnhancedPasswordInfoAsync()
        {
            try
            {
                DataBoxAccessPortTypeClient client = _client.GetAccessClient();
                tDummyInput input = new tDummyInput();
                GetPasswordInfoResponse? response = await client.GetPasswordInfoAsync(input);
                
                var output = response.GetPasswordInfoResponse1;
                var status = output?.dbStatus;
                
                return new DatovkaResult<tGetPasswInfoOutput>
                {
                    Data = output,
                    StatusCode = status?.dbStatusCode ?? "0000",
                    StatusMessage = status?.dbStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<tGetPasswInfoOutput>.FromException(ex);
            }
        }

        /// <summary>
        /// Find personal data boxes by criteria (name, address, etc.)
        /// Uses the already existing FindDataBoxByIdAsync but with custom search criteria
        /// </summary>
        public async Task<DatovkaResult<tDbOwnersArray>> FindPersonalDataBoxAsync(tDbOwnerInfo searchCriteria)
        {
            try
            {
                if (searchCriteria == null)
                    throw new ArgumentNullException(nameof(searchCriteria));

                DataBoxSearchPortTypeClient client = _client.GetSearchClient();
                tFindDBInput input = new tFindDBInput { dbOwnerInfo = searchCriteria };
                FindDataBoxResponse? response = await client.FindDataBoxAsync(input);
                
                var output = response.FindDataBoxResponse1;
                var status = output?.dbStatus;
                
                return new DatovkaResult<tDbOwnersArray>
                {
                    Data = output?.dbResults,
                    StatusCode = status?.dbStatusCode ?? "0000",
                    StatusMessage = status?.dbStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return DatovkaResult<tDbOwnersArray>.FromException(ex);
            }
        }

        // Synchronous wrappers for compatibility
        public DatovkaResult<Services.Access.tDbOwnerInfo> GetDataBoxInfo() => GetDataBoxInfoAsync().GetAwaiter().GetResult();
        public DatovkaResult<tDbUserInfo> GetUserInfo() => GetUserInfoAsync().GetAwaiter().GetResult();
        public DatovkaResult<DateTime?> GetPasswordExpires() => GetPasswordExpiresAsync().GetAwaiter().GetResult();
        public DatovkaListResult<tRecord> GetListOfReceivedMessages(int days = 90, int limit = 1000) 
            => GetListOfReceivedMessagesAsync(days, limit).GetAwaiter().GetResult();
        public DatovkaListResult<tRecord> GetListOfSentMessages(int days = 90, int limit = 1000) 
            => GetListOfSentMessagesAsync(days, limit).GetAwaiter().GetResult();
        public DatovkaResult<byte[]> DownloadSignedReceivedMessage(string messageId) 
            => DownloadSignedReceivedMessageAsync(messageId).GetAwaiter().GetResult();
        public DatovkaResult<byte[]> DownloadSignedSentMessage(string messageId) 
            => DownloadSignedSentMessageAsync(messageId).GetAwaiter().GetResult();
        public DatovkaResult<byte[]> DownloadDeliveryInfo(string messageId) 
            => DownloadDeliveryInfoAsync(messageId).GetAwaiter().GetResult();
        public DatovkaListResult<DataBoxAttachment> GetReceivedDataMessageAttachments(string messageId) 
            => GetReceivedDataMessageAttachmentsAsync(messageId).GetAwaiter().GetResult();
        public DatovkaResult<tDbOwnersArray> FindDataBoxById(string dataBoxId) 
            => FindDataBoxByIdAsync(dataBoxId).GetAwaiter().GetResult();
        public DatovkaResult<tMessageCreateOutput> SendDataMessage(tMessageCreateInput message) 
            => SendDataMessageAsync(message).GetAwaiter().GetResult();
        public DatovkaResult<string> GetStats() => GetStatsAsync().GetAwaiter().GetResult();
    }
}
