using GSD.Common.Tracing;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace GSD.Common.Http
{
    public class ConfigHttpRequestor : HttpRequestor
    {
        private readonly string repoUrl;

        public ConfigHttpRequestor(ITracer tracer, Enlistment enlistment, RetryConfig retryConfig)
            : base(tracer, retryConfig, enlistment)
        {
            this.repoUrl = enlistment.RepoUrl;
        }

        public bool TryQueryGSDConfig(bool logErrors, out ServerGSDConfig serverGSDConfig, out HttpStatusCode? httpStatus, out string errorMessage)
        {
            serverGSDConfig = null;
            httpStatus = null;
            errorMessage = null;

            Uri gvfsConfigEndpoint;
            string gvfsConfigEndpointString = this.repoUrl + GSDConstants.Endpoints.GSDConfig;
            try
            {
                gvfsConfigEndpoint = new Uri(gvfsConfigEndpointString);
            }
            catch (UriFormatException e)
            {
                EventMetadata metadata = new EventMetadata();
                metadata.Add("Method", nameof(this.TryQueryGSDConfig));
                metadata.Add("Exception", e.ToString());
                metadata.Add("Url", gvfsConfigEndpointString);
                this.Tracer.RelatedError(metadata, "UriFormatException when constructing Uri", Keywords.Network);

                return false;
            }

            long requestId = HttpRequestor.GetNewRequestId();
            RetryWrapper<ServerGSDConfig> retrier = new RetryWrapper<ServerGSDConfig>(this.RetryConfig.MaxAttempts, CancellationToken.None);

            if (logErrors)
            {
                retrier.OnFailure += RetryWrapper<ServerGSDConfig>.StandardErrorHandler(this.Tracer, requestId, "QueryGvfsConfig");
            }

            RetryWrapper<ServerGSDConfig>.InvocationResult output = retrier.Invoke(
                tryCount =>
                {
                    using (GitEndPointResponseData response = this.SendRequest(
                        requestId,
                        gvfsConfigEndpoint,
                        HttpMethod.Get,
                        requestContent: null,
                        cancellationToken: CancellationToken.None))
                    {
                        if (response.HasErrors)
                        {
                            return new RetryWrapper<ServerGSDConfig>.CallbackResult(response.Error, response.ShouldRetry);
                        }

                        try
                        {
                            string configString = response.RetryableReadToEnd();
                            ServerGSDConfig config = JsonConvert.DeserializeObject<ServerGSDConfig>(configString);
                            return new RetryWrapper<ServerGSDConfig>.CallbackResult(config);
                        }
                        catch (JsonReaderException e)
                        {
                            return new RetryWrapper<ServerGSDConfig>.CallbackResult(e, shouldRetry: false);
                        }
                    }
                });

            if (output.Succeeded)
            {
                serverGSDConfig = output.Result;
                httpStatus = HttpStatusCode.OK;
                return true;
            }

            GitObjectsHttpException httpException = output.Error as GitObjectsHttpException;
            if (httpException != null)
            {
                httpStatus = httpException.StatusCode;
                errorMessage = httpException.Message;
            }

            if (logErrors)
            {
                this.Tracer.RelatedError(
                    new EventMetadata
                    {
                        { "Exception", output.Error.ToString() }
                    },
                    $"{nameof(this.TryQueryGSDConfig)} failed");
            }

            return false;
        }
    }
}
