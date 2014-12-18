using System;
using System.Net;
using System.Threading.Tasks;
using Nancy;
using RestSharp;
using RestSharp.Extensions;
using HttpStatusCode = Nancy.HttpStatusCode;

namespace KibanaHost
{
    public class KibanaModule : NancyModule
    {
        public KibanaModule()
        {
            Get["/config"] = _ =>
            {
                return Response.AsJson(new
                {
                    port = Config.Instance.Port,
                    host = Config.Instance.Host,
                    kibana_index = Config.Instance.KibanaIndex,
                    default_app_id = Config.Instance.DefaultAppId,
                    request_timeout = Config.Instance.RequestTimeout,
                    shard_timeout = Config.Instance.ShardTimeout,
                    verify_ssl = Config.Instance.VerifySsl,
                    bundledPluginIds = Config.Instance.BundledPluginIds,
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
            var client = new RestClient(Config.Instance.Elasticsearch);
            var method = TranslateRestMethod(Request.Method);
            var request = new RestRequest(req + "?" + Request.Url.Query, method) { RequestFormat = DataFormat.Json };            
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
