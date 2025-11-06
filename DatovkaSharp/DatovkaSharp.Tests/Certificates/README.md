# Certificate Test Files

This directory is for placing test certificates to test certificate-based authentication modes.

## Setup

1. Place your test certificate files in this directory
2. Update the `appCfg.json` file with the certificate paths and passwords

## Certificate Types

### SS (Spisová služba) Certificate
- **Mode**: Filing Service
- **Authentication**: System certificate only (no username required)
- **Endpoint**: `https://ws1c.mojedatovaschranka.cz/cert/`
- **Example configuration**:
  ```json
  "ssCertificatePath": "Certificates/ss_certificate.pfx",
  "ssCertificatePassword": "your-certificate-password"
  ```

### HSS (Hostovaná spisová služba) Certificate
- **Mode**: Hosted Filing Service
- **Authentication**: System certificate + Target DataBox ID
- **Endpoint**: `https://ws1c.mojedatovaschranka.cz/hspis/`
- **Use case**: External applications accessing specific data boxes
- **Example configuration**:
  ```json
  "hssCertificatePath": "Certificates/hss_certificate.pfx",
  "hssCertificatePassword": "your-certificate-password",
  "hssDataBoxId": "target-databox-id"
  ```

## Obtaining Test Certificates

For information on obtaining system certificates for ISDS integration:
- Contact your ISDS administrator or integrator
- Refer to the official ISDS documentation
- For test environment certificates, contact Czech POINT support

## Security Note

**Do NOT commit certificate files to version control!**

This directory is included in `.gitignore` to prevent accidental commits of sensitive certificate files.


