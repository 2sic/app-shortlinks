// this is the script which actually does the real redirecting 
using DotNetNuke.Security;
using DotNetNuke.Web.Api;
using System.Web.Http;
using ToSic.SexyContent.WebApi;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text.RegularExpressions;

public class RedirectController : SxcApiController
{
    [AllowAnonymous] 
	[HttpGet]
    [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Anonymous)]
    public HttpResponseMessage Go(string key, string domain = null, string url = null, bool debug = false)
    {
        // lower the key, just to be sure
        key = key.ToLower();

        // 1. get all links
        var all = AsDynamic(App.Data["Link"]);

        // 2. try to find the key all lower case (assumes that the system is all lower-case & case-insensitive)
        var current = all.FirstOrDefault(l => l.Key == key);

        if(current == null)
            return GoToDefault(key);


        var link = current.Link;

        // todo: 
        // 4. if yes and not retired: redirect
        // 5. if yes and retired: redirect to app-setting
        if(current.Retired != null && current.Retired)
            link = App.Settings.RetiredLink;

        // decide what to do with the link / how to handle it
        var grp = ((IEnumerable<dynamic>)current.Group).FirstOrDefault();
        var mode = grp != null ? grp.RedirectType : "default";
        string forward = grp != null ? grp.ForwardHandler.ToString() : "";


        if(mode == "forward" && !String.IsNullOrEmpty(forward))
            link = ReplaceCI(forward, "{link}", link);

        // now inject various patters as needed
        link = ReplaceCI(link, "{key}", key);
        link = ReplaceCI(link, "{domain}", domain);
        link = ReplaceCI(link, "{url}", url);

        // todo: maybe log?



        // redirect
        if(debug)
            return Message("debug: would redirect to " + link);
        else
            return  Redirect(link);
    }


    private HttpResponseMessage GoToDefault(string key) {
        var fallback = App.Settings.FallbackLink;
        if(fallback == null)
            throw new Exception("can't find real link and fallback fails too");

        return Message(fallback);
    }

    private HttpResponseMessage Message(string message){
        var response = Request.CreateResponse(HttpStatusCode.OK, message);
        //response.Content = message;
        return response;
    }

    private HttpResponseMessage Redirect(string link){
        // now redirect
        var response = Request.CreateResponse(HttpStatusCode.Moved);
        response.Headers.Location = new Uri(link);
        return response;
    }

    public static string ReplaceCI(string input, string search, string replacement){
        string result = Regex.Replace(
            input ?? "",
            Regex.Escape(search ?? ""), 
            (replacement ?? "").Replace("$","$$"), 
            RegexOptions.IgnoreCase
        );
        return result;
    }
}

