#pragma checksum "C:\Users\user\Documents\TJB\GroupPay\Views\Pay\Bank.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "ba3a4f64df42cfdfb3affda18896c8cce38ac81d"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Pay_Bank), @"mvc.1.0.view", @"/Views/Pay/Bank.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/Pay/Bank.cshtml", typeof(AspNetCore.Views_Pay_Bank))]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"ba3a4f64df42cfdfb3affda18896c8cce38ac81d", @"/Views/Pay/Bank.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"b6c72eadc28a85792f13ff93682befbea288effa", @"/Views/_ViewImports.cshtml")]
    public class Views_Pay_Bank : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("class", new global::Microsoft.AspNetCore.Html.HtmlString("TJ_body"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.BodyTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_BodyTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "C:\Users\user\Documents\TJB\GroupPay\Views\Pay\Bank.cshtml"
  
    Layout = "_PayLayout";

#line default
#line hidden
            BeginContext(35, 4, true);
            WriteLiteral("\r\n\r\n");
            EndContext();
            BeginContext(39, 3022, false);
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("body", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "ba3a4f64df42cfdfb3affda18896c8cce38ac81d3591", async() => {
                BeginContext(61, 2951, true);
                WriteLiteral(@"
    <div class=""TJ-header"">
        <span>充值系统</span>
    </div>
    <div class=""content"" id=""target""></div>
    <script id=""ractiveTemplate"" type=""text/reactive"">
        <div id=""Loading"">
            <table width=""100%"" height=""100%"" border=""0"">
                <tr>
                    <td align=""center"" valign=""middle"">
                        {{#if !error}}
                        订单号：<span id=""pserno"">{{mrn}}</span><br>
                        充值金额：<span class=""notice-field"" id=""pprice"">{{amount}}</span><br>
                        <img id=""loadGif"" src=""/images/pay/loading.gif"" />
                        {{/if}}
                        <div class=""Loadingtxt"">{{message}}</div>
                    </td>
                </tr>
            </table>
        </div>
        <div id=""mainContent"" style=""display:none"">
            <div class=""TJ-contents"">
                <table border=""0"">
                    <tr>
                        <td rowspan=""3"" align=""center"" valign=""bottom"" w");
                WriteLiteral(@"idth=""40%"">充值金额<br />{{amount}}</td>
                        <td width=""60%"">{{bankName}}</td>
                    </tr>
                    <tr>
                        <td>{{accountName}}</td>
                    </tr>
                    <tr>
                        <td>{{bankCard}}</td>
                    </tr>
                </table>
            </div>
            <div class=""TJ-details"">
                <ul>
                    <li>订单号：{{mrn}}</li>
                    <li>支持　支付宝、微信转账</li>
                    <li>支持　手机银行大额转账　<red>秒到</red></li>
                    <li>
                        <div class=""copytxt"">
                            姓　名：<red>{{accountName}}</red>
                        </div>
                        <input type=""button"" value=""复制姓名"" class=""copybtn cpa"" data-clipboard-text=""{{accountName}}"" />
                    </li>
                    <li>
                        <div class=""copytxt"">
                            金　额：<red>{{amount}}</red>
           ");
                WriteLiteral(@"             </div>
                        <input type=""button"" value=""复制金额"" class=""copybtn cpb"" data-clipboard-text=""{{amount}}"" />
                    </li>
                    <li>
                        <div class=""copytxt"">
                            卡　号：<red>{{bankCard}}</red>
                        </div>
                        <input type=""button"" value=""复制卡号"" class=""copybtn cpc"" data-clipboard-text=""{{bankCard}}"" />
                    </li>
                    <li class=""dodgerblue"">卡号　姓名　金额　请点击复制　金额不对不会上分</li>
                    <li class=""dodgerblue""><red>重要提示</red>　以上银行帐户限本次使用，账户每次更换</li>
                    <li class=""dodgerblue"">如入款至过期帐号，无法查收，本公司恕不负责</li>
                    <li><red>充值成功后５分钟未到帐，请马上与平台客服联系</red></li>
                </ul>
            </div>
        </div>
        <input type=""button"" value=""返　回"" class=""TJ_back"" onclick=""window.location='");
                EndContext();
                BeginContext(3013, 19, false);
#line 71 "C:\Users\user\Documents\TJB\GroupPay\Views\Pay\Bank.cshtml"
                                                                              Write(ViewBag.CallBackUrl);

#line default
#line hidden
                EndContext();
                BeginContext(3032, 22, true);
                WriteLiteral("\'\" />\r\n    </script>\r\n");
                EndContext();
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_BodyTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.BodyTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_BodyTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            EndContext();
            BeginContext(3061, 4, true);
            WriteLiteral("\r\n\r\n");
            EndContext();
            DefineSection("Scripts", async() => {
                BeginContext(3082, 94, true);
                WriteLiteral("\r\n<script type=\"text/javascript\">\r\n    $(document).ready(function () {\r\n        initBankPage(\"");
                EndContext();
                BeginContext(3177, 27, false);
#line 78 "C:\Users\user\Documents\TJB\GroupPay\Views\Pay\Bank.cshtml"
                 Write(Html.Raw(@ViewBag.ErrorMsg));

#line default
#line hidden
                EndContext();
                BeginContext(3204, 25, true);
                WriteLiteral("\");\r\n    });\r\n</script>\r\n");
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
