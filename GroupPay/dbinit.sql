create table `security_question`(
    `id` int not null auto_increment primary key,
    `question` varchar(200) not null
) engine=InnoDB default charset=utf8mb4;

create table `service_account` (
    `id` int not null primary key,
    `category` varchar(20) not null,
    `name` varchar(40) not null,
    `pri_hash` varchar(128) not null,
    `sec_hash` varchar(128) not null,
    `endpoint` varchar(200) not null
) engine=InnoDB default charset=utf8mb4;

create table `service_instance` (
    `id` int not null auto_increment primary key,
    `service_id` int not null,
    `cluster` varchar(20) not null,
    `server` varchar(64) not null,
    `endpoint` varchar(200) not null,
    index `idx_instance_service_id` (`service_id`)
) engine=InnoDB default charset=utf8mb4;

create table `user_role` (
    `id` int not null auto_increment primary key,
    `name` varchar(40) not null,
    `memo` varchar(100)
) engine=InnoDB default charset=utf8mb4 auto_increment=100;

create table `role_perm` (
    `role_id` int not null,
    `perm` varchar(40) not null,
    constraint `pk_role_perm` primary key (`role_id`, `perm`)
) engine=InnoDB default charset=utf8mb4;

create table `user_account` (
    `id` bigint not null auto_increment primary key,
    `role_id` int not null,
    `account_name` varchar(20) not null,
    `avatar` varchar(200),
    `balance` double not null default 0,
	`evaluation_point` int default 50,
	`pending_balance` int default 0,
	`pending_payments` int default 0,
    `email` varchar(100),
    `nick_name` varchar(50),
    `merchant_id` int(11) default 0,
    `password` varchar(128) not null,
    `status` int not null default -1,
    `online` bit not null default 0,
	`wechat_account` varchar(20) not null,
    `has_sub_account` bit not null default 0,
    `create_time` bigint not null,
    `password_last_set` bigint not null default 0,
    `last_login_timestamp` bigint not null default 0,
    index `idx_user_account_name` (`account_name`),
    index `idx_user_role_id` (`role_id`)
) engine=InnoDB default charset=utf8mb4 auto_increment=1000;

create table `security_answer` (
    `id` bigint not null auto_increment primary key,
    `user_id` bigint not null,
    `question_id` int not null,
    `answer` varchar(200) not null,
    index `idx_answer_user_q_id` (`user_id`, `question_id`)
) engine=InnoDB default charset=utf8mb4;

create table `login_log` (
    `id` bigint not null auto_increment primary key,
    `user_id` bigint not null,
    `timestamp` bigint not null,
    `browser` varchar(200) not null,
    `ip` varchar(100) not null,
    index `idx_login_log_user` (`user_id`),
    index `idx_login_timestamp` (`timestamp`)
) engine=InnoDB default charset=utf8mb4;

create table `user_relation` (
    `user_id` bigint not null,
    `upper_level_id` bigint not null,
    `is_direct` bit not null default 0,
    constraint `pk_user_relation` primary key (`user_id`, `upper_level_id`),
    index `idx_user_relation_upper_id`(`upper_level_id`)
) engine=InnoDB default charset=utf8mb4;

create table `collect_channel` (
    `id` int not null auto_increment primary key,
    `name` varchar(40) not null,
    `type` int not null,
	`provider` int not null,
	`ratio` int default 5,
	`enabled` bit default 1,
	`instruments_limit` int default 5,
	`default_daliy_limit` int DEFAULT '10000',
	`valid_time` int DEFAULT '10'
) engine=InnoDB default charset=utf8mb4;

create table `collect_instrument` (
    `id` bigint not null auto_increment primary key,
    `name` varchar(40) not null,
    `status` int not null,
    `user_id` bigint not null,
    `channel_id` int not null,
    `token` varchar(200) null,
    `original_qr_code` varchar(200) null,
    `qr_code` varchar(200) null,
    `account_provider` varchar(200) null,
    `account_holder` varchar(60) null,
    `account_name` varchar(100) null,
	`daily_limit` int default 0,
	`expiry_limit` int default 0,
    index `idx_user_id` (`user_id`)
) engine=InnoDB default charset=utf8mb4 auto_increment=1000;

drop table if exists `merchant`
create table `merchant` (
    `id` int not null auto_increment primary key,
    `name` varchar(100) not null,
    `app_key` varchar(128) not null,
    `app_pwd` varchar(128) not null,
	`user_id` bigint not null,
	`wechat_ratio` double,
    `ali_ratio` double,
	`bank_ratio` double,
	`channel_enabled` int(11),
	`user_id` bigint(20),
	`channel_limit` text,
	`wechat_ratio_static` double,
	`ali_ratio_static` double,
	`bank_ratio_static` double,
	index `idx_merchant_app_key` (`app_key`)
) engine=InnoDB default charset=utf8mb4 auto_increment=1000000;

create table `payment` (
    `id` bigint not null auto_increment primary key,
    `channel` int not null,
    `ciid` bigint null,
    `merchant_id` int not null,
    `mrn` varchar(64) not null,
    `amount` int not null,
    `notify_url` varchar(200) not null,
    `status` int not null,
    `create_time` bigint not null,
    `accept_time` bigint,
    `settle_time` bigint,
	`callback_url` varchar(128),
	`origin_amount` int(11),
	`ratio` double,
	unique index `idx_payment_merchant_mrn` (`merchant_id`,`mrn`),
    index `idx_payment_ciid` (`ciid`),
    index `idx_payment_merchant` (`merchant_id`),
    index `idx_payment_mrn` (`mrn`),
    index `idx_payment_accept_time` (`accept_time`)
) engine=InnoDB default charset=utf8mb4 auto_increment=1000;

create table `transaction_log` (
    `id` bigint not null auto_increment primary key,
    `type` int not null,
    `payment_id` bigint not null,
    `operator_id` bigint not null default 0,
    `user_id` bigint not null,
    `amount` int not null,
    `balance_before` double not null,
    `balance_after` double not null,
    `time` bigint not null,
	`amountFrom` bigint,
    index `idx_txn_log_uid` (`user_id`),
    index `idx_txn_log_time` (`time`)
) engine=InnoDB default charset=utf8mb4;

drop table if exists `commission_ratio`;
create table `commission_ratio` (
    `lbound` int not null,
    `ubound` int not null,
    `ratio` double not null,
    constraint `pk_commission_ratio` primary key (`lbound`, `ubound`)
) engine=InnoDB default charset=utf8mb4;

drop table if exists `agency_commission`;
create table `agency_commission` (
    `user_id` bigint not null,
    `week`  int not null,
    `revenue` double not null,
    `commission` double not null,
    `cashed` bit not null default 0,
    `cash_time` bigint null,
	`type` int not null default 1;
    constraint `pk_agency_commission` primary key (`week`, `user_id`),
    index `idx_agency_commission_uid` (`user_id`)
) engine=InnoDB default charset=utf8mb4;

drop table if exists `site_config`;
create table `site_config` (
    `id` int not null auto_increment primary key,
    `name` varchar(100) not null,
    `display_name` varchar(100) not null,
    `value` varchar(255) not null,
	unique key `name` (`name`)
) engine=InnoDB default charset=utf8mb4;

insert into `collect_channel`(`name`, `type`, `provider`, `ratio`) values('微信', 1, 1, 7);
insert into `collect_channel`(`name`, `type`, `provider`, `ratio`) values('支付宝', 1, 2, 6);
insert into `collect_channel`(`name`, `type`, `provider`, `ratio`) values('银行卡', 2, 3, 5);
insert into `collect_channel`(`name`, `type`, `provider`, `ratio`) values('支付宝红包', 3, 2, 6);
insert into `collect_channel`(`name`, `type`, `provider`, `ratio`) values('云闪付', 4, 3, 5);

insert into merchant(`name`, `app_key`, `app_pwd`) values('测试商户', '1073ab6cda4b991cd29f9e83a307f34004ae9327', '00cafd126182e8a9e7c01bb2f0dfd00496be724f');

insert into security_question(`question`) values('你父亲是谁?');
insert into security_question(`question`) values('你最要好的同学是谁?');
insert into security_question(`question`) values('你家住在哪里?');

insert into user_role(`id`, `name`, `memo`) values(100, 'Member', '普通会员');
insert into role_perm(`role_id`, `perm`) values(100, 'InstrumentOwner');
insert into role_perm(`role_id`, `perm`) values(100, 'SelfMgmt');

insert into user_role(`id`, `name`, `memo`) values(101, 'sa', '管理员');
insert into role_perm(`role_id`, `perm`) values(101, 'ConfigWriter');
insert into role_perm(`role_id`, `perm`) values(101, 'ConfigReader');
insert into role_perm(`role_id`, `perm`) values(101, 'ChannelReader');
insert into role_perm(`role_id`, `perm`) values(101, 'ChannelWriter');
insert into role_perm(`role_id`, `perm`) values(101, 'InstrumentReader');
insert into role_perm(`role_id`, `perm`) values(101, 'InstrumentWriter');
insert into role_perm(`role_id`, `perm`) values(101, 'LogReader');
insert into role_perm(`role_id`, `perm`) values(101, 'LogWriter');
insert into role_perm(`role_id`, `perm`) values(101, 'MerchantReader');
insert into role_perm(`role_id`, `perm`) values(101, 'MerchantWriter');
insert into role_perm(`role_id`, `perm`) values(101, 'PaymentReader');
insert into role_perm(`role_id`, `perm`) values(101, 'PaymentWriter');
insert into role_perm(`role_id`, `perm`) values(101, 'UserReader');
insert into role_perm(`role_id`, `perm`) values(101, 'UserWriter');
insert into role_perm(`role_id`, `perm`) values(101, 'CredMgr');
insert into role_perm(`role_id`, `perm`) values(101, 'BalanceMgr');
insert into role_perm(`role_id`, `perm`) values(101, 'SelfMgmt');
insert into role_perm(`role_id`, `perm`) values(101, 'UserMgmt');
insert into role_perm(`role_id`, `perm`) VALUES(101, 'ReportReader');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(101, 'RechargeReader');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(101, 'AgentMgmt');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(101, 'AgentSettler');

INSERT INTO user_role (`id`, `name`, `memo`) VALUES (102, 'Operations', '运维');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'BalanceMgr');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'ChannelReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'ChannelWriter');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'ConfigReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'CredMgr');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'InstrumentReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'InstrumentWriter');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'LogReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'LogWriter');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'MerchantReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'MerchantWriter');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'PaymentReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'PaymentWriter');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'SelfMgmt');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'UserReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'UserWriter');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (102, 'ReportReader');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(102, 'RechargeReader');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(102, 'AgentMgmt');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(102, 'AgentSettler');

INSERT INTO user_role (`id`, `name`, `memo`) VALUES (103, 'cs', '客服');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (103, 'ChannelReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (103, 'ConfigReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (103, 'InstrumentReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (103, 'MerchantReader');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (103, 'SelfMgmt');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (103, 'UserReader');

INSERT INTO user_role (`id`, `name`, `memo`) VALUES (104, 'Checker', '查单');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (104, 'SelfMgmt');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (104, 'PaymentReader');

INSERT INTO user_role (`id`, `name`, `memo`) VALUES (105, 'CA', '查账');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (105, 'SelfMgmt');
INSERT INTO role_perm (`role_id`, `perm`) VALUES (105, 'ReportReader');

INSERT INTO `group_pay`.`user_role`(`name`,`memo`)VALUES('Agent','商户');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(106,'SelfMgmt');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(106,'ReportReader');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(106,'PaymentReader');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(106,'Agent');

INSERT INTO `commission_ratio`(`lbound`,`ubound`,`ratio`)VALUES(1,50000,5);
INSERT INTO `commission_ratio`(`lbound`,`ubound`,`ratio`)VALUES(50001,200000,6);
INSERT INTO `commission_ratio`(`lbound`,`ubound`,`ratio`)VALUES(200001,500000,7);
INSERT INTO `commission_ratio`(`lbound`,`ubound`,`ratio`)VALUES(500001,1000000,8);
INSERT INTO `commission_ratio`(`lbound`,`ubound`,`ratio`)VALUES(1000001,5000000,9);
INSERT INTO `commission_ratio`(`lbound`,`ubound`,`ratio`)VALUES(5000001,-1,10);

INSERT INTO `group_pay`.`site_config`(`name`,`display_name`,`value`)VALUES('marquee','跑马灯讯息','欢迎使用淘金宝');

drop table if exists `award_config`;
create table award_config
(
`id` bigint not null auto_increment primary key,
`bouns` int,
`condition` int,
`modify_time` bigint not null
)engine=InnoDB default charset=utf8mb4 auto_increment=1000;

INSERT INTO `award_config`(`bouns`,`condition`,`modify_time`)VALUES(13,5000,1559059200000);
INSERT INTO `award_config`(`bouns`,`condition`,`modify_time`)VALUES(28,10000,1559059200000);
INSERT INTO `award_config`(`bouns`,`condition`,`modify_time`)VALUES(58,20000,1559059200000);
INSERT INTO `award_config`(`bouns`,`condition`,`modify_time`)VALUES(118,40000,1559059200000);
INSERT INTO `award_config`(`bouns`,`condition`,`modify_time`)VALUES(238,80000,1559059200000);

drop table if exists `user_evaluation`;
CREATE TABLE `user_evaluation` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `type` int(11) NOT NULL,
  `condition` int(11) DEFAULT '0',
  `count` int(11) DEFAULT '0',
  `value` int(11) DEFAULT '0',
  `group` int(11) DEFAULT '0',
  `repeat` int(11) DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4;

drop table if exists `evaluation_log`;
create table `evaluation_log` (
    `id` bigint not null auto_increment primary key,
    `type` int not null,
    `operator_id` bigint not null default 0,
    `user_id` bigint not null,
    `point` int not null,
    `point_before` double not null,
    `point_after` double not null,
	`note` varchar(40),
    `time` bigint not null,
    index `idx_txn_log_uid` (`user_id`),
    index `idx_txn_log_time` (`time`)
) engine=InnoDB default charset=utf8mb4;

CREATE TABLE `transaction_log` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `type` int(11) NOT NULL,
  `payment_id` bigint(20) NOT NULL,
  `operator_id` bigint(20) NOT NULL DEFAULT '0',
  `user_id` bigint(20) NOT NULL,
  `amount` int(11) NOT NULL,
  `balance_before` double NOT NULL,
  `balance_after` double NOT NULL,
  `time` bigint(20) NOT NULL,
  `amountFrom` bigint(20) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_txn_log_uid` (`user_id`),
  KEY `idx_txn_log_time` (`time`)
) ENGINE=InnoDB AUTO_INCREMENT=158558 DEFAULT CHARSET=utf8mb4;

CREATE TABLE `recharge` (
  `id` varchar(64) NOT NULL,
  `channel` int(11) NOT NULL,
  `amount` int(11) NOT NULL,
  `user_id` bigint(20) NOT NULL,
  `create_time` bigint(20) NOT NULL,
  `settle_time` bigint(20) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_create_time` (`create_time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `merchant_wireout_order` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `status` int(11) NOT NULL,
  `user_id` bigint(20) NOT NULL,
  `account_provider` varchar(200) DEFAULT NULL,
  `account_holder` varchar(60) DEFAULT NULL,
  `account_name` varchar(100) DEFAULT NULL,
  `amount` int(11) NOT NULL,
  `operator` bigint(20) DEFAULT null,
  `create_time` bigint(20) NOT NULL,
  `settle_time` bigint(20) DEFAULT 0,
  PRIMARY KEY (`id`),
  KEY `idx_user_id` (`user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=1000 DEFAULT CHARSET=utf8mb4;