using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DatovkaSharp.Exceptions;
using DatovkaSharp.Services.Operations;

namespace DatovkaSharp
{
    /// <summary>
    /// Fluent builder for creating data box messages with validation.
    /// </summary>
    public class DatovkaMessageBuilder
    {
        private readonly tMessageCreateInput _message;
        private readonly List<tFilesArrayDmFile> _files;
        private long _currentTotalSize;

        /// <summary>
        /// Initializes a new instance of the DatovkaMessageBuilder class.
        /// </summary>
        public DatovkaMessageBuilder()
        {
            _message = new tMessageCreateInput
            {
                dmEnvelope = new tMessageCreateInputDmEnvelope()
            };
            _files = new List<tFilesArrayDmFile>();
            _currentTotalSize = 0;
        }

        /// <summary>
        /// Sets the recipient's data box ID.
        /// </summary>
        public DatovkaMessageBuilder To(string recipientDataBoxId)
        {
            if (string.IsNullOrWhiteSpace(recipientDataBoxId))
                throw new ArgumentNullException(nameof(recipientDataBoxId));

            _message.dmEnvelope.dbIDRecipient = recipientDataBoxId;
            return this;
        }

        /// <summary>
        /// Sets the message subject/annotation.
        /// </summary>
        public DatovkaMessageBuilder WithSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentNullException(nameof(subject));

            _message.dmEnvelope.dmAnnotation = subject;
            return this;
        }

        /// <summary>
        /// Sets the sender identification (optional).
        /// </summary>
        public DatovkaMessageBuilder WithSenderIdent(string senderIdent)
        {
            _message.dmEnvelope.dmSenderIdent = senderIdent;
            return this;
        }

        /// <summary>
        /// Sets the sender reference number (optional).
        /// </summary>
        public DatovkaMessageBuilder WithSenderRefNumber(string refNumber)
        {
            _message.dmEnvelope.dmSenderRefNumber = refNumber;
            return this;
        }

        /// <summary>
        /// Sets the recipient reference number (optional).
        /// </summary>
        public DatovkaMessageBuilder WithRecipientRefNumber(string refNumber)
        {
            _message.dmEnvelope.dmRecipientRefNumber = refNumber;
            return this;
        }

        /// <summary>
        /// Sets the file reference number (optional).
        /// </summary>
        public DatovkaMessageBuilder WithFileRefNumber(string refNumber)
        {
            _message.dmEnvelope.dmToHands = refNumber;
            return this;
        }

        /// <summary>
        /// Sets the "to hands" field (optional).
        /// </summary>
        public DatovkaMessageBuilder WithToHands(string toHands)
        {
            _message.dmEnvelope.dmToHands = toHands;
            return this;
        }

        /// <summary>
        /// Marks the message for personal delivery.
        /// </summary>
        public DatovkaMessageBuilder AsPersonalDelivery(bool personalDelivery = true)
        {
            _message.dmEnvelope.dmPersonalDelivery = personalDelivery;
            return this;
        }

        /// <summary>
        /// Requests delivery notification.
        /// </summary>
        public DatovkaMessageBuilder WithDeliveryNotification(bool allowSubstDelivery = false)
        {
            _message.dmEnvelope.dmAllowSubstDelivery = allowSubstDelivery;
            return this;
        }

        /// <summary>
        /// Sets the message type (optional).
        /// </summary>
        public DatovkaMessageBuilder WithType(string messageType)
        {
            _message.dmEnvelope.dmType = messageType;
            return this;
        }

        /// <summary>
        /// Adds an attachment from a file path.
        /// </summary>
        /// <param name="filePath">Path to the file to attach.</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <exception cref="FileNotFoundException">Thrown when file doesn't exist.</exception>
        /// <exception cref="FileSizeOverflowException">Thrown when adding the file would exceed the size limit.</exception>
        public DatovkaMessageBuilder AddAttachment(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Attachment file not found.", filePath);

            DataBoxAttachment attachment = DataBoxAttachment.FromFile(filePath);
            
            // Validate size before adding
            MessageValidator.ValidateAddingFile(_currentTotalSize, attachment.Content.Length);
            
            tFilesArrayDmFile file = new tFilesArrayDmFile
            {
                dmFileDescr = attachment.FileName,
                dmMimeType = attachment.MimeType,
                Item = attachment.Content
            };
            
            _files.Add(file);
            _currentTotalSize += attachment.Content.Length;
            
            return this;
        }

        /// <summary>
        /// Adds an attachment from raw data.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="content">File content as byte array.</param>
        /// <param name="mimeType">MIME type (optional, will be determined from fileName if not provided).</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <exception cref="FileSizeOverflowException">Thrown when adding the file would exceed the size limit.</exception>
        public DatovkaMessageBuilder AddAttachment(string fileName, byte[] content, string? mimeType = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (content == null || content.Length == 0)
                throw new ArgumentNullException(nameof(content));

            // Validate size before adding
            MessageValidator.ValidateAddingFile(_currentTotalSize, content.Length);

            tFilesArrayDmFile file = new tFilesArrayDmFile
            {
                dmFileDescr = fileName,
                dmMimeType = mimeType ?? DataBoxHelper.GetMimeType(fileName),
                Item = content
            };
            
            _files.Add(file);
            _currentTotalSize += content.Length;
            
            return this;
        }

        /// <summary>
        /// Adds a text content file as an attachment.
        /// </summary>
        /// <param name="fileName">Name of the text file.</param>
        /// <param name="textContent">Text content.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public DatovkaMessageBuilder AddTextContent(string fileName, string textContent)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(textContent))
                throw new ArgumentNullException(nameof(textContent));

            byte[] content = Encoding.UTF8.GetBytes(textContent);
            return AddAttachment(fileName, content, "text/plain");
        }

        /// <summary>
        /// Builds and validates the message.
        /// </summary>
        /// <returns>The constructed and validated message ready for sending.</returns>
        /// <exception cref="MissingRequiredFieldException">Thrown when required fields are missing.</exception>
        /// <exception cref="MissingMainFileException">Thrown when no attachments are present.</exception>
        public tMessageCreateInput Build()
        {
            // Set attachments
            _message.dmFiles = _files.ToArray();

            // Validate the complete message
            MessageValidator.ValidateMessageForSending(_message);

            return _message;
        }

        /// <summary>
        /// Gets the current total size of all attachments in bytes.
        /// </summary>
        public long GetCurrentTotalSize() => _currentTotalSize;

        /// <summary>
        /// Gets the number of attachments currently added.
        /// </summary>
        public int GetAttachmentCount() => _files.Count;
    }
}

