// this is the script which actually does the real redirecting 
using DotNetNuke.Security;
using DotNetNuke.Web.Api;
using System.Web.Http;
using ToSic.SexyContent.WebApi;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Web.Compilation;
using System.Runtime.CompilerServices;
using DotNetNuke.Services.Mail;
using Newtonsoft.Json;

public class RedirectController : SxcApiController
{
    [AllowAnonymous] 
	[HttpGet]
    [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Anonymous)]
    public string Go(string key, string domain = "", string full = null)
    {
        // lower the key, just to be sure
        key = key.ToLower();

        // 1. get all links
        var all = AsDynamic(App.Data["Link"]);

        // 2. try to find the key all lower case (assumes that the system is case-insensitive)
        var current = all.FirstOrDefault(l => l.Key == key);

        if(current == null)
            return GoToDefault(key);

        // todo: 
        // 4. if yes and not retired: redirect
        // 5. if yes and retired: redirect to app-setting
        if(current.Retired != null && current.Retired)
            return GoToDefault(key);

        // todo: maybe log?

        // redirect
        Response.Redirect(current.Link);
    }

    private string GoToDefault(string key) {
        var fallback = App.Settings.FallbackLink;
        if(fallback == null)
            throw new Exception("can't find real link and fallback fails too");

        return fallback;
    }


}
