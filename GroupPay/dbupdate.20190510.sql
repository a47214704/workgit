drop table if exists `collect_channel`;
create table `collect_channel` (
    `id` int not null auto_increment primary key,
    `name` varchar(40) not null,
    `type` int not null,
	`provider` int not null,
	`ratio` int default 5
) engine=InnoDB default charset=utf8mb4;

insert into `collect_channel`(`name`, `type`, `provider`, `ratio`) values('微信', 1, 1, 7);
insert into `collect_channel`(`name`, `type`, `provider`, `ratio`) values('支付宝', 1, 2, 6);
insert into `collect_channel`(`name`, `type`, `provider`, `ratio`) values('银行卡', 2, 3, 5);
insert into `collect_channel`(`name`, `type`, `provider`, `ratio`) values('支付宝红包', 3, 2, 6);
insert into `collect_channel`(`name`, `type`, `provider`, `ratio`) values('云闪付', 4, 3, 5);

alter table `collect_instrument` add `daily_limit` int default 0;
alter table `collect_instrument` add `expiry_limit` int default 0;

update `collect_instrument` set `daily_limit` = 30000 where `channel_id` = 1;
update `collect_instrument` set `expiry_limit` = 7 where `channel_id` = 1;
update `collect_instrument` set `daily_limit` = 50000 where `channel_id` = 2;
