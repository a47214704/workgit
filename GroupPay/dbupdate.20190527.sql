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

create table award_record
(
`id` bigint not null auto_increment primary key,
`user_id` bigint not null,
`bouns` int not null,
`create_time` bigint not null
)engine=InnoDB default charset=utf8mb4 auto_increment=1000;