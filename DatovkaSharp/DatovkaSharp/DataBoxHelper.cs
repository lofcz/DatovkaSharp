using System.Collections.Generic;

namespace DatovkaSharp
{
    /// <summary>
    /// Helper class for Czech Data Box constants and utilities
    /// </summary>
    public static class DataBoxHelper
    {
        // Service type constants
        public const int OPERATIONS_WS = 0;
        public const int INFO_WS = 1;
        public const int SEARCH_WS = 2;
        public const int ACCESS_WS = 3;
        public const int STAT_WS = 4;
        public const int VODZ_WS = 5;

        /// <summary>
        /// Data Box types
        /// </summary>
        public static Dictionary<string, string> DataBoxTypes => new Dictionary<string, string>
        {
            { "FO", "Fyzická osoba" },
            { "PFO", "Podnikající fyzická osoba" },
            { "PFO_ADVOK", "Advokát" },
            { "PFO_DANPOR", "Daňový poradce" },
            { "PFO_INSSPR", "Insolvenční správce" },
            { "PO", "Právnická osoba" },
            { "PO_ZAK", "PO_ZAK" },
            { "PO_REQ", "PO_REQ" },
            { "OVM", "Orgán veřejné moci" },
            { "OVM_NOTAR", "Notář" },
            { "OVM_EXEKUT", "Exekutor" },
            { "OVM_REQ", "OVM_REQ" }
        };

        /// <summary>
        /// Message states
        /// </summary>
        public static Dictionary<int, string> MessageStates => new Dictionary<int, string>
        {
            { 1, "Podáno" },
            { 2, "Odeslána" },
            { 3, "Chyba - vir" },
            { 4, "Dodána" },
            { 5, "Doručeno fikcí" },
            { 6, "Doručeno" },
            { 7, "Přečteno" },
            { 8, "Nedoručitelné" },
            { 9, "Smazáno" },
            { 10, "Datový trezor" }
        };

        /// <summary>
        /// Message state descriptions
        /// </summary>
        public static Dictionary<int, string> MessageStateDescriptions => new Dictionary<int, string>
        {
            { 1, "Zpráva byla podána (vznikla v ISDS)" },
            { 2, "Datová zpráva včetně písemností podepsána časovým razítkem" },
            { 3, "Zpráva neprošla AV kontrolou; nakažená písemnost je smazána; konečný stav zprávy před smazáním" },
            { 4, "Zpráva dodána do ISDS (zapsán čas dodání)" },
            { 5, "Uplynulo 10 dní od dodání veřejné zprávy, která dosud nebyla doručena přihlášením (předpoklad doručení fikcí u neOVM DS); u komerční zprávy nemůže tento stav nastat" },
            { 6, "Osoba oprávněná číst tuto zprávu se přihlásila - dodaná zpráva byla doručena" },
            { 7, "Zpráva byla přečtena (na portále nebo akcí ESS)" },
            { 8, "Zpráva byla označena jako nedoručitelná, protože DS adresáta byla zpětně znepřístupněna" },
            { 9, "Obsah zprávy byl smazán, obálka zprávy včetně hashů přesunuta do archivu" },
            { 10, "Zpráva je v Datovém trezoru" }
        };

        /// <summary>
        /// Get MIME type for a file
        /// </summary>
        public static string GetMimeType(string fileName)
        {
            Dictionary<string, string> mimeTypes = new Dictionary<string, string>
            {
                { "isdoc", "text/isdoc" },
                { "isdocx", "text/isdocx" },
                { "zfo", "application/vnd.software602.filler.form-xml-zip" }
            };

            string extension = System.IO.Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;
            
            if (!string.IsNullOrEmpty(extension) && mimeTypes.TryGetValue(extension, out string? type))
            {
                return type;
            }

            // Default MIME type detection for common types
            return extension switch
            {
                "pdf" => "application/pdf",
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "txt" => "text/plain",
                "xml" => "text/xml",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xls" => "application/vnd.ms-excel",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}

