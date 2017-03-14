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

public class FormController : SxcApiController
{
    [AllowAnonymous] 
	[HttpGet]
    [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Anonymous)]
    public void Go([FromUrl]string key)
    {

    }


}
