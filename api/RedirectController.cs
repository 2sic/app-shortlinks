// this is the script which actually does the real redirecting
// Add namespaces to enable security in Oqtane & Dnn despite the differences
#if NETCOREAPP
using Microsoft.AspNetCore.Authorization; // .net core [AllowAnonymous] & [Authorize]
using Microsoft.AspNetCore.Mvc;           // .net core [HttpGet] / [HttpPost] etc.
#else
// 2sxclint:disable:no-dnn-namespaces 2sxclint:disable:no-web-namespaces
using System.Web.Http;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ToSic.Razor.Blade;

public class RedirectController : Custom.Hybrid.Api12
{
  [AllowAnonymous]
	[HttpGet]
  public dynamic Go(string key, string domain = null, string url = null, bool debug = false) {

    // 0. Pro feature: Check if we're using CAP$
    var realQrScanDetected = IsQrCode(ref key, Settings.EnableQrUseTracking);

    // Lower the key, just to be sure
    key = key.ToLower();

    // 1. get all links
    var allLinks = AsList(App.Data["Link"]);

    // 2. try to find the key all lower case (assumes that the system is all lower-case & case-insensitive)
    var currentLink = allLinks.FirstOrDefault(l => l.Key == key);

    var link = GenerateLink(currentLink);

    // Now inject various patterns as needed
    link = ReplaceCI(link, "{key}", key);
    link = ReplaceCI(link, "{domain}", domain);
    link = ReplaceCI(link, "{url}", url);

    // Add defHost
    if(link.StartsWith("/")) {
      var defHost = Settings.TargetDomain;

      if(Text.Has(defHost))
        link = defHost + link;

      link = "//" + link;
    }

    // Redirect
    if(debug) {
      var response = Request.CreateResponse(HttpStatusCode.OK, "debug: would redirect to " + link);
      response.Content = message;
      return response;
    }
    var response = Request.CreateResponse(HttpStatusCode.Redirect);
    response.Headers.Location = new Uri(link, UriKind.RelativeOrAbsolute);
    return response;
  }
  public static string GenerateLink(dynamic currentLink) {
    // Not found - redirect to fallback
    if(currentLink == null) {
      return Settings.FallbackLink;
    }

    var link = currentLink.Link;

    // decide what to do with the link / how to handle it
    var linkGroup = ((IEnumerable<dynamic>)currentLink.Group).FirstOrDefault();
    var mode = linkGroup != null ? linkGroup.RedirectType : "default";
    string forward = linkGroup != null ? linkGroup.ForwardHandler.ToString() : "";
    string forwardRetired = linkGroup != null ? linkGroup.ForwardRetired.ToString() : "";

    var addAnalytics = linkGroup == null
      ? Settings.EnableAddingQueryString
      : Text.First(linkGroup.EnableAddingQueryString, Settings.EnableAddingQueryString);

    if(addAnalytics == "true") {
      var analyticsParams = linkGroup == null
        ? Settings.QueryStringAddition
        : Text.First(linkGroup.QueryStringAddition, Settings.QueryStringAddition);

      link = TargetUrlWithParams(link, analyticsParams);
    }

    // 5. If yes and retired: use redirect to app-setting
    if(currentLink.Retired != null && currentLink.Retired)
      link = Settings.RetiredLink;

    // If group setting retired link not empty and retired: use redirect to group settings
    if(Text.Has(forwardRetired) && currentLink.Retired != null && currentLink.Retired)
      link = forwardRetired;

    if(mode == "forward" && Text.Has(forward))
      link = ReplaceCI(forward, "{link}", link);
    return link;
  }

  public static bool IsQrCode(ref string key, bool hasQrTracking) {
    // In case it's empty, it could also be null, then errors would pop up
    key = key ?? "";
    if(key.EndsWith("$") && Settings.EnableQrUseTracking) {
      // Remember, to in future tell analytics about this
      realQrScanDetected = true;
      key = key.TrimEnd(new char[] {'$'});
      return true;
    }
    return false;
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
    string paramPrefix = link.Contains("?") ? "&" : "?";
    string[] urlItems = {};

    if(link.Contains("#")) {
      urlItems = link.Split('#');
      return urlItems[0] + paramPrefix + linkParams + "#" + urlItems[1];
    }

    return link + paramPrefix + linkParams;
  }
}
