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
using System.Linq;
using System.Net;
using System.Net.Http;
using ToSic.Razor.Blade;

[AllowAnonymous]
public class RedirectController : Custom.Hybrid.Api12
{
	[HttpGet]
  public dynamic Go(string key, string domain = null, string url = null, bool debug = false) {
    var helper = CreateInstance("RedirectHelper.cs");

    // Pro feature: Check if we're using CAP$
    var realQrScanDetected = helper.IsQrCode(ref key);

    // Set analytics
    // if (Settings.EnableQrUseTracking && realQrScanDetected) {}

    // Lower the key, just to be sure
    key = key.ToLower();

    // Get all links
    var allLinks = AsList(App.Data["Link"]);

    // Try to find the key all lower case (assumes that the system is all lower-case & case-insensitive)
    var currentLink = allLinks.FirstOrDefault(l => l.Key == key);

    var linkGroup = AsList(currentLink.Group as object).FirstOrDefault();
    bool hasLinkGroup = linkGroup != null;

    // Check if analytics are needed

    var analyticsEnabled = hasLinkGroup
      ? Settings.EnableAddingQueryString
      : Text.First(linkGroup.EnableAddingQueryString, Settings.EnableAddingQueryString);

    var analyticsParams = "";

    if(bool.Parse(analyticsEnabled)) {
      analyticsParams = hasLinkGroup
        ? Settings.QueryStringAddition
        : Text.First(linkGroup.QueryStringAddition, Settings.QueryStringAddition);
    }

    // Generate the Link
    var link = helper.GenerateLink(currentLink, Settings.FallbackLink, analyticsParams);

    // Now inject various patterns as needed
    link = helper.ReplaceCI(link, "{key}", key);
    link = helper.ReplaceCI(link, "{domain}", domain);
    link = helper.ReplaceCI(link, "{url}", url);

    // Add defHost
    if(link.StartsWith("/")) {
      var defHost = Settings.TargetDomain;

      if(Text.Has(defHost))
        link = defHost + link;

      link = "//" + link;
    }

    if(debug) {
      return new HttpResponseMessage(HttpStatusCode.OK) { Content = "debug: would redirect to " + link };
    }

    // Redirect
    var response = new HttpResponseMessage(HttpStatusCode.Redirect);
    response.Headers.Location = new Uri(link, UriKind.RelativeOrAbsolute);
    return response;
  }
}
