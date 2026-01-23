using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Demo1NET4
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // ✅ CORS - CHỈ CONFIG MỘT LẦN Ở ĐÂY
            var cors = new EnableCorsAttribute(
                origins: "http://localhost:4200",  // URL Angular
                headers: "*",
                methods: "*"
            );
            config.EnableCors(cors);

            // ✅ Attribute routing TRƯỚC convention routing
            config.MapHttpAttributeRoutes();

            // ✅ Convention routing
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // ✅ JSON Settings
            var jsonFormatter = config.Formatters.OfType<JsonMediaTypeFormatter>().First();
            jsonFormatter.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            // ✅ Bỏ XML formatter
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}