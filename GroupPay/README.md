# Group Pay

## 安装说明

### 项目依赖
1. MySql数据库5.7以上
2. Java 1.8以上
3. 二维码转码服务

#### 二维码转码服务
1. 项目地址 https://dev.azure.com/tmuccadam/pacific/_git/qrcodesvc
2. 私有Maven库配置在setup/settings.xml里面
3. 使用`mvn package` 构建程序包
4. 在target文件夹下创建配置文件**config.json**，如下：
```
{
  "outputFolder": "www/images",
  "resourceRoot": "www/"
}
```
> *outputFolder* 是重新编码以后的二维码图片文件夹，必须是在**resourceRoot**下面

> *resourceRoot* 是网站的资源文件夹，如果不存在的话需要手动创建
5. 运行方式 `java -jar qrcodesvc-0.0.1-jar-with-dependencies.jar config.json`，该服务监听8080端口。
6. ...表示返回的是数组对象

### 数据库初始化
初始化脚本文件 `dbinit.sql`

## API 文档

### 约定
1. 方括号[]内的参数表示可选参数；
2. 尖括号<>内的参数表示必选参数；
3. API返回一般格式为
```
{
  "error": "error_code",
  "errorMessage": "错误信息",
  "traceActivity": "错误日志跟踪ID",
  "result": <<具体API返回的结果对象>>
}
```
4. error_code列表
* > **success** API调用成功，对应response code 200-299
* > **invalid_data** 无效的请求数据，对应response code 400
* > **object_not_found** 所请求的对象不存在，对应response code 404
* > **dependency_failed** API所依赖的第三方服务调用失败，对应response code 500
* > **object_conflict** 所请求的对象与现有的对象冲突，对应response code 409
* > **more_data** 需要更多的数据完成请求对象（具体API会具体说明需要什么数据），对应response code 200-299
* > **service_not_ready** 系统服务还没有就绪，可以重试，对应response code 503
* > **invalid_credentials** 无效的用户令牌或者非法的用户信息，对应response code 401或者400
* > **password_change_required** 需要修改密码（发生在登录API上），对应response code 200-299
* > **not_supported** API不支持所请求的操作，对应response code 501
* > **unknwn** 未知系统错误，对应response code 500

### 获取验证码图片
```
GET /CaptchaImage?token=[token]
```
API返回PNG二进制图片数据。
> **token**是可选参数，值由客户端传入，必须是唯一标识，用来关联验证码图片和验证码，登录和注册的时候需要将*token*传入。
>  如果*token*为空，那么服务器将会把验证码和用户会话信息通过cookie关联。

### 用户注册
```
POST /api/User?token=[token]&captcha=<captcha>&referrer=[referrer]

Content-Type: application/json
{
  "accountName": "手机号码",
  "password": "密码"
}
```
API对象
```
Content-Type: application/json
{
  "id": 用户ID,
  "accountName": "手机号码"
}
```
如果返回的error_code为more_data则需要完善用户的密码保护问题和答案，具体参考相应的API说明。

### 用户名检测
```
HEAD /api/User?account=<手机号码>&token=[token]&captcha=<captcha>
```
返回
* > 400 无效请求
* > 404 用户名不存在
* > 200 用户已经存在

### 用户登录
```
POST /api/User/Login?token=[token]&captcha=<captcha>

Content-Type: application/json
{
  "accountName": "手机号",
  "password": "密码"
}
```
返回对象
```
{
  "userId": UserID,
  "token": "用户令牌，用于访问用户其他信息的令牌"
}
```

### 获取用户账户信息
```
GET /api/User/<id>

Authentication UserToken <用户令牌>
```
返回对象
```
{
    "error": "success",
    "errorMessage": null,
    "traceActivity": null,
    "result": {
        "id": UserID,
        "role": {
            "id": RoleID
        },
        "accountName": "13345670894",
        "avatar": null,
        "nickName": null,
        "balance": 999998,
        "email": null,
        "phone": null,
        "promotionCode": "7AMAAAAAAAA",
        "status": 1,
        "hasSubAccounts": false,
        "createTimestamp": 0,
        "passwordLastSet": 0,
        "createTime": "1970-01-01T00:00:00",
        "passwordLastSetTime": "1970-01-01T00:00:00"
    }
}
```

### 获取用户密保问题列表
```
GET /api/SecurityQuestion
```
返回对象
```
[
  {
    "id": ID1,
    "question": "问题1"
  },
  {
    "id": ID2,
    "question": "问题2"
  },
  ...
]
```

### 保存用户密保答案
```
POST /api/User/<id>/SecurityAnswers

Authentication: UserToken <用户令牌>
Content-Type: application/json
[
  {
    "question": {
      "id": 问题ID1
    },
    "answer": "答案1"
  },
  {
    "question": {
      "id": 问题ID2
    },
    "answer": "答案2"
  },
  ...
]
```
返回对象：
```
{
  "accountName":"手机号"
}
```

### 更新用户信息
```
PUT /api/User/<id>

Authentication: UserToken <用户令牌>
Content-Type: application/json
{
  "nickName": "昵称",
  "email": "Email",
  "phone": "电话号码"
}
```
返回对象：
```
{
  "accountName":"手机号"
}
```
### 更改用户密码
```
POST /api/User/<id>/UpdatePassword

Authentication: UserToken <用户令牌>
Content-Type: application/json
{
  "oldPassword": "现有密码",
  "newPassword": "新密码"
}
```
返回对象：
```
{
  "accountName":"手机号"
}
```

### 获取支持的收款通道
```
GET /api/CollectChannel

Authentication: UserToken <用户令牌>
```
返回对象
```
[
  {
    "id": ID,
    "name": "名字",
    "dailyLimit": "每日限额",
    "expiryLimit": "有效期(天数)",
    "type": 1 - 二维码, 2 - 账号
  },
  ...
]
```

### 获取现有收款方式
```
GET /api/CollectInstrument

Authentication: UserToken <用户令牌>
```
返回对象:
```
[
  {
    "id": ID
    "name": "名称",
    "status": 状态 1-pending, 2-active,
    "channel":{
      "id": 通道ID,
      "name": "通道名称"
    },
    qrCode: "二维码地址，如果有的话",
    "accountProvider": "银行名称",
    "accountHolder": "账户名称",
    "accountName": "银行账号"
  },
  ...
]
```

### 用表单创建收款方式
```
POST /api/CollectInstrument/Create

Authentication: UserToken <用户令牌>
Content-Type: form-data/multipart
--------
Name=名称
--------
ChannelId=通道ID
--------
QrCodeFile
Content-Type:image/png
FileName:file.png

base64 encoded data
--------
BankName=银行名称
--------
AccountNumber=银行账号
--------
AccountName=账号名字
```
返回对象:
```
{
  "id": ID
  "name": "名称",
  "status": 状态 1-pending, 2-active,
  "channel":{
    "id": 通道ID,
    "name": "通道名称"
  },
  qrCode: "二维码地址，如果有的话",
  "accountProvider": "银行名称",
  "accountHolder": "账户名称",
  "accountName": "银行账号"
}
```

### JSON创建收款方式
```
POST /api/CollectInstrument

Authentication: UserToken <用户令牌>
Content-Type: application/json
{
  "name": "名称",
  "channel":{
    "id": 通道ID,
  },
  "originalQrCode": "<文件扩展名>.<文件内容的base64编码>",
  "accountProvider": "银行名称",
  "accountHolder": "账户名称",
  "accountName": "银行账号"
}
```
返回对象:
```
{
  "id": ID
  "name": "名称",
  "status": 状态 1-pending, 2-active,
  "channel":{
    "id": 通道ID,
    "name": "通道名称"
  },
  qrCode: "二维码地址，如果有的话",
  "accountProvider": "银行名称",
  "accountHolder": "账户名称",
  "accountName": "银行账号"
}
```

### 删除收款方式
```
DELETE /api/CollectInstrument/<id>
Authentication: UserToken <用户令牌>
```
如果成功，返回 response code 204，没有内容

### 获取支付订单
```
GET /api/Payment?status=<status>

Authentication: UserToken <用户令牌>
```
> **status** 1 - 等待中， 2 - 已接受， 3 - 已付款， 4 - 已清算， 5 - 已取消

返回对象:
```
[
  {
    "id": ID,
    "channel": "通道名称",
    "amount": "金额，整数，精确到分",
    "expiration": settle过期时间，毫秒数
  },
  ...
]
```

### 确认付款
```
POST /api/Payment/<id>/Settle

Authentication: UserToken <用户令牌>
```
返回对象:
```
{
  "id": ID,
  "channel": 通道ID,
  "instrument": {
    "id": ID,
    "userId": UserID
  },
  "amount": 金额,
  "merchantReferenceNumber": "商户参考编号",
  "status": 状态，参考订单信息
}
```

## 通知通道规范

### Web Socket地址
```
/ws/paysrv?_token=<用户令牌>
```

### 约定
* 通知/请求/响应 消息格式:
```
{
  "operation": "操作名称",
  "error": "错误编码，参考error_code",
  "content": <<具体操作返回的结果对象>>
}
```
* 操作名称
1. > unknown 未知操作，系统错误
2. > payment_notification 有支付订单可以接受
3. > list_payment 列出可接受的所有支付订单
4. > accept_payment 接受支付订单
5. > notify_to_settle 提示结算订单
6. > remove_payment 订单已经被接受，需要从备选列表中删除

### 列出所有可接受订单
```
{
  "operation":"list_payment"
}
```
返回对象
```
[
  {
    "id": ID,
    "channel": "通道名称",
    "amount": "金额，整数，精确到分"
  },
  ...
]
```

### 接受支付订单
```
{
  "operation": "accept_payment",
  "content": {
    "id": 支付订单ID
  }
}
```
返回对象
```
{
  "id": ID,
  "channel": "通道名称",
  "amount": "金额，整数，精确到分"
}
```

## 商户API

### 支付请求
```
POST /api/Payment

x-app-key:<商户的AppKey>
x-signature:<请求签名>
Content-Type:application/x-www-form-urlencoded

Amount=<金额精确到分>&Channel=<通道ID>&MerchantReferenceNumber=<商户参考号>&NotifyUrl=<回调地址>
```
注意事项
1. > *Amount*是整数类型，100为1元
2. > *Channel* 1 - 微信, 2 - 支付宝, 3 - 银行卡
3. > *MerchantReferenceNumber* 可以是商户的订单号，商户用来查找本地订单
4. > *NotifyUrl* 商户的回调地址，回调API参考回调文档

请求签名格式
```
md5("Amount=<amount>&Channel=<channelId>&Key=<key>&MerchantReferenceNumber=<mrn>&NotifyUrl<notifyUrl>")
```
其中**key**是商户的密钥

返回对象(参考API返回规格)
```
{
  "refNumber": "参考编号",
  "qrCodeUrl": "支付二维码地址",
  "bankName": "银行名称(只有银行卡通道有效)",
  "accountName": "账户名称(只有银行卡通道有效)",
  "accountNumber": "银行账号(只有银行卡通道有效)"
}
```

### 支付回调
```
POST <notify-url>

Content-Type: application/x-www-form-urlencoded

Amount=<amount>&MerchantReferenceNumber=<mrn>&Sig=<签名>
```
签名格式
```
md5("Amount=<amount>&MerchantReferenceNumber=<mrn>&Key=<key>")
```
其中**key**是商户的密钥

如果商户返回200，则视为回调成功，否则将在2秒后重试，再次失败后，过4秒重试，再失败则不再回调

### 提示放币API
```
GET /api/Payment/<RefNumber>/Notify

x-app-key:<商户的AppKey>
x-signature:<请求签名>
```

请求签名格式
```
md5("Key=<key>&RefNumber=<RefNumber>")
```
其中**key**是商户的密钥，通知成功则返回201

### 获取团队信息
```
GET /api/User/<id>/Team

x-rainier-ticket: <用户令牌>
```
返回对象
```
{
    "total": 1,
    "directMembers": {
        "total": 1,
        "thisWeekIncrement": 1,
        "thisMonthIncrement": 1
    },
    "directAgents": {
        "total": 0,
        "thisWeekIncrement": 0,
        "thisMonthIncrement": 0
    }
}
```

### 获取直属代理
```
GET /api/User/<id>/Agents?page=[第几页]&pageToken=[上一次请求拿到的pageToken]

x-rainier-ticket: <用户令牌>
```
返回对象
```
{
    "totalPages": 1,
    "page": 0,
    "records": [
        {
            "id": 1003,
            "tenantId": 0,
            "accountName": "13345670892",
            "balance": 0,
            "savingBalance": 0,
            "promotionCode": "6wMAAAAAAAA",
            "gameId": 0,
            "status": 0,
            "hasSubAccounts": false,
            "createTimestamp": 0,
            "passwordLastSet": 0,
            "lastLoginTimestamp": 0,
            "createTime": "1970-01-01T00:00:00Z",
            "passwordLastSetTime": "1970-01-01T00:00:00Z",
            "lastLoginTime": "1970-01-01T00:00:00Z",
            "userName": "1003",
            "name": "13345670892",
            "roles": []
        }
    ],
    "pageToken": "0,1003"
}
```

### 获取直属会员
```
GET /api/User/<id>/Members?page=[第几页]&pageToken=[上一次请求拿到的pageToken]

x-rainier-ticket: <用户令牌>
```
返回对象
```
{
    "totalPages": 1,
    "page": 0,
    "records": [
        {
            "id": 1003,
            "tenantId": 0,
            "accountName": "13345670892",
            "balance": 0,
            "savingBalance": 0,
            "promotionCode": "6wMAAAAAAAA",
            "gameId": 0,
            "status": 0,
            "hasSubAccounts": false,
            "createTimestamp": 0,
            "passwordLastSet": 0,
            "lastLoginTimestamp": 0,
            "createTime": "1970-01-01T00:00:00Z",
            "passwordLastSetTime": "1970-01-01T00:00:00Z",
            "lastLoginTime": "1970-01-01T00:00:00Z",
            "userName": "1003",
            "name": "13345670892",
            "roles": []
        }
    ],
    "pageToken": "0,1003"
}
```

### 获取业绩信息
```
GET /api/User/<id>/Revenue

x-rainier-ticket: <用户令牌>
```
返回对象
```
{
    "totalRevenue": 0,
    "memberRevenue": 0,
    "agentRevenue": 0,
    "myCommission": 0,
    "totalComission": 0,
    "agencyCommission": 0
}
```

### 获取可提现佣金
```
GET /api/User/<id>/CommissionBalance

x-rainier-ticket: <用户令牌>
```
返回对象
```
{
    "userId": <userId>,
    "commission": 佣金
}
```
* > 如果返回 HTTP 202则说明上一周的佣金正在结算当中，应该给个按钮让用户过10秒钟后继续调用该API获取结算出来的数据

### 获取佣金提现记录
```
GET /api/User/<id>/CommissionCashRecords

x-rainier-ticket: <用户令牌>
```
返回对象
```
[
  {
    "userId": <userId>,
    "week": 20190128,
    "commission": 1000.00,
    "cashed": true, (已经提现)
    "cash_time": 提现时间1970/1/1至今的毫秒数
  }
]
```

### 佣金提现
```
POST /api/User/<id>/CashCommission

x-rainier-ticket: <用户令牌>
```
返回对象
{
  "available": 用户新的可用余额
}
可能的错误
* > 404 用户不存在
* > 400 错误代码: invalid_data 没有可提现的佣金