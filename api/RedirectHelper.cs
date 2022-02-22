using System;
using System.Linq;
using System.Text.RegularExpressions;
using ToSic.Razor.Blade;

public class RedirectHelper : Custom.Hybrid.Code12 {
  public string GenerateLink(dynamic currentLink, string fallbackLink, string analyticsParams = null) {
    // Not found - redirect to fallback
    if(currentLink == null) {
      return fallbackLink;
    }

    var link = currentLink.Link;

    // decide what to do with the link / how to handle it
    var linkGroup = AsList(currentLink.Group as object).FirstOrDefault();
    bool hasLinkGroup = linkGroup != null;

    var mode = hasLinkGroup ? linkGroup.RedirectType : "default";
    string forward = hasLinkGroup ? linkGroup.ForwardHandler.ToString() : "";
    string forwardRetired = hasLinkGroup ? linkGroup.ForwardRetired.ToString() : "";

    // Add analytics params if they exist
    if(analyticsParams != null) {
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

  public bool IsQrCode(ref string key) {
    // In case it's empty, it could also be null, then errors would pop up
    key = key ?? "";
    if(key.EndsWith("$")) {
      key = key.TrimEnd(new char[] {'$'});
      return true;
    }
    return false;
  }

  public string ReplaceCI(string input, string search, string replacement){
    string result = Regex.Replace(
      input ?? "",
      Regex.Escape(search ?? ""),
      (replacement ?? "").Replace("$","$$"),
      RegexOptions.IgnoreCase
    );
    return result;
  }

  public string TargetUrlWithParams(string link, string linkParams){
    string paramPrefix = link.Contains("?") ? "&" : "?";
    string[] urlItems = {};

    if(link.Contains("#")) {
      urlItems = link.Split('#');
      return urlItems[0] + paramPrefix + linkParams + "#" + urlItems[1];
    }

    return link + paramPrefix + linkParams;
  }
}