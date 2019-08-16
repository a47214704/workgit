#pragma checksum "D:\asp\interview_test\interview_test\Views\Home\Background.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "a8b07df6ecfeec879c83a0640652cd7c9b975963"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Home_Background), @"mvc.1.0.view", @"/Views/Home/Background.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/Home/Background.cshtml", typeof(AspNetCore.Views_Home_Background))]
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
#line 1 "D:\asp\interview_test\interview_test\Views\_ViewImports.cshtml"
using interview_test;

#line default
#line hidden
#line 2 "D:\asp\interview_test\interview_test\Views\_ViewImports.cshtml"
using interview_test.Models;

#line default
#line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"a8b07df6ecfeec879c83a0640652cd7c9b975963", @"/Views/Home/Background.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"f611a4847064450c67af4483f8b135a0426a7555", @"/Views/_ViewImports.cshtml")]
    public class Views_Home_Background : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "D:\asp\interview_test\interview_test\Views\Home\Background.cshtml"
  
    ViewData["Title"] = "後台";

#line default
#line hidden
            BeginContext(38, 8318, true);
            WriteLiteral(@"<div class=""content"" id=""target""></div>
<script id=""ractiveTemplate"" type=""text/reactive"">
    {{message}}
    <div id=""interviewList"" style=""padding-top:100px"">
        <table class=""table table-hover"">
            <thead>
                <tr>
                    <th scope=""col"">#</th>
                    <th scope=""col"">姓名</th>
                    <th scope=""col"">作答時間/分</th>
                    <th scope=""col"">日期</th>
                    <th scope=""col"">狀態</th>
                    <th scope=""col"">備註</th>
                </tr>
            </thead>
            <tbody>
                {{#each data:i}}
                <tr on-click=""correct"">
                    <th scope=""row"">{{id}}</th>
                    <td>{{name}}</td>
                    <td>{{testtime}}/秒</td>
                    <td>{{timestamp}}</td>
                    <td>{{status == 0 ? '未改':'已改'}}</td>
                    <td>{{note}}</td>
                </tr>
                {{/each}}
            </tbody>
        </ta");
            WriteLiteral(@"ble>
    </div>

    <div id=""interviewEdit"" style=""padding-top:100px;display:none"">
        <div class=""alert alert-info"" role=""alert"">
            <h3 class=""display-3"">{{data[count].name}}</h3>
            <p class=""lead"">測驗耗時:{{data[count].testtime}}秒</p>
            <p class=""lead"">分數:{{data[count].score == ''? '尚未批改': data[count].score}}</p>
            <hr class=""my-4"">
            <p>狀態:{{data[count].status == 0 ? '尚未批改':'已改'}}</p>
            <p>作答日期:{{data[count].timestamp}}</p>
            <p>備註:{{data[count].note}}</p>
        </div>

        <div class=""panel panel-default"" style=""margin-bottom:20px;"">
            <div class=""panel-heading"">
                <h4 class=""panel-title"">
                    <button class=""btn btn-info"" data-toggle=""collapse"" href=""#choice"" on-click=""choicebar""> 選擇題 </button>
                    <button class=""btn btn-info"" data-toggle=""collapse"" href=""#short"" on-click=""shortbar""> 簡答題 </button>
                </h4>
            </div>
            <d");
            WriteLiteral(@"iv id=""choice"" class=""collapse"">
                {{#each testdata:i}}
                {{#if type == 1}}
                <div style=""padding:20px; background-color: #f0f0f0"">
                    <div class=""alert alert-success"">ANSWER:<br>{{answer}}</div>
                    <div class=""alert"" style=""background-color:#e8d1d1"">Interviewee:<br />{{interviewdata[i]}}</div>
                </div>
                {{/if}}
                {{/each}}
            </div>
            <div id=""short"" class=""collapse"">
                {{#each testdata:i}}
                {{#if type == 2}}
                <div style=""padding:20px; background-color: #f0f0f0"">
                    <pre>{{topic}}</pre>
                    <div class=""alert alert-success"">ANSWER:<br>{{answer}}</div>
                    <div class=""alert"" style=""background-color:#e8d1d1"">Interviewee:<br />{{interviewdata[i]}}</div>
                </div>
                {{/if}}
                {{/each}}
            </div>
        </div>
     ");
            WriteLiteral(@"   <label>輸入分數</label>
        <input type=""text"" class=""form-control"" aria-label=""Default"" aria-describedby=""inputGroup-sizing-default"" name=""score"" value=""{{upscore}}"">
        <label>備註</label>
        <input type=""text"" class=""form-control"" aria-label=""Default"" aria-describedby=""inputGroup-sizing-default"" name=""note"" value=""{{upnote}}"">
        <div style=""margin:20px 0px"">
            <button class=""btn btn-primary"" on-click=""back"">返回</button>
            <button class=""btn btn-danger"" data-toggle=""modal"" data-target=""#exampleModal"" on-click=""edit"">批改完成</button>
        </div>
    </div>

    <div class=""modal fade"" id=""exampleModal"" tabindex=""-1"" role=""dialog"" aria-labelledby=""exampleModalLabel"" aria-hidden=""true"">
        <div class=""modal-dialog"" role=""document"">
            <div class=""modal-content"">
                <div class=""modal-header"">
                    <h5 class=""modal-title"" id=""exampleModalLabel"">批改完成</h5>
                    <button type=""button"" class=""close"" data-dismis");
            WriteLiteral(@"s=""modal"" aria-label=""Close"">
                        <span aria-hidden=""true"">&times;</span>
                    </button>
                </div>
                <div class=""modal-body"">
                    <h4 class=""display-3"">{{data[count].name}}</h4>
                    <p>分數:{{upscore}}</p>
                    <p>備註:{{upnote}}</p>
                </div>
                <div class=""modal-footer"">
                    <button type=""button"" class=""btn btn-secondary"" data-dismiss=""modal"">Close</button>
                    <button type=""button"" class=""btn btn-primary"" on-click=""reload"">返回上一頁</button>
                </div>
            </div>
        </div>
    </div>
</script>

<script>
    var ractive = new Ractive({
        target: ""#target"",
        template: ""#ractiveTemplate"",
        data: {
            accountName: '',
            message: '',
            data: '',
            testdata: '',
            interviewdata: [],
            count: '',
            upscore: '',
    ");
            WriteLiteral(@"        upnote: ''
        },
        on: {
            correct: function (ctx) {
                var id = ctx.get(""id"");
                var interviewdata = this.get(""data"");
                this.set('count', id - 1);
                this.set('interviewdata', interviewdata[id - 1].answer.split("",""));
                console.log(interviewdata[id - 1].answer.split("",""));
                this.set('upscore', interviewdata[id - 1].score);
                this.set('upnote', interviewdata[id - 1].note);
                $.ajax({
                    url: '/api/InterviewTest',
                    type: 'GET',
                    context: this,
                    dataType: 'json'
                }).done(function (data) {
                    if (data == '') {
                        this.set('message', '資料出錯');
                    } else {
                        this.set('testdata', data);
                        this.set('message', '');
                    }
                }).fail(function (xh");
            WriteLiteral(@"r, status) {
                    this.set('message', '加载有誤:' + status);
                });
                $('#interviewList').hide();
                $('#interviewEdit').show();
            },
            back: function (ctx) {
                $('#interviewList').show();
                $('#interviewEdit').hide();
            },
            edit: function (ctx) {
                var upscore = this.get('upscore');
                var upnote = this.get('upnote');
                var count = this.get('count') + 1;
                var json = { score: upscore, note: upnote };
                $.ajax({
                    url: '/api/Background?count=' + count,
                    type: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    data: JSON.stringify(json),
                    context: this,
                    dataType: 'json'
                })
                .done(function (data) {

      ");
            WriteLiteral(@"          })
                .fail(function (xhr, status) {

                });

            },
            choicebar: function (ctx) {
                $('#short').collapse('hide');
            },
            shortbar: function (ctx) {
                $('#choice').collapse('hide');
            },
            reload: function (ctx) {
                location.reload();
            }
        },
        oncomplete: function () {
            this._loadData();
        },
        _loadData: function () {
            this.set('message', '載入中請稍等');
            $.ajax({
                url: '/api/Background',
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                if (data == '') {
                    this.set('message', '資料出錯' + data.error);
                } else {
                    this.set('data', data);
                    this.set('message', '');
                }
            }).fail(func");
            WriteLiteral("tion (xhr, status) {\r\n                this.set(\'message\', \'加载有誤:\' + status);\r\n            });\r\n        }\r\n    });\r\n</script>\r\n");
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
