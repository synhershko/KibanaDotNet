using System;
using System.Net;
using System.Threading.Tasks;
using Nancy;
using RestSharp;
using RestSharp.Extensions;
using HttpStatusCode = Nancy.HttpStatusCode;

namespace KibanaDotNet.KibanaHost
{
    public class KibanaModule : NancyModule
    {
        public KibanaModule()
        {
            Get["/config"] = _ =>
            {
                return Response.AsJson(new
                {
                    port = 5062,
                    host = "0.0.0.0",
                    kibana_index = ".kibana",
                    default_app_id = "discover",
                    request_timeout = 60,
                    shard_timeout = 30000,
                    verify_ssl = true,
                    bundledPluginIds = new[] // TODO load dynamically
                    {
                        "plugins/dashboard/index",
                        "plugins/discover/index",
                        "plugins/doc/index",
                        "plugins/kibana/index",
                        "plugins/metric_vis/index",
                        "plugins/settings/index",
                        "plugins/table_vis/index",
                        "plugins/vis_types/index",
                        "plugins/visualize/index"
                    },
                    plugins = new[] // TODO load dynamically
                    {
                        "plugins/dashboard/index",
                        "plugins/discover/index",
                        "plugins/doc/index",
                        "plugins/kibana/index",
                        "plugins/metric_vis/index",
                        "plugins/settings/index",
                        "plugins/table_vis/index",
                        "plugins/vis_types/index",
                        "plugins/visualize/index"
                    },
                });
            };

            Get["/elasticsearch", true] = async (_, ct) =>
            {
                return await ElasticsearchProxy(string.Empty);                
            };

            Get["/elasticsearch/{req*}", true] =
            Post["/elasticsearch/{req*}", true] =
            Put["/elasticsearch/{req*}", true] =
            async (_, ct) =>
            {
                return await ElasticsearchProxy((string)_.req);
            };
        }

        private async Task<Response> ElasticsearchProxy(string req)
        {
            var client = new RestClient("http://localhost:9200/");
            var method = TranslateRestMethod(Request.Method);
            var request = new RestRequest(req + Request.Url.Query, method) { RequestFormat = DataFormat.Json };            
            if (method == Method.POST || method == Method.PUT)
                request.AddParameter("text/json", Request.Body.ReadAsBytes(), ParameterType.RequestBody);

            var taskSource = new TaskCompletionSource<IRestResponse>();
            client.ExecuteAsync(request, (restResponse, handle) => taskSource.SetResult(restResponse));
            var rsp = await taskSource.Task;
            if (rsp.StatusCode == 0 && rsp.ErrorException is WebException)
            {
                return Response.AsJson(rsp.ErrorException, HttpStatusCode.InternalServerError);
            }

            var response = Response.AsText(rsp.Content, rsp.ContentType);
            response.StatusCode = (HttpStatusCode)(int)rsp.StatusCode;
            return response;
        }

        private static Method TranslateRestMethod(string method)
        {
            var ret = Method.GET;
            Enum.TryParse(method, true, out ret);
            return ret;            
        }
    }
}
