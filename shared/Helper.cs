using System;
using System.Linq;

public class Helper: Custom.Hybrid.Code12
{
  private string _qrPath;
  private string qrPathTemplate = "//api.qrserver.com/v1/create-qr-code/?color={foreground}&bgcolor={background}&qzone={qzone}&margin=0&size={dim}x{dim}&ecc={ecc}&data={link}";
  private string _qrSvgPath;
  
  string QrPath {
    get {
      if(_qrPath == null) {
        _qrPath = QrCodeGenerator(
          Settings.QrForegroundColor,
          Settings.QrBackgroundColor,
          Decimal.ToInt32(Settings.QrDimension),
          Settings.QrEcc,
          0,
          null);
      }
      return _qrPath;
    }
  }
  string QrSvgPath {
    get {
      if(_qrSvgPath == null)
        _qrSvgPath = QrCodeGenerator(
          Settings.QrSvgForeground,
          Settings.QrSvgBackground,
          Decimal.ToInt32(Settings.QrSvgDimension),
          Settings.QrSvgEcc,
          Decimal.ToInt32(Settings.QrSvgQuietZone),
          "svg");
      return _qrSvgPath;
    }
  }
  public string TestLink(string key, bool preprocess) {
    if(preprocess) {
      key = PreprocessUrl(key);
    }
    return Link.To(api: "api/Redirect/go", parameters: "key=" + key);
  }

  public string QrLink(string target) {
    target = PreprocessUrl(target);
    return QrPath.Replace("{link}", target);
  }

  public string QrSvgLink(string target) {
    target = PreprocessUrl(target);
    return Settings.EnablePro
      ? QrSvgPath.Replace("{link}", target)
      : "";
  }

  public string QrEpsLink(string target) {
    target = PreprocessUrl(target);
    return Settings.EnablePro
      ? QrSvgPath.Replace("=svg&", "=eps&").Replace("{link}", target)
      : "";
  }

  public string QrCodeGenerator(string fg, string bg, int size, string ecc, int qZone, string format) {
    var qrCode = qrPathTemplate
      .Replace("{qzone}", qZone.ToString())
      .Replace("{foreground}", fg.Replace("#", ""))
      .Replace("{background}", bg.Replace("#", ""))
      .Replace("{dim}", size.ToString())
      .Replace("{ecc}", ecc);
    if(format != null)
      qrCode = qrCode.Replace("&data=", "&format=" + format + "&data=");
    return qrCode;
  }

  // Special Pro feature
  // if pro is enabled, and UrlDifferentiation is activated
  // the url will be modified so the QR code has the same value, but in a way that allows it to be
  // detected as a QR key
  // It does this by converting everything to all caps (incl. http etc.) which allows for longer codes
  // It then adds a $ at the end to mark the QR use.
  // see also https://azing.org/2sxc/r/A7LozGPM
  public string PreprocessUrl(string target) {
    // don't do this unless pro features are enabled
    if(!Settings.EnableQrUseTracking)
      return target;

    // requires that original url key doesn't have upper-case
    if(target.ToLower() != target)
      return "Error: can't use CAP$ on this url because it contains upper case characters - which is bad practice";

    return target.ToUpper() + "$";
  }

  // this is the block in charge of creating unique, new url-keys which are not yet in use
  public string GenerateCode(string prefix, int length, bool lowerOnly) {
    int maxTries = 100; // only try this 100x to not crash
    var realLength = length - prefix.Length;
    var existingKeys = AsList(App.Data["Link"])
      .Select(l => l.Key)
      .Distinct()
      .ToDictionary(k => k, null);

    for(var attempt = 0; attempt < maxTries; attempt++) {
      var newKey = prefix + RandomString(realLength, lowerOnly);
      if(!existingKeys.ContainsKey(newKey))
        return newKey;
    }

    throw new Exception("tried too many attempts, didn't find a code, will abort'");
  }

  // funky, trivial randomizer
  // got basic idea from http://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings-in-c
  private static Random random = new Random();

  public static string RandomString(int length, bool lowerOnly) {
    const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    const string withUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" + chars;

    return new string(Enumerable.Repeat(lowerOnly ? chars : withUpper, length)
      .Select(s => s[random.Next(s.Length)]).ToArray());
  }
}