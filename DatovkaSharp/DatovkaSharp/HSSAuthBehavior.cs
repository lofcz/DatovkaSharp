using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace DatovkaSharp
{
    /// <summary>
    /// Custom behaviour to add Basic auth header for HSS mode while using certificate authentication
    /// </summary>
    internal class HssAuthBehavior(string dataBoxId) : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new HssAuthMessageInspector(dataBoxId));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }

    /// <summary>
    /// Message inspector to inject Basic auth header with DataBox ID for HSS mode
    /// </summary>
    internal class HssAuthMessageInspector(string dataBoxId) : IClientMessageInspector
    {
        public object? BeforeSendRequest(ref Message request, System.ServiceModel.IClientChannel channel)
        {
            // Add Basic auth header with DataBox ID (username) and empty password
            HttpRequestMessageProperty? httpRequest = null;
            
            // Check if the property exists
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out object? property))
            {
                httpRequest = property as HttpRequestMessageProperty;
            }
            
            // Create if it doesn't exist
            if (httpRequest == null)
            {
                httpRequest = new HttpRequestMessageProperty();
                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequest);
            }

            // Create Basic auth header: "username:" (DataBox ID with empty password)
            string credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{dataBoxId}:"));
            httpRequest.Headers["Authorization"] = $"Basic {credentials}";

            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
        }
    }
}

