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
    constraint `pk_agency_commission` primary key (`week`, `user_id`),
    index `idx_agency_commission_uid` (`user_id`)
) engine=InnoDB default charset=utf8mb4;