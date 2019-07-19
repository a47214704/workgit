alter table `merchant` add `user_id` bigint not null;
alter table `merchant` add `wechat_ratio` double;
alter table `merchant` add `ali_ratio` double;
alter table `merchant` add `bank_ratio` double;

INSERT INTO `user_role`(`name`,`memo`)VALUES('Agent','商户');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(106,'SelfMgmt');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(106,'PaymentReader');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(106,'Agent');

INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(101,'RechargeReader');
INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(101,'AgentMgmt');