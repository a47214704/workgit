ALTER TABLE `user_account` ADD `wechat_account` varchar(20) not null;
ALTER TABLE `user_account` CHANGE COLUMN `status` `status` int not null default -1 ;
