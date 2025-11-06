using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using DatovkaSharp.Services.Access;
using DatovkaSharp.Services.Info;
using DatovkaSharp.Services.Operations;
using DatovkaSharp.Services.Search;
using DatovkaSharp.Services.Stat;
using DatovkaSharp.Services.VoDZ;

namespace DatovkaSharp
{
    /// <summary>
    /// Main client for Czech Data Box (ISDS) operations
    /// </summary>
    public class DatovkaClient : IDisposable
    {
        private readonly DataBoxEnvironment _environment;
        private string? _username;
        private string? _password;
        private string? _certificatePath;
        private byte[]? _certificateBytes;
        private X509Certificate2? _certificate;
        private bool _useCertificate;
        private CertificateAuthenticationMode _certAuthMode;
        private string? _dataBoxId;
        
        private DataBoxAccessPortTypeClient? _accessClient;
        private DataBoxSearchPortTypeClient? _searchClient;
        private dmInfoPortTypeClient? _infoClient;
        private dmOperationsPortTypeClient? _operationsClient;
        private IsdsStatPortTypeClient? _statClient;
        private dmVoDZPortTypeClient? _vodzClient;

        /// <summary>
        /// Simple API wrapper instance
        /// </summary>
        public DatovkaApi Api { get; }

        /// <summary>
        /// Creates a new DatovkaClient instance
        /// </summary>
        /// <param name="environment">The environment to connect to</param>
        public DatovkaClient(DataBoxEnvironment environment = DataBoxEnvironment.Production)
        {
            _environment = environment;
            Api = new DatovkaApi(this);
        }

        /// <summary>
        /// Login with username and password
        /// </summary>
        public void LoginWithUsernameAndPassword(string username, string password)
        {
            _username = username ?? throw new ArgumentNullException(nameof(username));
            _password = password ?? throw new ArgumentNullException(nameof(password));
            _useCertificate = false;
        }

        /// <summary>
        /// Login with certificate from file (SS - Spisová služba mode)
        /// </summary>
        public void LoginWithCertificate(string certificatePath, string? password = null)
        {
            if (string.IsNullOrEmpty(certificatePath))
                throw new ArgumentNullException(nameof(certificatePath));
            
            if (!System.IO.File.Exists(certificatePath))
                throw new System.IO.FileNotFoundException($"Certificate file not found: {certificatePath}", certificatePath);
            
            _certificatePath = certificatePath;
            _password = password;
            _useCertificate = true;
            _certAuthMode = CertificateAuthenticationMode.FilingService;
        }

        /// <summary>
        /// Login with certificate from byte array (SS - Spisová služba mode)
        /// </summary>
        public void LoginWithCertificate(byte[] certificateBytes, string? password = null)
        {
            _certificateBytes = certificateBytes ?? throw new ArgumentNullException(nameof(certificateBytes));
            _password = password;
            _useCertificate = true;
            _certAuthMode = CertificateAuthenticationMode.FilingService;
        }

        /// <summary>
        /// Login with certificate from stream (SS - Spisová služba mode)
        /// </summary>
        public void LoginWithCertificate(System.IO.Stream certificateStream, string? password = null)
        {
            if (certificateStream == null)
                throw new ArgumentNullException(nameof(certificateStream));
            
            using var ms = new System.IO.MemoryStream();
            certificateStream.CopyTo(ms);
            LoginWithCertificate(ms.ToArray(), password);
        }

        /// <summary>
        /// Login with X509Certificate2 object directly (SS - Spisová služba mode)
        /// </summary>
        public void LoginWithCertificate(X509Certificate2 certificate)
        {
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            _useCertificate = true;
            _certAuthMode = CertificateAuthenticationMode.FilingService;
        }

        /// <summary>
        /// Login with certificate and DataBox ID (HSS - Hostovaná spisová služba mode) from file.
        /// Used by external applications to access specific databoxes.
        /// </summary>
        public void LoginWithCertificateAndDataBoxId(string certificatePath, string dataBoxId, string? password = null)
        {
            if (string.IsNullOrEmpty(certificatePath))
                throw new ArgumentNullException(nameof(certificatePath));
            
            if (!System.IO.File.Exists(certificatePath))
                throw new System.IO.FileNotFoundException($"Certificate file not found: {certificatePath}", certificatePath);
            
            if (string.IsNullOrEmpty(dataBoxId))
                throw new ArgumentNullException(nameof(dataBoxId));
            
            _certificatePath = certificatePath;
            _dataBoxId = dataBoxId;
            _password = password;
            _useCertificate = true;
            _certAuthMode = CertificateAuthenticationMode.HostedFilingService;
        }

        /// <summary>
        /// Login with certificate and DataBox ID (HSS - Hostovaná spisová služba mode) from byte array.
        /// Used by external applications to access specific databoxes.
        /// </summary>
        public void LoginWithCertificateAndDataBoxId(byte[] certificateBytes, string dataBoxId, string? password = null)
        {
            _certificateBytes = certificateBytes ?? throw new ArgumentNullException(nameof(certificateBytes));
            _dataBoxId = dataBoxId ?? throw new ArgumentNullException(nameof(dataBoxId));
            _password = password;
            _useCertificate = true;
            _certAuthMode = CertificateAuthenticationMode.HostedFilingService;
        }

        /// <summary>
        /// Login with certificate and DataBox ID (HSS - Hostovaná spisová služba mode) from stream.
        /// Used by external applications to access specific databoxes.
        /// </summary>
        public void LoginWithCertificateAndDataBoxId(System.IO.Stream certificateStream, string dataBoxId, string? password = null)
        {
            if (certificateStream == null)
                throw new ArgumentNullException(nameof(certificateStream));
            
            using var ms = new System.IO.MemoryStream();
            certificateStream.CopyTo(ms);
            LoginWithCertificateAndDataBoxId(ms.ToArray(), dataBoxId, password);
        }

        /// <summary>
        /// Login with X509Certificate2 object directly and DataBox ID (HSS - Hostovaná spisová služba mode).
        /// Used by external applications to access specific databoxes.
        /// </summary>
        public void LoginWithCertificateAndDataBoxId(X509Certificate2 certificate, string dataBoxId)
        {
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            _dataBoxId = dataBoxId ?? throw new ArgumentNullException(nameof(dataBoxId));
            _useCertificate = true;
            _certAuthMode = CertificateAuthenticationMode.HostedFilingService;
        }

        /// <summary>
        /// Get Data Box Access service client
        /// </summary>
        public DataBoxAccessPortTypeClient GetAccessClient()
        {
            if (_accessClient == null)
            {
                EndpointAddress endpoint = GetEndpointAddress(DataBoxHelper.ACCESS_WS);
                Binding binding = CreateBinding();
                _accessClient = new DataBoxAccessPortTypeClient(binding, endpoint);
                ConfigureClient(_accessClient.ClientCredentials);
            }
            return _accessClient;
        }

        /// <summary>
        /// Get Data Box Search service client
        /// </summary>
        public DataBoxSearchPortTypeClient GetSearchClient()
        {
            if (_searchClient == null)
            {
                EndpointAddress endpoint = GetEndpointAddress(DataBoxHelper.SEARCH_WS);
                Binding binding = CreateBinding();
                _searchClient = new DataBoxSearchPortTypeClient(binding, endpoint);
                ConfigureClient(_searchClient.ClientCredentials);
            }
            return _searchClient;
        }

        /// <summary>
        /// Get DM Info service client
        /// </summary>
        public dmInfoPortTypeClient GetInfoClient()
        {
            if (_infoClient == null)
            {
                EndpointAddress endpoint = GetEndpointAddress(DataBoxHelper.INFO_WS);
                Binding binding = CreateBinding();
                _infoClient = new dmInfoPortTypeClient(binding, endpoint);
                ConfigureClient(_infoClient.ClientCredentials);
            }
            return _infoClient;
        }

        /// <summary>
        /// Get DM Operations service client
        /// </summary>
        public dmOperationsPortTypeClient GetOperationsClient()
        {
            if (_operationsClient == null)
            {
                EndpointAddress endpoint = GetEndpointAddress(DataBoxHelper.OPERATIONS_WS);
                Binding binding = CreateBinding();
                _operationsClient = new dmOperationsPortTypeClient(binding, endpoint);
                ConfigureClient(_operationsClient.ClientCredentials);
            }
            return _operationsClient;
        }

        /// <summary>
        /// Get ISDS Statistics service client
        /// </summary>
        public IsdsStatPortTypeClient GetStatClient()
        {
            if (_statClient == null)
            {
                EndpointAddress endpoint = GetEndpointAddress(DataBoxHelper.STAT_WS);
                Binding binding = CreateBinding();
                _statClient = new IsdsStatPortTypeClient(binding, endpoint);
                ConfigureClient(_statClient.ClientCredentials);
            }
            return _statClient;
        }

        /// <summary>
        /// Get VoDZ (large messages) service client
        /// </summary>
        public dmVoDZPortTypeClient GetVoDZClient()
        {
            if (_vodzClient == null)
            {
                EndpointAddress endpoint = GetEndpointAddress(DataBoxHelper.VODZ_WS);
                Binding binding = CreateBinding();
                _vodzClient = new dmVoDZPortTypeClient(binding, endpoint);
                ConfigureClient(_vodzClient.ClientCredentials);
            }
            return _vodzClient;
        }

        private EndpointAddress GetEndpointAddress(int serviceType)
        {
            string url = BuildServiceUrl(serviceType);
            return new EndpointAddress(url);
        }

        private string BuildServiceUrl(int serviceType)
        {
            string baseUrl = "https://ws";
            
            // VoDZ uses ws2
            if (serviceType == DataBoxHelper.VODZ_WS)
            {
                baseUrl = "https://ws2";
            }
            else
            {
                baseUrl = "https://ws1";
            }

            // Certificate login adds 'c'
            if (_useCertificate)
            {
                baseUrl += "c";
            }

            // Environment
            baseUrl += _environment == DataBoxEnvironment.Test 
                ? ".czebox.cz/" 
                : ".mojedatovaschranka.cz/";

            // Certificate path - depends on authentication mode
            if (_useCertificate)
            {
                baseUrl += _certAuthMode == CertificateAuthenticationMode.HostedFilingService 
                    ? "hspis/" 
                    : "cert/";
            }

            baseUrl += "DS/";

            // Service-specific endpoint
            baseUrl += serviceType switch
            {
                DataBoxHelper.VODZ_WS => "vodz",
                DataBoxHelper.OPERATIONS_WS => "dz",
                DataBoxHelper.INFO_WS => "dx",
                DataBoxHelper.SEARCH_WS => "df",
                DataBoxHelper.ACCESS_WS or DataBoxHelper.STAT_WS => "DsManage",
                _ => throw new DataBoxException($"Unknown service type: {serviceType}")
            };

            return baseUrl;
        }

        private Binding CreateBinding()
        {
            BasicHttpsBinding binding = new BasicHttpsBinding(BasicHttpsSecurityMode.Transport)
            {
                MaxReceivedMessageSize = 10485760, // 10 MB
                MaxBufferSize = 10485760,
                SendTimeout = TimeSpan.FromMinutes(5),
                ReceiveTimeout = TimeSpan.FromMinutes(5),
                OpenTimeout = TimeSpan.FromMinutes(1),
                CloseTimeout = TimeSpan.FromMinutes(1)
            };

            if (_useCertificate)
            {
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            }
            else
            {
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            }

            return binding;
        }

        private void ConfigureClient(ClientCredentials? credentials)
        {
            if (_useCertificate)
            {
                // Load certificate
                X509Certificate2 cert;
                if (_certificate != null)
                {
                    // Use provided certificate object directly
                    cert = _certificate;
                }
                else if (_certificateBytes != null)
                {
                    // From byte array
                    cert = string.IsNullOrEmpty(_password)
                        ? new X509Certificate2(_certificateBytes)
                        : new X509Certificate2(_certificateBytes, _password);
                }
                else if (!string.IsNullOrEmpty(_certificatePath))
                {
                    // From file
                    cert = string.IsNullOrEmpty(_password)
                        ? new X509Certificate2(_certificatePath)
                        : new X509Certificate2(_certificatePath, _password);
                }
                else
                {
                    throw new DataBoxException("Certificate object, path, or bytes must be provided");
                }
                
                if (credentials != null)
                {
                    credentials.ClientCertificate.Certificate = cert;
                    
                    // HSS mode requires DataBox ID in username
                    if (_certAuthMode == CertificateAuthenticationMode.HostedFilingService)
                    {
                        credentials.UserName.UserName = _dataBoxId;
                    }
                }
            }
            else
            {
                // Username/password authentication
                credentials?.UserName.UserName = _username;
                credentials?.UserName.Password = _password;
            }
        }

        /// <summary>
        /// Test the connection to ISDS
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                DatovkaResult<string> result = await Api.GetStatsAsync();
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _accessClient?.Close();
            _searchClient?.Close();
            _infoClient?.Close();
            _operationsClient?.Close();
            _statClient?.Close();
            _vodzClient?.Close();
        }
    }
}

