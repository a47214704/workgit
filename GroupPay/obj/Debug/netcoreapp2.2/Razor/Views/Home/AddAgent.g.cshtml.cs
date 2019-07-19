#pragma checksum "C:\Users\user\Documents\TJB\GroupPay\Views\Home\AddAgent.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "440d6f7d8eb53d6f8bd200aebefb4d461f480e3f"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Home_AddAgent), @"mvc.1.0.view", @"/Views/Home/AddAgent.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/Home/AddAgent.cshtml", typeof(AspNetCore.Views_Home_AddAgent))]
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
#line 1 "C:\Users\user\Documents\TJB\GroupPay\Views\_ViewImports.cshtml"
using GroupPay;

#line default
#line hidden
#line 2 "C:\Users\user\Documents\TJB\GroupPay\Views\_ViewImports.cshtml"
using GroupPay.Models;

#line default
#line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"440d6f7d8eb53d6f8bd200aebefb4d461f480e3f", @"/Views/Home/AddAgent.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"b6c72eadc28a85792f13ff93682befbea288effa", @"/Views/_ViewImports.cshtml")]
    public class Views_Home_AddAgent : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "C:\Users\user\Documents\TJB\GroupPay\Views\Home\AddAgent.cshtml"
  
    ViewData["Title"] = "添加账号";

#line default
#line hidden
            BeginContext(40, 2511, true);
            WriteLiteral(@"<div class=""content"">
	<div id=""target"">
    </div>
    <script id=""ractiveTemplate"" type=""text/reactive"">
		<div style=""color:blue;padding:0 0 10px 0;"">{{message}}</div>
        <table class=""listTable colTable"">
            <tr>
                <td class=""title"">账号：</td>
                <td><input type=""text"" name=""account"" value=""{{account}}"" /></td>
            </tr>
            <tr>
                <td class=""title"">昵称：</td>
                <td><input type=""text"" name=""nickName"" value=""{{nickName}}"" /></td>
            </tr>
            <tr>
                <td class=""title"">密码：</td>
                <td><input type=""password"" name=""password"" value=""{{password}}"" /></td>
            </tr>
            <tr>
                <td class=""title"" style=""width:200px"">微信手续费(%)：</td>
                <td><input type=""number"" name=""wechatRatio"" value=""{{wechatRatio}}"" /></td>
            </tr>
            <tr>
                <td class=""title"" style=""width:200px"">支付宝手续费(%)：</td>
               ");
            WriteLiteral(@" <td><input type=""number"" name=""aliRatio"" value=""{{aliRatio}}"" /></td>
            </tr>
            <tr>
                <td class=""title"" style=""width:200px"">银行卡手续费(%)：</td>
                <td><input type=""number"" name=""bankRatio"" value=""{{bankRatio}}"" /></td>
            </tr>
            <tr>
                <td class=""title"" style=""width:200px"">开启支付通道：</td>
                <td>
                    <div class=""row"">
                        {{#each channel}}
                            {{#if enabled}}
                                <div class=""col-6""><input type=""checkbox"" name=""OpenChannel"" data-value=""{{id}}"">{{name}}</div>
                            {{else}}
                                <input type=""hidden"" name=""OpenChannel"" data-value=""{{id}}"">
                            {{/if}}
                        {{/each}}
                    </div>
                </td>
            </tr>
            <tr>
                <td class=""title"">验证码：</td>
                <td>
             ");
            WriteLiteral(@"       <input type=""text"" name=""captcha"" value=""{{captcha}}"" style=""width:190px"" autocomplete=""off"" />
                    <img src=""/Home/CaptchaImage"" id=""inputCaptchaImg"" on-click=""refreshCaptcha"" style=""width:100px; vertical-align:middle; height:33px;"" />
                </td>
            </tr>
        </table>
		<div style=""text-align:center; padding-top:15px;"">
			<input type=""button"" value=""确定"" on-click=""saveUser"">
		</div>
	</script>
</div>
");
            EndContext();
            DefineSection("Scripts", async() => {
                BeginContext(2568, 76, true);
                WriteLiteral("\r\n<script type=\"text/javascript\">$(document).ready(initAddAgent);</script>\r\n");
                EndContext();
            }
            );
        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591