alter table `user_account` add `evaluation_point` int default 50;
alter table `agency_commission` add `type` int not null;
alter table `site_config` add unique key `name` (`name`);

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
