#pragma checksum "C:\Users\user\Documents\TJB\GroupPay\Views\Home\AllAgentManage.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "869749fb6bbce9af5ba59d7f357c0f0ca1d925c1"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Home_AllAgentManage), @"mvc.1.0.view", @"/Views/Home/AllAgentManage.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/Home/AllAgentManage.cshtml", typeof(AspNetCore.Views_Home_AllAgentManage))]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"869749fb6bbce9af5ba59d7f357c0f0ca1d925c1", @"/Views/Home/AllAgentManage.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"b6c72eadc28a85792f13ff93682befbea288effa", @"/Views/_ViewImports.cshtml")]
    public class Views_Home_AllAgentManage : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("name", "_ConsoleHeader", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("src", new global::Microsoft.AspNetCore.Html.HtmlString("~/js/RechargeReport.js"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
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
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.PartialTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_PartialTagHelper;
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "C:\Users\user\Documents\TJB\GroupPay\Views\Home\AllAgentManage.cshtml"
  
    ViewData["Title"] = "支付报表";

#line default
#line hidden
            BeginContext(40, 33, false);
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("partial", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "869749fb6bbce9af5ba59d7f357c0f0ca1d925c14044", async() => {
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_PartialTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.PartialTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_PartialTagHelper);
            __Microsoft_AspNetCore_Mvc_TagHelpers_PartialTagHelper.Name = (string)__tagHelperAttribute_0.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_0);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            EndContext();
            BeginContext(73, 3664, true);
            WriteLiteral(@"
<div class=""content"" id=""target""></div>

<script id=""ractiveTemplate"" type=""text/reactive"">
    <div class=""search-row"">
        <label>用户账号：</label><input type=""text"" name=""accountName"" id=""accountName"" value=""{{accountName}}"" />
        <label>ID查询：</label><input type=""text"" name=""accountID"" id=""accountID"" value=""{{accountID}}"" />
        <label>姓名查询：</label><input type=""text"" name=""accountFullName"" id=""accountFullName"" value=""{{accountFullName}}"" />
        <label>刷新：</label>
        <select value=""{{RefreshTime}}"" class=""widthauto"">
            <option value=""60000"">60秒</option>
            <option value=""120000"">120秒</option>
        </select>
    </div>
    <div class=""search-row"">
        <label>创建时间：</label>
        <input type=""button"" value=""本日查询"" on-click=""['setDateAndTime', 'Day']"" class=""setDateAndTime"" />
        <input type=""button"" value=""本周查询"" on-click=""['setDateAndTime', 'Week']"" class=""setDateAndTime"" />
        <input type=""button"" value=""本月查询"" on-click=""['setDateAndTime");
            WriteLiteral(@"', 'Month']"" class=""setDateAndTime"" />
        <br />
        <label>　　　　　</label>
        <input type=""text"" name=""startDate"" id=""startDate"" value=""{{startDate}}"" />
        <input type=""text"" name=""startTime"" id=""startTime"" value=""{{startTime}}"" />
        <label>至　</label>
        <input type=""text"" name=""endDate"" id=""endDate"" value=""{{endDate}}"" />
        <input type=""text"" name=""endTime"" id=""endTime"" value=""{{endTime}}"" />
        <input type=""submit"" name=""searchBtn"" id=""searchBtn"" value=""查找"" on-click=""doSearch"" />
    </div>
    <div style=""color:blue;padding:0 0 10px 0;"">{{message}}</div>
    <table class=""listTable bgwhite"">
        <thead>
            <th width=""27%""></th>
            <th width=""20%""></th>
            <th width=""23%"">总数</th>
            <th width=""14%"">已用点数</th>
            <th width=""16%"">可用点数</th>
        </thead>
        <tbody>
            {{#each Toprecords}}
            <tr>
                <td>{{accountID}}</td>
                <td>{{jobName}}</td>
  ");
            WriteLiteral(@"              <td>{{usedPoints + availablePoints}}</td>
                <td>{{usedPoints}}</td>
                <td>{{availablePoints }}</td>
            </tr>
            {{/each}}
        <tbody>
    </table>
    <br>
    <table class=""listTable"">
        <thead>
        <th width=""15%"">ID</th>
        <th width=""15%"">账号名称</th>
        <th width=""17%"">充值量</th>
        <th width=""9%"">汇率(%)</th>
        <th width=""14%"">佣金合计</th>
        <th width=""14%"">下发金额</th>
        <th width=""16%"">成功率(%)</th>
        </thead>
        <tbody>
            {{#each records}}
            <tr>
                <td>{{id}}</td>
                <td>{{accountName}}</td>
                <td>{{amount}}</td>
                <td>{{exchangeRate}}</td>
                <td>{{commission}}</td>
                <td></td>
                <td>{{successRate}}</td>
            </tr>
            {{/each}}
            {{#if total}}
            <tr class=""total"">
                <td>合计</td>
                <td></td>");
            WriteLiteral(@"
                <td>{{total.amount}}</td>
                <td>{{total.exchangeRate}}</td>
                <td>{{total.commission}}</td>
                <td></td>
                <td>{{total.successRate}}</td>
            </tr>
            {{/if}}
        <tbody>
    </table>
    <div class=""pager"">
        {{#each pages as p}}
        {{#if p == currentPage || p == -1}}
        <span>{{#if p == -1}}...{{else}}{{p}}{{/if}}</span>
        {{else}}
        <a href=""javascript:void(0);"" on-click=""doPage"">{{p}}</a>
        {{/if}}
        {{/each}}
    </div>
</script>
");
            EndContext();
            BeginContext(3737, 46, false);
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("script", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "869749fb6bbce9af5ba59d7f357c0f0ca1d925c19206", async() => {
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            EndContext();
            BeginContext(3783, 2, true);
            WriteLiteral("\r\n");
            EndContext();
            DefineSection("Scripts", async() => {
                BeginContext(3802, 102, true);
                WriteLiteral("\r\n    <script type=\"text/javascript\">\r\n        $(document).ready(initRechargeReport);\r\n    </script>\r\n");
                EndContext();
            }
            );
            BeginContext(3907, 721, true);
            WriteLiteral(@"
<style>
    body {
        font-family: Microsoft JhengHei !important;
    }
    .search-row .setDateAndTime {
        font-size: 13px;
        padding: 5px 10px;
        height: auto;
        background-color: white;
        border: 1px solid #008CBA;
        color: #008CBA;
    }
    .search-row .setDateAndTime:hover {
        color: white;
    }
    .widthauto {
        width:auto;    
    }
    .bgwhite th,.bgwhite td {
        background-color:white !important;
        border:1px solid #DDD;
    }
    input[type=""text""], input[type=""password""], select, input[type=""number""] {
        height: calc(2.25rem + 2px);
    }
    .listTable {
        word-break:break-all;
    }
</style>");
            EndContext();
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