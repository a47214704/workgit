alter table `payment` add column `callback_url` varchar(128);
alter table `payment` add column `origin_amount` int(11);

alter table merchant add column channel_limit text;
update merchant set channel_limit = '';