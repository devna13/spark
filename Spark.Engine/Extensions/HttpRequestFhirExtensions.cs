﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Spark.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using Spark.Core;
using System.Net;
using Hl7.Fhir.Serialization;
using System.Net.Http.Headers;

namespace Spark.Core
{
    public static class HttpRequestFhirExtensions
    {
        
        public static void SaveBody(this HttpRequestMessage request, string contentType, byte[] data)
        {
            Binary b = new Binary { Content = data, ContentType = contentType };

            request.Properties.Add(Const.UNPARSED_BODY, b);
        }

        public static Binary GetBody(this HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey(Const.UNPARSED_BODY))
                return request.Properties[Const.UNPARSED_BODY] as Binary;
            else
                return null;
        }

        /// <summary>
        /// Temporary hack!
        /// Adds a resourceEntry to the request property bag. To be picked up by the MediaTypeFormatters for adding http headers.
        /// </summary>
        /// <param name="entry">The resource entry with information to generate headers</param>
        /// <remarks> 
        /// The SendAsync is called after the headers are set. The SetDefaultHeaders have no access to the content object.
        /// The only solution is to give the information through the Request Property Bag.
        /// </remarks>
        public static void SaveEntry(this HttpRequestMessage request, Interaction entry)
        {
            request.Properties.Add(Const.RESOURCE_ENTRY, entry);
        }

        public static Interaction GetEntry(this HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey(Const.RESOURCE_ENTRY))
                return request.Properties[Const.RESOURCE_ENTRY] as Interaction;
            else
                return null;
        }

        //public static HttpResponseMessage HttpResponse(this HttpRequestMessage request, HttpStatusCode code, Entry entry)
        //{
        //    request.SaveEntry(entry);

        //    HttpResponseMessage msg;
        //    msg = request.CreateResponse<Resource>(code, entry.Resource);
            
        //    // DSTU2: tags
        //    //msg.Headers.SetFhirTags(entry.Tags);
        //    return msg;
        //}

       

        public static void AcquireHeaders(this HttpResponseMessage response, FhirResponse fhir)
        {
            // http.StatusCode = fhir.StatusCode;
            if (fhir.Key != null)
            {
                response.Headers.ETag = ETag.Create(fhir.Key.VersionId);
                response.Content.Headers.ContentLocation = fhir.Key.ToUri(Localhost.Base);
            }

            if (fhir.Resource != null && fhir.Resource.Meta != null)
            {
                response.Content.Headers.LastModified = fhir.Resource.Meta.LastUpdated;
            }
        }
       
        private static HttpResponseMessage CreateBareFhirResponse(this HttpRequestMessage request, FhirResponse fhir)
        {
            if (fhir.Resource != null)
            {
                return request.CreateResponse(fhir.StatusCode, fhir.Resource);
            }
            else
            {
                return request.CreateResponse(fhir.StatusCode);
            }
        }

        public static HttpResponseMessage CreateResponse(this HttpRequestMessage request, FhirResponse fhir)
        {
            HttpResponseMessage message = request.CreateBareFhirResponse(fhir);
            message.AcquireHeaders(fhir);
            return message;
        }

        /*
        public static HttpResponseMessage HttpResponse(this HttpRequestMessage request, Entry entry)
        {
            return request.HttpResponse(HttpStatusCode.OK, entry);
        }
        */

        //public static HttpResponseMessage Error(this HttpRequestMessage request, int code, OperationOutcome outcome)
        //{
        //    return request.CreateResponse((HttpStatusCode)code, outcome);
        //}

        //public static HttpResponseMessage StatusResponse(this HttpRequestMessage request, Entry entry, HttpStatusCode code)
        //{
        //    request.SaveEntry(entry);
        //    HttpResponseMessage msg = request.CreateResponse(code);
        //    // DSTU2: tags
        //    // msg.Headers.SetFhirTags(entry.Tags); // todo: move to model binder
        //    msg.Headers.Location = entry.Key.ToUri(Localhost.Base);
        //    return msg;
        //}

        /*
        public static ICollection<Tag> GetFhirTags(this HttpRequestMessage request)
        {
            return request.Headers.GetFhirTags();
        }
        */

        public static DateTimeOffset? GetDateParameter(this HttpRequestMessage request, string name)
        {
            string param = request.GetParameter(name);
            if (param == null) return null;
            return DateTimeOffset.Parse(param);
        }

        public static int? GetIntParameter(this HttpRequestMessage request, string name)
        {
            string s = request.GetParameter(name);
            int n;
            return (int.TryParse(s, out n)) ? n : (int?)null;
        }

        public static bool? GetBooleanParameter(this HttpRequestMessage request, string name)
        {
            string s = request.GetParameter(name);           
            if(s == null) return null;

            try
            {
                bool b = PrimitiveTypeConverter.Convert<bool>(s);
                return (bool.TryParse(s, out b)) ? b : (bool?)null;
            }
            catch
            {
                return null;
            }
        }

        public static DateTimeOffset? IfModifiedSince(this HttpRequestMessage request)
        {
            return request.Headers.IfModifiedSince;
            //string s = request.Header("If-Modified-Since");
            //DateTimeOffset date;
            //if (DateTimeOffset.TryParse(s, out date))
            //{
            //    return date;
            //}
            //{ 
            //    return null;
            //}
        }

        public static IEnumerable<string> IfNoneMatch(this HttpRequestMessage request)
        {
            // The if-none-match can be either '*' or tags. This needs further implementation.
            return request.Headers.IfNoneMatch.Select(h => h.Tag);
        }

        public static string IfMatchVersionId(this HttpRequestMessage request)
        {
            EntityTagHeaderValue tag = request.Headers.IfMatch.FirstOrDefault();
            string versionid = (tag != null) ? tag.Tag : null;
            return versionid;
        }

        public static bool RequestSummary(this HttpRequestMessage request)
        {
            return (request.GetParameter("_summary") == "true");
        }

    }
}