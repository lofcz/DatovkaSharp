using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DatovkaSharp.Services.Access;
using DatovkaSharp.Services.Info;
using DatovkaSharp.Services.Operations;
using DatovkaSharp.Services.Search;
using DatovkaSharp.Services.Stat;
using tDbOwnerInfo = DatovkaSharp.Services.Search.tDbOwnerInfo;
using tDbReqStatus = DatovkaSharp.Services.Access.tDbReqStatus;
using tIDMessInput = DatovkaSharp.Services.Operations.tIDMessInput;
using tStatus = DatovkaSharp.Services.Info.tStatus;

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
                
                tGetOwnInfoOutput? output = response.GetOwnerInfoFromLoginResponse1;
                tDbReqStatus? status = output?.dbStatus;
                
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
                
                tGetUserInfoOutput? output = response.GetUserInfoFromLoginResponse1;
                tDbReqStatus? status = output?.dbStatus;
                
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
                
                tGetPasswInfoOutput? output = response.GetPasswordInfoResponse1;
                tDbReqStatus? status = output?.dbStatus;
                
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
                
                tListOfMessOutput? output = response.GetListOfReceivedMessagesResponse1;
                tStatus? status = output?.dmStatus;
                tRecord[]? records = output?.dmRecords?.dmRecord;
                
                return new DatovkaListResult<tRecord>
                {
                    Data = records?.ToList() ?? [],
                    StatusCode = status?.dmStatusCode ?? "0000",
                    StatusMessage = status?.dmStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return new DatovkaListResult<tRecord>
                {
                    Data = [],
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
                
                tListOfMessOutput? output = response.GetListOfSentMessagesResponse1;
                tStatus? status = output?.dmStatus;
                tRecord[]? records = output?.dmRecords?.dmRecord;
                
                return new DatovkaListResult<tRecord>
                {
                    Data = records?.ToList() ?? [],
                    StatusCode = status?.dmStatusCode ?? "0000",
                    StatusMessage = status?.dmStatusMessage ?? "Success",
                    RawResponse = response
                };
            }
            catch (Exception ex)
            {
                return new DatovkaListResult<tRecord>
                {
                    Data = [],
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
                
                tSignedMessDownOutput? output = response.SignedMessageDownloadResponse1;
                Services.Operations.tStatus? status = output?.dmStatus;
                
                return new DatovkaResult<byte[]>
                {
                    Data = output?.dmSignature ?? [],
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
                
                tSignedMessDownOutput? output = response.SignedSentMessageDownloadResponse1;
                Services.Operations.tStatus? status = output?.dmStatus;
                
                return new DatovkaResult<byte[]>
                {
                    Data = output?.dmSignature ?? [],
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
                
                tSignDelivMessOutput? output = response.GetSignedDeliveryInfoResponse1;
                tStatus? status = output?.dmStatus;
                
                return new DatovkaResult<byte[]>
                {
                    Data = output?.dmSignature ?? [],
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
                
                tMessDownOutput? output = response.MessageDownloadResponse1;
                Services.Operations.tStatus? status = output?.dmStatus;
                List<DataBoxAttachment> attachments = [];
                tFilesArrayDmFile[]? dmFiles = output?.dmReturnedMessage?.dmDm?.dmFiles;
                
                if (dmFiles is { Length: > 0 })
                {
                    foreach (tFilesArrayDmFile file in dmFiles)
                    {
                        DataBoxAttachment attachment = new DataBoxAttachment
                        {
                            FileName = file.dmFileDescr ?? "unnamed",
                            Content = file.Item as byte[] ?? [],
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
                    Data = [],
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
                
                tFindDBOuput? output = response.FindDataBoxResponse1;
                Services.Search.tDbReqStatus? status = output?.dbStatus;
                
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
        /// Create a basic data message from file paths
        /// </summary>
        public tMessageCreateInput CreateBasicDataMessage(
            string recipientDataBoxId, 
            string subject, 
            List<string>? attachmentPaths = null)
        {
            // Convert file paths to DataBoxAttachment objects using FromStream
            List<DataBoxAttachment>? attachments = null;
            if (attachmentPaths is { Count: > 0 })
            {
                attachments = [];
                foreach (string? path in attachmentPaths)
                {
                    if (File.Exists(path))
                    {
                        using FileStream fileStream = File.OpenRead(path);
                        attachments.Add(DataBoxAttachment.FromStream(fileStream, Path.GetFileName(path)));
                    }
                }
            }

            // Delegate to the main overload
            return CreateBasicDataMessage(recipientDataBoxId, subject, attachments);
        }

        /// <summary>
        /// Create a basic data message with attachments from DataBoxAttachment objects (e.g., from streams)
        /// </summary>
        public tMessageCreateInput CreateBasicDataMessage(
            string recipientDataBoxId, 
            string subject, 
            List<DataBoxAttachment>? attachments = null)
        {
            if (string.IsNullOrEmpty(recipientDataBoxId))
                throw new ArgumentNullException(nameof(recipientDataBoxId));
            if (string.IsNullOrEmpty(subject))
                throw new ArgumentNullException(nameof(subject));

            tMessageCreateInput messageInput = CreateMessageWithEnvelope(recipientDataBoxId, subject);
            messageInput.dmFiles = ConvertAttachmentsToFiles(attachments);

            return messageInput;
        }

        private tMessageCreateInput CreateMessageWithEnvelope(string recipientDataBoxId, string subject)
        {
            return new tMessageCreateInput
            {
                dmEnvelope = new tMessageCreateInputDmEnvelope
                {
                    dbIDRecipient = recipientDataBoxId,
                    dmAnnotation = subject
                }
            };
        }

        private tFilesArrayDmFile[] ConvertAttachmentsToFiles(List<DataBoxAttachment>? attachments)
        {
            List<tFilesArrayDmFile> files = [];

            if (attachments is { Count: > 0 })
            {
                foreach (DataBoxAttachment attachment in attachments)
                {
                    files.Add(new tFilesArrayDmFile
                    {
                        dmFileDescr = attachment.FileName,
                        dmMimeType = attachment.MimeType,
                        Item = attachment.Content
                    });
                }
            }

            return files.ToArray();
        }

        /// <summary>
        /// Get data box credit information
        /// </summary>
        /// <param name="dataBoxId">Data box ID (optional, uses logged-in user's ID if not provided)</param>
        /// <param name="fromDate">Optional start date for credit history</param>
        /// <param name="toDate">Optional end date for credit history</param>
        public async Task<DatovkaResult<tDBCreditInfoOutput>> GetDataBoxCreditInfoAsync(
            string? dataBoxId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                // If no dataBoxId provided, get the current user's data box ID
                if (string.IsNullOrEmpty(dataBoxId))
                {
                    DatovkaResult<Services.Access.tDbOwnerInfo> ownerInfoResult = await GetDataBoxInfoAsync();
                    if (!ownerInfoResult.IsSuccess || ownerInfoResult.Data == null)
                    {
                        return DatovkaResult<tDBCreditInfoOutput>.Failure(
                            ownerInfoResult.StatusCode,
                            $"Failed to get data box ID: {ownerInfoResult.StatusMessage}",
                            ownerInfoResult.RawResponse
                        );
                    }
                    dataBoxId = ownerInfoResult.Data.dbID;
                }

                DataBoxSearchPortTypeClient client = _client.GetSearchClient();
                tDBCreditInfoInput input = new tDBCreditInfoInput
                {
                    dbID = dataBoxId,
                    ciFromDate = fromDate,
                    ciTodate = toDate  // Note: API has a typo "Todate" instead of "ToDate"
                };

                DataBoxCreditInfoResponse? response = await client.DataBoxCreditInfoAsync(input);

                if (response?.DataBoxCreditInfoResponse1?.dbStatus?.dbStatusCode == "0000")
                {
                    return DatovkaResult<tDBCreditInfoOutput>.Success(
                        response.DataBoxCreditInfoResponse1,
                        response.DataBoxCreditInfoResponse1.dbStatus.dbStatusCode,
                        response.DataBoxCreditInfoResponse1.dbStatus.dbStatusMessage,
                        response
                    );
                }

                return DatovkaResult<tDBCreditInfoOutput>.Failure(
                    response?.DataBoxCreditInfoResponse1?.dbStatus?.dbStatusCode ?? "9999",
                    response?.DataBoxCreditInfoResponse1?.dbStatus?.dbStatusMessage ?? "Unknown error",
                    response
                );
            }
            catch (Exception ex)
            {
                return DatovkaResult<tDBCreditInfoOutput>.FromException(ex);
            }
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
                
                tMessageCreateOutput? output = response.CreateMessageResponse1;
                
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
                
                tNumOfMessagesOutput? output = response.NumOfMessagesResponse1;
                tStatReqStatus? status = output?.dbStatus;
                
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
        /// Download message attachments
        /// </summary>
        public async Task<DatovkaListResult<DataBoxAttachment>> GetMessageAttachmentsAsync(string messageId)
        {
            try
            {
                if (string.IsNullOrEmpty(messageId))
                    throw new ArgumentNullException(nameof(messageId));

                dmOperationsPortTypeClient client = _client.GetOperationsClient();
                tIDMessInput input = new tIDMessInput { dmID = messageId };
                MessageDownloadResponse? response = await client.MessageDownloadAsync(input);

                tMessDownOutput? output = response?.MessageDownloadResponse1;
                Services.Operations.tStatus? status = output?.dmStatus;

                if (status?.dmStatusCode == "0000")
                {
                    tFilesArrayDmFile[]? files = output?.dmReturnedMessage?.dmDm?.dmFiles;
                    List<DataBoxAttachment> attachments = [];

                    if (files != null)
                    {
                        foreach (var file in files)
                        {
                            if (file?.Item is byte[] content)
                            {
                                attachments.Add(new DataBoxAttachment
                                {
                                    FileName = file.dmFileDescr ?? "unnamed",
                                    MimeType = file.dmMimeType ?? "application/octet-stream",
                                    Content = content
                                });
                            }
                        }
                    }

                    return DatovkaListResult<DataBoxAttachment>.Success(
                        attachments,
                        status.dmStatusCode,
                        status.dmStatusMessage,
                        response
                    );
                }

                return DatovkaListResult<DataBoxAttachment>.Failure(
                    status?.dmStatusCode ?? "9999",
                    status?.dmStatusMessage ?? "Unknown error",
                    response
                );
            }
            catch (Exception ex)
            {
                return DatovkaListResult<DataBoxAttachment>.FromException(ex);
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
                
                tMarkMessOutput? output = response.MarkMessageAsDownloadedResponse1;
                tStatus? status = output?.dmStatus;
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
                
                tReqStatusOutput? output = response.ChangeISDSPasswordResponse1;
                tDbReqStatus? status = output?.dbStatus;
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
                
                tGetPasswInfoOutput? output = response.GetPasswordInfoResponse1;
                tDbReqStatus? status = output?.dbStatus;
                
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
                
                tFindDBOuput? output = response.FindDataBoxResponse1;
                Services.Search.tDbReqStatus? status = output?.dbStatus;
                
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
    }
}
