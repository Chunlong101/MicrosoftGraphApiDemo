using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using AppModelv2_WebApp_OpenIDConnect_DotNet.Core;
using Newtonsoft.Json;
using System.IO;
using NLog;

namespace AppModelv2_WebApp_OpenIDConnect_DotNet
{
    public partial class Index : System.Web.UI.Page
    {
        private static string AuthorizationCode;
        private static Logger log = LogManager.GetLogger("Index");

        protected void Page_Load(object sender, EventArgs e)
        {
            // Allow anonymous or not 
            //if (!Request.IsAuthenticated)
            //{
            //    Response.Redirect("~/Home/SignIn");
            //}
            //else
            //{
            //    currentUser = HttpContext.Current.User;
            //}

            try
            {
                if (!IsPostBack && User.Identity.IsAuthenticated)
                {
                    var claims = ClaimsPrincipal.Current.Identities.First().Claims.ToList();
                    string username = claims?.FirstOrDefault(x => x.Type.Equals("name", StringComparison.OrdinalIgnoreCase))?.Value;
                    SignIn.Text = "Hi " + username;

                    log.Info("Current user is authenticated thru Azure AD OpenId Connect, username : ", username);

                    SignIn.Enabled = false;
                    SignIn.BorderStyle = BorderStyle.None;
                    SignIn.ForeColor = System.Drawing.Color.Black;
                }

                if (!IsPostBack && !string.IsNullOrEmpty(Request.QueryString["code"]) && Request.UrlReferrer != null && Request.UrlReferrer.AbsoluteUri == "https://login.microsoftonline.com/")
                {
                    log.Info("Current request is coming from https://login.microsoftonline.com, should be the authorization code callback");
                    AuthorizationCode = Request.QueryString["code"];
                    Label1.Text = "You now have the authorization code, click Button 1 again to get access token and upload the file selected";
                    this.ClientScript.RegisterStartupScript(this.GetType(),
                                            "navigate",
                                            "window.location.hash='#header2-f';",
                                            true);
                }

                if (!IsPostBack && !string.IsNullOrEmpty(AuthorizationCode))
                {
                    if (Request.UrlReferrer == null || (Request.UrlReferrer != null && Request.UrlReferrer.AbsoluteUri != "https://login.microsoftonline.com/"))
                    {
                        log.Info("Current request looks like a page refresh or something similar");
                        AuthorizationCode = null;
                    }
                }

            }
            catch (Exception ex)
            {
                log.Fatal("Fatal error occurred pls check the log file for more details : {0}", ex.Message);
                log.Fatal(ex.StackTrace);
            }
        }

        protected void SignIn_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Home/SignIn");
        }

        protected void Upload1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(AuthorizationCode))
            {
                log.Info("Currently there's no authorization code, redirecting to azure to get one");
                Response.Redirect("https://login.microsoftonline.com/9da0ca7c-edef-4857-b7f1-df1bbdacefd3/oauth2/v2.0/authorize?response_type=code&response_mode=query&scope=openid profile Sites.ReadWrite.All offline_access&state=12345&client_id=4f22d685-5d26-4d45-8768-f6be4fb1efac&redirect_uri=https://localhost:44368/Index.aspx&prompt=consent&code_challenge=dRioQKjhxyGm7qBj-FwKFBPgdv3_GprNK7y5erWa_J8");
            }

            try
            {
                if (!FileUpload1.HasFile)
                {
                    Label1.Text = "Pls choose a file to upload";
                    return;
                }

                log.Info("Now trying to get your access token...");
                var client = new RestClient("https://login.microsoftonline.com/9da0ca7c-edef-4857-b7f1-df1bbdacefd3/oauth2/v2.0/token");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("client_id", "4f22d685-5d26-4d45-8768-f6be4fb1efac");
                request.AddParameter("scope", "openid profile Sites.ReadWrite.All offline_access");
                request.AddParameter("redirect_uri", "https://localhost:44368/Index.aspx");
                request.AddParameter("grant_type", "authorization_code");
                request.AddParameter("client_secret", "6Cz7Q~FfWbh9S~-p1AD~oMFI2AYT1cN1JPqsA");
                request.AddParameter("code", AuthorizationCode);
                request.AddParameter("code_verifier", "dRioQKjhxyGm7qBj-FwKFBPgdv3_GprNK7y5erWa_J8");
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    log.Info("Access token has been retrived successfully, response content : {0}", response.Content);
                }
                else
                {
                    log.Error("Failed to get access token, error msg : {0}" + response.ErrorMessage);
                }

                var tokens = JsonConvert.DeserializeObject<TokenResponse>(response.Content);

                string accessToken = tokens.access_token;

                log.Info("Uploading the file...");

                using (Stream fs = FileUpload1.PostedFile.InputStream)
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        byte[] bytes = br.ReadBytes((Int32)fs.Length);

                        client = new RestClient("https://graph.microsoft.com/v1.0/drives/b!ruuaAvmW5kyqpO0TSSQ1XP9CZkK4ul9ErcpIHp779HqDjzc8qdp0T7ggZc5tccC9/root:/FolderA/FolderB/FileC.txt:/content");
                        client.Timeout = -1;
                        var upload = new RestRequest(Method.PUT);
                        upload.AddHeader("Authorization", "Bearer " + accessToken);
                        upload.AddParameter("text/plain", bytes, ParameterType.RequestBody);
                        IRestResponse uploadResponse = client.Execute(upload);

                        if (uploadResponse.IsSuccessful)
                        {
                            log.Info("File has been uploaded successfully, pls check https://chunlong.sharepoint.com/Shared%20Documents/Forms/AllItems.aspx");
                            Label1.Text = "File has been uploaded successfully, pls check https://chunlong.sharepoint.com/Shared%20Documents/Forms/AllItems.aspx";
                        }
                        else
                        {
                            log.Error("Failed to upload the file, error msg : {0}", uploadResponse.ErrorMessage);
                            Label1.Text = string.Format("Failed to upload the file, error msg : {0}", uploadResponse.ErrorMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Fatal("Fatal error occurred pls check the log file for more details : {0}", ex.Message);
                log.Fatal(ex.StackTrace);
            }
        }

        protected void Upload2_Click(object sender, EventArgs e)
        {

        }
    }
}