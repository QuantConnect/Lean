/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Oanda.RestV20.Client;
using Oanda.RestV20.Model;
using RestSharp;

namespace Oanda.RestV20.Api
{
    public partial class DefaultApi
    {
        /// <summary>
        /// Pending Orders List all pending Orders in an Account
        /// </summary>
        /// <exception cref="Oanda.RestV20.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="authorization">The authorization bearer token previously obtained by the client</param>
        /// <param name="accountID">Account Identifier</param>
        /// <param name="acceptDatetimeFormat">Format of DateTime fields in the request and response. (optional)</param>
        /// <returns>JSON string response</returns>
        public string ListPendingOrdersAsJson(string authorization, string accountID, string acceptDatetimeFormat = null)
        {
            // verify the required parameter 'authorization' is set
            if (authorization == null)
                throw new ApiException(400, "Missing required parameter 'authorization' when calling DefaultApi->ListPendingOrders");
            // verify the required parameter 'accountID' is set
            if (accountID == null)
                throw new ApiException(400, "Missing required parameter 'accountID' when calling DefaultApi->ListPendingOrders");

            var localVarPath = "/accounts/{accountID}/pendingOrders";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new Dictionary<String, String>();
            var localVarHeaderParams = new Dictionary<String, String>(Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json"
            };
            String localVarHttpContentType = Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json"
            };
            String localVarHttpHeaderAccept = Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            // set "format" to json by default
            // e.g. /pet/{petId}.{format} becomes /pet/{petId}.json
            localVarPathParams.Add("format", "json");
            if (accountID != null) localVarPathParams.Add("accountID", Configuration.ApiClient.ParameterToString(accountID)); // path parameter
            if (authorization != null) localVarHeaderParams.Add("Authorization", Configuration.ApiClient.ParameterToString(authorization)); // header parameter
            if (acceptDatetimeFormat != null) localVarHeaderParams.Add("Accept-Datetime-Format", Configuration.ApiClient.ParameterToString(acceptDatetimeFormat)); // header parameter

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse)Configuration.ApiClient.CallApi(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("ListPendingOrders", localVarResponse);
                if (exception != null) throw exception;
            }

            return localVarResponse.Content;
        }


        /// <summary>
        /// Create Order Create an Order for an Account
        /// </summary>
        /// <exception cref="Oanda.RestV20.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="authorization">The authorization bearer token previously obtained by the client</param>
        /// <param name="accountID">Account Identifier</param>
        /// <param name="createOrderBody"></param>
        /// <param name="acceptDatetimeFormat">Format of DateTime fields in the request and response. (optional)</param>
        /// <returns>ApiResponse of InlineResponse201</returns>
        public ApiResponse<InlineResponse201> CreateOrder(string authorization, string accountID, string createOrderBody, string acceptDatetimeFormat = null)
        {
            // verify the required parameter 'authorization' is set
            if (authorization == null)
                throw new ApiException(400, "Missing required parameter 'authorization' when calling DefaultApi->CreateOrder");
            // verify the required parameter 'accountID' is set
            if (accountID == null)
                throw new ApiException(400, "Missing required parameter 'accountID' when calling DefaultApi->CreateOrder");
            // verify the required parameter 'createOrderBody' is set
            if (createOrderBody == null)
                throw new ApiException(400, "Missing required parameter 'createOrderBody' when calling DefaultApi->CreateOrder");

            var localVarPath = "/accounts/{accountID}/orders";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new Dictionary<String, String>();
            var localVarHeaderParams = new Dictionary<String, String>(Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json"
            };
            String localVarHttpContentType = Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json"
            };
            String localVarHttpHeaderAccept = Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            // set "format" to json by default
            // e.g. /pet/{petId}.{format} becomes /pet/{petId}.json
            localVarPathParams.Add("format", "json");
            if (accountID != null) localVarPathParams.Add("accountID", Configuration.ApiClient.ParameterToString(accountID)); // path parameter
            if (authorization != null) localVarHeaderParams.Add("Authorization", Configuration.ApiClient.ParameterToString(authorization)); // header parameter
            if (acceptDatetimeFormat != null) localVarHeaderParams.Add("Accept-Datetime-Format", Configuration.ApiClient.ParameterToString(acceptDatetimeFormat)); // header parameter

            localVarPostBody = createOrderBody; // json

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse)Configuration.ApiClient.CallApi(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int)localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("CreateOrder", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<InlineResponse201>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (InlineResponse201)Configuration.ApiClient.Deserialize(localVarResponse, typeof(InlineResponse201)));
        }

        /// <summary>
        /// Replace Order Replace an Order in an Account by simultaneously cancelling it and creating a replacement Order
        /// </summary>
        /// <exception cref="Oanda.RestV20.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="authorization">The authorization bearer token previously obtained by the client</param>
        /// <param name="accountID">Account Identifier</param>
        /// <param name="orderSpecifier">The Order Specifier</param>
        /// <param name="replaceOrderBody">Specification of the replacing Order. The replacing order must have the same type as the replaced Order.</param>
        /// <param name="acceptDatetimeFormat">Format of DateTime fields in the request and response. (optional)</param>
        /// <returns>ApiResponse of InlineResponse2011</returns>
        public ApiResponse<InlineResponse2011> ReplaceOrder(string authorization, string accountID, string orderSpecifier, string replaceOrderBody, string acceptDatetimeFormat = null)
        {
            // verify the required parameter 'authorization' is set
            if (authorization == null)
                throw new ApiException(400, "Missing required parameter 'authorization' when calling DefaultApi->ReplaceOrder");
            // verify the required parameter 'accountID' is set
            if (accountID == null)
                throw new ApiException(400, "Missing required parameter 'accountID' when calling DefaultApi->ReplaceOrder");
            // verify the required parameter 'orderSpecifier' is set
            if (orderSpecifier == null)
                throw new ApiException(400, "Missing required parameter 'orderSpecifier' when calling DefaultApi->ReplaceOrder");
            // verify the required parameter 'replaceOrderBody' is set
            if (replaceOrderBody == null)
                throw new ApiException(400, "Missing required parameter 'replaceOrderBody' when calling DefaultApi->ReplaceOrder");

            var localVarPath = "/accounts/{accountID}/orders/{orderSpecifier}";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new Dictionary<String, String>();
            var localVarHeaderParams = new Dictionary<String, String>(Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
                "application/json"
            };
            String localVarHttpContentType = Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "application/json"
            };
            String localVarHttpHeaderAccept = Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            // set "format" to json by default
            // e.g. /pet/{petId}.{format} becomes /pet/{petId}.json
            localVarPathParams.Add("format", "json");
            if (accountID != null) localVarPathParams.Add("accountID", Configuration.ApiClient.ParameterToString(accountID)); // path parameter
            if (orderSpecifier != null) localVarPathParams.Add("orderSpecifier", Configuration.ApiClient.ParameterToString(orderSpecifier)); // path parameter
            if (authorization != null) localVarHeaderParams.Add("Authorization", Configuration.ApiClient.ParameterToString(authorization)); // header parameter
            if (acceptDatetimeFormat != null) localVarHeaderParams.Add("Accept-Datetime-Format", Configuration.ApiClient.ParameterToString(acceptDatetimeFormat)); // header parameter

            localVarPostBody = replaceOrderBody; // json

            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse)Configuration.ApiClient.CallApi(localVarPath,
                Method.PUT, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int)localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("ReplaceOrder", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<InlineResponse2011>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (InlineResponse2011)Configuration.ApiClient.Deserialize(localVarResponse, typeof(InlineResponse2011)));
        }

    }
}
