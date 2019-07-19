CREATE TABLE `merchant_wireout_order` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `status` int(11) NOT NULL,
  `user_id` bigint(20) NOT NULL,
  `account_provider` varchar(200) DEFAULT NULL,
  `account_holder` varchar(60) DEFAULT NULL,
  `account_name` varchar(100) DEFAULT NULL,
  `amount` int(11) NOT NULL,
  `operator` bigint(20) DEFAULT null,
  `create_time` bigint(20) NOT NULL,
  `settle_time` bigint(20) DEFAULT 0,
  PRIMARY KEY (`id`),
  KEY `idx_user_id` (`user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=1000 DEFAULT CHARSET=utf8mb4;

INSERT INTO `role_perm`(`role_id`,`perm`)VALUES(101,'AgentSettler');