alter table payment add `ratio` double DEFAULT 0;
alter table merchant add `wechat_ratio_static` double DEFAULT 0;
alter table merchant add `ali_ratio_static` double DEFAULT 0;
alter table merchant add `bank_ratio_static` double DEFAULT 0;