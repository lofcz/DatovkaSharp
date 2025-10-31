using System;
using System.Collections.Generic;

namespace DatovkaSharp
{
    /// <summary>
    /// Represents a data box message
    /// </summary>
    public class DataBoxMessage
    {
        /// <summary>
        /// Message ID
        /// </summary>
        public string? MessageId { get; set; }

        /// <summary>
        /// Subject/Annotation
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Sender data box ID
        /// </summary>
        public string? SenderDataBoxId { get; set; }

        /// <summary>
        /// Sender name
        /// </summary>
        public string? SenderName { get; set; }

        /// <summary>
        /// Recipient data box ID
        /// </summary>
        public string? RecipientDataBoxId { get; set; }

        /// <summary>
        /// Recipient name
        /// </summary>
        public string? RecipientName { get; set; }

        /// <summary>
        /// Message status
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Delivery time
        /// </summary>
        public DateTime? DeliveryTime { get; set; }

        /// <summary>
        /// Acceptance time
        /// </summary>
        public DateTime? AcceptanceTime { get; set; }

        /// <summary>
        /// List of attachments
        /// </summary>
        public List<DataBoxAttachment> Attachments { get; set; }

        /// <summary>
        /// To hands (recipient person)
        /// </summary>
        public string? ToHands { get; set; }

        /// <summary>
        /// Legal title for law
        /// </summary>
        public string? LegalTitleLaw { get; set; }

        /// <summary>
        /// Legal title year
        /// </summary>
        public string? LegalTitleYear { get; set; }

        /// <summary>
        /// Legal title section
        /// </summary>
        public string? LegalTitleSect { get; set; }

        /// <summary>
        /// Legal title paragraph
        /// </summary>
        public string? LegalTitlePar { get; set; }

        /// <summary>
        /// Legal title point
        /// </summary>
        public string? LegalTitlePoint { get; set; }

        public DataBoxMessage()
        {
            Attachments = [];
        }

        /// <summary>
        /// Get status description
        /// </summary>
        public string GetStatusDescription()
        {
            return DataBoxHelper.MessageStates.ContainsKey(Status) 
                ? DataBoxHelper.MessageStates[Status] 
                : "Unknown";
        }
    }
}

