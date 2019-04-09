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
    public HttpResponseMessage Go(string key, string domain = null, string url = null, bool debug = true)
    {
        // lower the key, just to be sure
        key = key.ToLower();

        // 1. get all links
        var all = AsDynamic(App.Data["Link"]);

        // 2. try to find the key all lower case (assumes that the system is all lower-case & case-insensitive)
        var current = all.FirstOrDefault(l => l.Key == key);

        // not found - redirect to fallback
        var link = "";
        if(current == null)
            link = App.Settings.FallbackLink;
        else {
            link = current.Link;

            // decide what to do with the link / how to handle it
            var grp = ((IEnumerable<dynamic>)current.Group).FirstOrDefault();            
            var mode = grp != null ? grp.RedirectType : "default";
            string forward = grp != null ? grp.ForwardHandler.ToString() : "";
            string forwardRetired = grp != null ? grp.ForwardRetired.ToString() : "";
            var additionalAnalyticsParamBool = "";
            var additionalAnalyticsParam = "";

            // check for analytic params settings
            if(grp != null){
                if(!String.IsNullOrEmpty(grp.EnableAddingQueryString)) {
                    additionalAnalyticsParamBool = grp.EnableAddingQueryString;
                } else {
                    additionalAnalyticsParamBool = App.Settings.EnableAddingQueryString;
                }
            } else {
                additionalAnalyticsParamBool = App.Settings.EnableAddingQueryString;
            }

            if(additionalAnalyticsParamBool == "true"){
                if(grp != null && !String.IsNullOrEmpty(grp.QueryStringAddition)){
                    additionalAnalyticsParam = grp.QueryStringAddition;
                } else {
                    additionalAnalyticsParam = App.Settings.QueryStringAddition;
                }                

                link = TargetUrlWithParams(link, additionalAnalyticsParam);
            }

            // 5. if yes and retired: use redirect to app-setting
            if(current.Retired != null && current.Retired)
                link = App.Settings.RetiredLink;

            // if group setting retired link not empty and retired: use redirect to group settings
            if(!String.IsNullOrEmpty(forwardRetired) && current.Retired != null && current.Retired)
                link = forwardRetired;

            if(mode == "forward" && !String.IsNullOrEmpty(forward))
                link = ReplaceCI(forward, "{link}", link);
        }

        // now inject various patterns as needed
        link = ReplaceCI(link, "{key}", key);
        link = ReplaceCI(link, "{domain}", domain);
        link = ReplaceCI(link, "{url}", url);
        
        if(link.StartsWith("/")) {
            var defProt = App.Settings.TargetProtocol;
            var defHost = App.Settings.TargetDomain;

            if(!string.IsNullOrEmpty(defHost))
                link = defHost + link;
                
            if(!string.IsNullOrEmpty(defProt))
                link = defProt + link;
        }



        // redirect
        if(debug)
            return Message("debug: would redirect to " + link);
        else
            return Redirect(link);
    }


    private HttpResponseMessage Message(string message){
        var response = Request.CreateResponse(HttpStatusCode.OK, message);
        //response.Content = message;
        return response;
    }

    private HttpResponseMessage Redirect(string link){
        // now redirect
        var response = Request.CreateResponse(HttpStatusCode.Redirect);
        response.Headers.Location = new Uri(link, UriKind.RelativeOrAbsolute);
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

    public static string TargetUrlWithParams(string link, string linkParams){
        string paramPrefix = "?";
        string[] urlItems = {};

        if(link.Contains("?")){
           paramPrefix = "&";
        }
        
        if(link.Contains("#")){
            urlItems = link.Split('#');
            return urlItems[0] + paramPrefix + linkParams + "#" + urlItems[1];
        } else {
            return link + paramPrefix + linkParams;
        }
    }
}

