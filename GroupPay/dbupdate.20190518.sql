alter table collect_channel add `enabled` bit default 1;
alter table collect_channel add `instruments_limit` int default 5;
ALTER TABLE `user_account` CHANGE COLUMN `paddingBalance` `pending_balance` int default 0;
ALTER TABLE `user_account` CHANGE COLUMN `paddingCount` `pending_payments` int default 0;