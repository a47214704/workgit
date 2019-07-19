ALTER TABLE `user_account` CHANGE COLUMN `merchant_id` `merchant_id` int default 0;
ALTER TABLE `collect_channel` ADD `enabled` bit default 1;
ALTER TABLE `collect_channel` ADD `instruments_limit` int default 5;