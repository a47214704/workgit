<?php
class zealchinSdk
{
	//创建订单接口地址
	private static $create_order_api	= 'https://third.zealchin.com/p_order/create';
	private static $query_order_api		= 'https://third.zealchin.com/p_order/query';
	//接口密钥
	private $interface_key = 'e043fb66ee43b8d0f21936f41287d545';
	//商户编号
	private $member_id= 10020;
	
	//构造函数
	function __construct($interface_key,$member_id) {
		$this->interface_key=$interface_key;
		$this->member_id=$member_id;
	}
		  	 
    public function create_order($order_no,$money,$channel_id,$notify_url='',$redirect_url=''){
		//获取时间戳
		$time=time();
		$Map=[];
		$Map['order_no']=$order_no;
		$Map['money']=$money;
		$Map['member_id']=$this->member_id;
		$Map['channel_id']=$channel_id;
		$Map['jump']=0;
		if(!empty($notify_url)){
			$Map['notify_url']=$notify_url;
		}
		if(!empty($redirect_url)){
			$Map['redirect_url']=$redirect_url;
		}
		//按照a-z顺序将字段进行排序。
		ksort($Map);
		//将MAP内的值合并（仅仅值合并）
		$sign_str=implode('',$Map);
		//在字符串后增加查询密钥
		$sign_str.=$this->interface_key;
		//将字符串md5,小写
		$sign_str=md5($sign_str);
		$Map['sign']=$sign_str;
		//发送POST请求
		$Result=$this->httprequest(self::$create_order_api,$Map);
		//判断是否获取到正确的信息
		if($Result['http_code']==200){
			$Json=json_decode($Result['body'],true);
			if((!$Json)||(!isset($Json['status']))) return ['status'=>0,'msg'=>'订单接口访问失败'];
			return $Json;
		}
		return ['status'=>0,'msg'=>'未知错误'.$Result['http_code'],'content'=>$Result['body']];
	}
	
	public function query_order($order_no){
		$Map=[];
		$Map['time']=time();
		$Map['member_id']=$this->member_id;
		$Map['order_no']=$order_no;
		//按照a-z顺序将字段进行排序。
		ksort($Map);
		//将MAP内的值合并（仅仅值合并）
		$sign_str=implode('',$Map);
		//在字符串后增加查询密钥
		$sign_str.=$this->interface_key;
		//将字符串md5,小写
		$sign_str=md5($sign_str);
		$Map['sign']=$sign_str;
		//发送POST请求
		$Result=$this->httprequest(self::$query_order_api,$Map);
		//判断是否获取到正确的信息
		if($Result['http_code']==200){
			$Json=json_decode($Result['body'],true);
			if((!$Json)||(!isset($Json['status']))) return ['status'=>0,'msg'=>'订单接口访问失败'];
			return $Json;
		}
		return ['status'=>0,'msg'=>'未知错误'.$Result['http_code']];
	}
	
	public function notify_verify($data){
		$sign=$data['sign'];
		$Map=$data;
		unset($Map['sign']);
		ksort($Map);
		$check_sign_str=implode('',$Map).$this->interface_key;
		$check_sign=md5($check_sign_str);
		return $sign==$check_sign;
	}
	
	/**
	  +----------------------------------------------------------
	 * 以指定DNS方式访问指定域名获取返回数据
	  +----------------------------------------------------------
	 * @param string    $url      需要访问的URL地址
	 * @param array     $postdata POST信息数组
	 * @param string    $ip       指定的IP
	 * @param int       $timeout  超时时间
	  +----------------------------------------------------------
	 * @return array   返回数据组
	  +----------------------------------------------------------
	 */
	function httprequest($url , $postdata=null,$is_xwww=false, $ip=null , $timeout=20, $useragent='',$cookie_jar='')
	{
		$ch = curl_init();
		
		$herder = array();
		if($ip) 
		{
			$url = parse_url ($url);
			$host = $url['host'];
			$url = $url['scheme'].'://'
				 . $ip
				 . (empty($url['port'])?'':':'.$url['port'])
				 . $url['path']
				 . (empty($url['query'])?'':'?'.$url['query']);
			$herder[]  = 'Host: '.$host;
		}
		if ($is_xwww){
			$herder[]='Content-Type: application/x-www-form-urlencoded';
			if(is_array($postdata) && count($postdata)>0){
				$postdata=http_build_query($postdata);
			}
			$header[]='Content-Length: ' . strlen($postdata);
		}
		if(count($herder)>0) {
			curl_setopt($ch, CURLOPT_HTTPHEADER, $herder);
		}
		curl_setopt($ch, CURLOPT_TIMEOUT, $timeout);
		curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, false);
		curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false);
		curl_setopt($ch, CURLOPT_URL, $url);
		curl_setopt($ch, CURLOPT_HEADER, false);
		curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
		if (!empty($cookie_jar)){
			curl_setopt($ch, CURLOPT_COOKIEFILE, $cookie_jar); 
		}
		if (!empty($useragent)){
			curl_setopt($ch, CURLOPT_USERAGENT, $useragent);
		}
		
		if(is_array($postdata) && count($postdata)>0)
		{
			curl_setopt($ch, CURLOPT_POST, 1);
			curl_setopt($ch, CURLOPT_POSTFIELDS, $postdata);
		} elseif($postdata!=null && !is_array($postdata)) {
			curl_setopt($ch, CURLOPT_POST, 1);
			curl_setopt($ch, CURLOPT_POSTFIELDS, $postdata);
		} else {
			
		}
		
		$body = curl_exec($ch);
		$info = curl_getinfo($ch);
		$info['body'] = $body;
		curl_close($ch);
		return $info;
	}
	
	/**
	 *十进制转二进制、八进制、十六进制 不足位数前面补零*
	 *
	 * @param array $datalist 传入数据array(100,123,130)
	 * @param int $bin 转换的进制可以是：2,8,16
	 * @return array 返回数据 array() 返回没有数据转换的格式
	 * @Author chengmo QQ:8292669 
	 * @copyright http://www.cnblogs.com/chengmo 
	 */
	public static function decto_bin($datalist,$bin)
	{
		static $arr=array(0,1,2,3,4,5,6,7,8,9,'A','B','C','D','E','F');
		if(!is_array($datalist)) $datalist=array($datalist);
		if($bin==10)return $datalist; //相同进制忽略
		$bytelen=ceil(16/$bin); //获得如果是$bin进制，一个字节的长度
		$aOutChar=array();
		foreach ($datalist as $num)
		{
			$t="";
			$num=intval($num);
		if($num===0){$aOutChar[]='0';continue;}
			while($num>0)
			{
				$t=$arr[$num%$bin].$t;
				$num=floor($num/$bin);
			}
			$tlen=strlen($t);
			if($tlen%$bytelen!=0)
			{
			$pad_len=$bytelen-$tlen%$bytelen;
			$t=str_pad("",$pad_len,"0",STR_PAD_LEFT).$t; //不足一个字节长度，自动前面补充0
			}
			$aOutChar[]=$t;
		}
		return $aOutChar;
	} 
	
	/**
	 * 生成订单号
	 * @return 订单号
	 */

	public static function make_order_no($uid,$head='TE'){
		$my_t = gettimeofday();
		$time = time();
		$binMap = [];
		$binMap[0]=str_pad(self::decto_bin($uid,16)[0],4,'0',STR_PAD_LEFT);
		$binMap[1]=str_pad(self::decto_bin($time,16)[0],8,'0',STR_PAD_LEFT);
		$binMap[2]=str_pad(self::decto_bin(round($my_t['usec']/1000),16)[0],4,'0',STR_PAD_LEFT);
		$binMap[3]=str_pad(self::decto_bin(rand(1,65535),16)[0],4,'0',STR_PAD_LEFT);
		return $head.join($binMap);
	}
}
