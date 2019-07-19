alter table `collect_channel` add column `valid_time` int default 10;
alter table `merchant` add column `channel_enabled` int default 0;
update `collect_channel` set `valid_time` = 10;
update `merchant` set `channel_enabled` = 0;