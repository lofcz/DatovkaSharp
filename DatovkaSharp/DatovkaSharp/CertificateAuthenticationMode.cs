namespace DatovkaSharp
{
    /// <summary>
    /// Defines the authentication mode when using certificate-based authentication
    /// </summary>
    public enum CertificateAuthenticationMode
    {
        /// <summary>
        /// Spisová služba (Filing Service) - System certificate only.
        /// Uses endpoint: https://ws1c.mojedatovaschranka.cz/cert/
        /// </summary>
        FilingService,
        
        /// <summary>
        /// Hostovaná spisová služba (Hosted Filing Service) - System certificate + DataBox ID in Basic auth username.
        /// Uses endpoint: https://ws1c.mojedatovaschranka.cz/hspis/
        /// Allows external applications to access specific databoxes.
        /// </summary>
        HostedFilingService
    }
}


