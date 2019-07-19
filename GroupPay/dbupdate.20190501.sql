drop table if exists `site_config`;
create table `site_config` (
    `id` int not null auto_increment primary key,
    `name` varchar(100) not null,
    `display_name` varchar(100) not null,
    `value` varchar(255) not null
) engine=InnoDB default charset=utf8mb4;