using ANetConnect;
using CriticalValueService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;

namespace CriticalValueService
{
    public class PushCriticalMessage
    {

        public void DoStartPushMsgTask(CriticalMainEntry entry, int MainId)
        {
            Task.Factory.StartNew(() =>
            {
                string ms接收端IDN = string.Empty;
                if (entry.PatientType == "门诊")
                {
                    ms接收端IDN = "11";
                }else if (entry.PatientType == "住院")
                {
                    ms接收端IDN = "12,2";
                }
                else
                {
                    return;
                }
                //获取通知模板
                var strSql = $"select A.内容,A.推送方式N,A.接收对象N,A.护士确认超时时间N,A.医生处置超时时间N,A.主任医生处置超时时间N,接收端IDN from vc_vt推送模板关联表 A where a.业务类别I={entry.BusinessClass} and A.模板类别N=0 and A.接收端IDN in({ms接收端IDN})";
                var ds = DB.ExecSQL(strSql);
                if (ds.IsEof())
                {
                    Fun.WriteLog($"患者：{entry.Name}消息推送失败，未能查询到对应的推送模板。业务类别：{entry.BusinessClass},接收端idn：{ms接收端IDN}");
                }
                while (!ds.IsEof())
                {
                    var ms内容 = ds.GetValue("内容");
                    var strAcceptObj = ds.GetValue("接收对象N");
                    var nurseTime = Fun.StrToInt(ds.GetValue("护士确认超时时间N"));
                    var msgType = ds.GetValue("接收端IDN");
                    if (string.IsNullOrEmpty(ms内容)) return;
                    switch (ds.GetValue("推送方式N"))
                    {
                        case "0"://平台推送
                            DoPushPlatformMsg(ms内容, entry, strAcceptObj, nurseTime, MainId,msgType);
                            break;
                        case "1":
                            Do短信推送(entry, ms内容, strAcceptObj, MainId);
                            break;
                        case "2":
                            break;
                    }
                    ds.MoveNext();
                }
                
            });
        }
        /// <summary>
        /// 平台推送
        /// </summary>
        /// <param name="strContent"></param>
        /// <param name="entry"></param>
        /// <param name="StrAcceptObj"></param>
        /// <param name="NurseTimeout"></param>
        /// <param name="DoctorTimeout"></param>
        /// <param name="MainDoctorTimeout"></param>
        private void DoPushPlatformMsg(string strContent, CriticalMainEntry entry, string StrAcceptObj, int NurseTimeout, int mainId,string msgType)
        {
            //推送给科室下所有员工
            var msgID = PushPlatformMsg(strContent, entry, StrAcceptObj,mainId,msgType);
            DateTime sendTime;
            if (msgID <= 0)
            {
                sendTime = DateTime.Now;
            }
            else
            {
                var strTime = DB.ExecSingle($"select 发送时间 from AMS_K消息列表  where 系统序号='{msgID}'");
                sendTime = Convert.ToDateTime(strTime);
            }
            Timer Accepttimer = new Timer();
            Accepttimer.Interval = 60000;
            bool isTelPush = false;
            Accepttimer.Elapsed += delegate
            {
                try
                {
                    var rsData = DB.ExecSQL($"select * from VC_D危急值主表 where 系统序号='{mainId}' and 处置否B=1");
                    if (rsData.IsEof())
                    {
                        var rsStatus = DB.ExecSingle($"select 处置否B from VC_D危急值主表 where 系统序号='{mainId}'");
                        if (rsStatus == "0")//如果未确认
                        {
                            var datas = DB.ExecSQL($"select t.确认人R,t.确认时间 from AMS_K消息分发列表 t where t.消息序号i in ({msgID}) and 状态N=2 order by 确认时间");//护士或者医生确认
                            if (!datas.IsEof())
                            {
                                DB.ExecCmd($"update VC_D危急值主表 set 处置否B=2 where 系统序号={mainId}");
                                var confirm = DB.ExecSingle($"select 名称 from DOC_T员工档案  where 系统序号='{datas.GetValue("确认人R")}'");
                                AddOperateRecord(mainId, 0, Fun.StrToInt(datas.GetValue("确认人R")), "消息已确认", $"危急值消息被确认，确认人：{confirm}，确认时间：{datas.GetValue("确认时间")}");
                                ConfrimMsgToDeal(entry, mainId, Convert.ToDateTime(datas.GetValue("确认时间")));
                                Accepttimer.Stop();
                            }
                            else
                            {
                                if (DateTime.Now > sendTime.AddMinutes(NurseTimeout) && !isTelPush)//护士或者医生确认超时
                                {
                                    var strSql = string.Empty;
                                    //通知医技科室电话通知
                                    if (entry.BusinessClass == 1)//检验
                                    {
                                        strSql = $"select A.科室序号I from DOC_L科室属性关联表 A inner join doc_b科室属性 B on A.科室属性I=B.系统序号 inner join Doc_T科室档案 C on A.科室序号I=C.系统序号 where B.系统序号=5 and c.隶属机构i='{Fun.GetOrgId()}' ";
                                    }
                                    else
                                    {
                                        strSql = $"select A.科室序号I from DOC_L科室属性关联表 A inner join doc_b科室属性 B on A.科室属性I=B.系统序号 inner join Doc_T科室档案 C on A.科室序号I=C.系统序号 where B.系统序号=4 and c.隶属机构i='{Fun.GetOrgId()}' ";
                                    }
                                    var ds = DB.ExecSQL(strSql);
                                    while (!ds.IsEof())
                                    {
                                        var deptId = ds.GetValue("科室序号I");
                                        if (!string.IsNullOrEmpty(deptId))
                                        {
                                            List<string> users = new List<string>();
                                            var rs = DB.ExecSQL($"select 系统序号 from DOC_T员工档案 where 隶属科室I='{deptId}'");
                                            while (!rs.IsEof())
                                            {
                                                users.Add(rs.GetValue("系统序号"));
                                                rs.MoveNext();
                                            }
                                            if (users.Count > 0)
                                            {
                                                Fun.MessagePush("37", $"{JsonConvert.SerializeObject(entry)}&{mainId}", EMsgRec.User, users.ToArray());
                                                AddOperateRecord(mainId, 0, 0, "推送给医技科室提示进行电话通知", $"护士/医生{NurseTimeout}分钟未确认，推送给医技科室提示电话通知。");
                                                isTelPush = true;
                                            }
                                        }
                                        ds.MoveNext();
                                    }
                                }
                            }
                        }else if (rsStatus == "2"||rsStatus=="4")
                        {
                            Accepttimer.Stop();
                        }
                      
                    }
                    else
                    {
                        Accepttimer.Stop();
                    }
                }
                catch (Exception ex)
                {
                    Fun.WriteLog($"消息推送发生异常:{ex.Message}\r\n{ex.StackTrace}");
                }
            };
            Accepttimer.Start();
        }
        /// <summary>
        /// 书写病程记录判断
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="mainId"></param>
        /// <param name="dealTime"></param>
        public void  WriteMedicalRecord (int businessClass,int mainId,DateTime dealTime)
        {
            var writeTimeout = DB.ExecSingle($"select 书写病程超时时间N from VC_B业务类别 where 系统序号='{businessClass}' and 有效状态=1");
            if (!string.IsNullOrEmpty(writeTimeout))
            {
                Timer timer = new Timer();
                timer.Interval = 120000;
                timer.Elapsed += delegate
                {
                    bool isFinishWrite = DB.ExecSQLExist($"select * from VC_D危急值过程记录 where 主表序号I='{mainId}' and 过程状态='书写病程记录完成' ");
                    if (isFinishWrite)//书写了病程记录
                    {
                       // AddOperateRecord(mainId, 0, 1, "病程记录书写完成。", $"已经完成病程记录书写，书写时间：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                        timer.Stop();
                    }
                    else
                    {
                        if (DateTime.Now > dealTime.AddMinutes(Fun.StrToInt(writeTimeout)))//超时未进行病程记录
                        {
                            AddOperateRecord(mainId, 0, 0, "病程记录书写完成。", $"医生超过：{writeTimeout}分钟未进行病程书写记录。");
                            timer.Stop();
                        }

                    }
                };
                timer.Start();
            }
          
        }

        private void ConfrimMsgToDeal(CriticalMainEntry entry, int MainId, DateTime confirmTime)
        {
            string ms接收端IDN = string.Empty;
            if (entry.PatientType == "门诊")
            {
                ms接收端IDN = "11";
            }
            else if (entry.PatientType == "住院")
            {
                ms接收端IDN = "12,2";
            }
            else
            {
                return;
            }
            //获取超时模板
            var strSql = $"select A.内容,A.推送方式N,A.接收对象N,A.护士确认超时时间N,A.医生处置超时时间N,A.主任医生处置超时时间N,接收端IDN from vc_vt推送模板关联表 A where a.业务类别I={entry.BusinessClass} and A.模板类别N=1 and 接收端IDN in({ms接收端IDN})";
            var timeoutTemps = DB.ExecSQL(strSql);
            while (!timeoutTemps.IsEof())
            {
                var doctotTimeout = Fun.StrToInt(timeoutTemps.GetValue("医生处置超时时间N"));
                var mainDoctotTimeout = Fun.StrToInt(timeoutTemps.GetValue("主任医生处置超时时间N"));
                var timeoutContent = timeoutTemps.GetValue("内容");
                var timeoutAccept = timeoutTemps.GetValue("接收对象N");
                var timeoutPush = timeoutTemps.GetValue("推送方式N");
                var msgType= timeoutTemps.GetValue("接收端IDN");
                var ms内容 = DB.ExecSingle(timeoutContent.Replace("<患者序号I>", entry.PatiendId.ToString()));
                if (string.IsNullOrEmpty(ms内容)) return;
                bool ispushMainDoctor = false;//是否推送给主任医师
                bool isDptPush = false;//是否推送给医务科
                Timer timer = new Timer();
                timer.Interval = 120000;
                timer.Elapsed += delegate
                {
                    try
                    {
                        var rsData = DB.ExecSQL($"select * from VC_D危急值主表 where 系统序号='{MainId}' and 处置否B=1");
                        if (rsData.IsEof())
                        {
                            if (mainDoctotTimeout > 0)//超时推给医务科,配置了主任医师超时时间
                            {
                                PushMainDoctorAndMedicalDpt(entry, MainId, confirmTime, doctotTimeout, mainDoctotTimeout, timeoutAccept, timeoutPush, msgType, ms内容, ref ispushMainDoctor, ref isDptPush, timer);
                            }
                            else//不推给医务科
                            {
                                if (confirmTime.AddMinutes(doctotTimeout) < DateTime.Now )
                                {
                                    if (entry.ApplyDepId > 0)
                                    {
                                        switch (timeoutPush)
                                        {
                                            case "0":
                                                List<string> users = new List<string>();
                                                var res = DB.ExecSQL($"select B.系统序号 from DOC_L科室员工关联表 A inner join  doc_t员工档案 B on a.员工序号r=B.系统序号 inner join doc_t管理职务 C on c.系统序号=b.管理职务i where a.科室序号i='{entry.ApplyDepId}'  and c.名称 like '%科室主任%'");
                                                while (!res.IsEof())
                                                {
                                                    users.Add(res.GetValue("系统序号"));
                                                    res.MoveNext();
                                                }
                                                if (users.Any())
                                                {
                                                    Fun.MessagePush(msgType, ms内容, EMsgRec.User, users.ToArray());
                                                    AddOperateRecord(MainId, 0, 0, "平台推送给科室主任", $"医生超过{doctotTimeout}分钟未处置，推送给科室主任。");
                                                }
                                                break;
                                            case "1":
                                                var mssql = $"select nvl(b.联系电话,0) as 联系电话,B.名称,B.系统序号 from DOC_L科室员工关联表 A inner join  doc_t员工档案 B on a.员工序号r=B.系统序号 inner join doc_t管理职务 C on c.系统序号=b.管理职务i where a.科室序号i='{entry.ApplyDepId}'  and c.名称 like '%科室主任%'";
                                                var ds = DB.ExecSQL(mssql);
                                                while (!ds.IsEof())
                                                {
                                                    var ms手机号码 = ds.GetValue("联系电话");
                                                    var ms姓名 = ds.GetValue("名称");
                                                    var userID = ds.GetValue("系统序号");
                                                    if (ms手机号码 != "0") Fun.DoSendTelMessage(ms姓名, ms手机号码, ms内容);
                                                    AddOperateRecord(MainId, 0, 0, "短信推送给科室主任", $"医生超过{doctotTimeout}分钟未处置，短信推送给科室主任，姓名：{ms姓名},电话：{ms手机号码}");
                                                    ds.MoveNext();

                                                }
                                                break;
                                            case "2"://暂不支持
                                                break;
                                        }
                                    }
                                    timer.Stop();

                                }
                            }
                        }
                        else
                        {
                            timer.Stop();
                        }
                    }
                    catch (Exception ex)
                    {
                        Fun.WriteLog($"消息推送发生异常:{ex.Message}\r\n{ex.StackTrace}");
                    }
                };
                timer.Start();

                timeoutTemps.MoveNext();
            }
        }
        /// <summary>
        /// /推送给主任医生和医务科
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="MainId"></param>
        /// <param name="confirmTime"></param>
        /// <param name="doctotTimeout"></param>
        /// <param name="mainDoctotTimeout"></param>
        /// <param name="timeoutAccept"></param>
        /// <param name="timeoutPush"></param>
        /// <param name="msgType"></param>
        /// <param name="ms内容"></param>
        /// <param name="ispushMainDoctor"></param>
        /// <param name="isDptPush"></param>
        /// <param name="timer"></param>
        private void PushMainDoctorAndMedicalDpt(CriticalMainEntry entry, int MainId, DateTime confirmTime, int doctotTimeout, int mainDoctotTimeout, string timeoutAccept, string timeoutPush, string msgType, string ms内容, ref bool ispushMainDoctor, ref bool isDptPush, Timer timer)
        {
            //如果服务器时间大于初次发送时间+主任医生推送间隔时间并且小于初次发送时间+医务室推送间隔时间，则推送主任医师
            if (confirmTime.AddMinutes(doctotTimeout) < DateTime.Now && DateTime.Now < confirmTime.AddMinutes(mainDoctotTimeout) && !ispushMainDoctor)
            {
                if (entry.ApplyDepId > 0)
                {
                    switch (timeoutPush)
                    {
                        case "0":
                            List<string> users = new List<string>();
                            var res = DB.ExecSQL($"select B.系统序号 from DOC_L科室员工关联表 A inner join  doc_t员工档案 B on a.员工序号r=B.系统序号 inner join doc_t管理职务 C on c.系统序号=b.管理职务i where a.科室序号i='{entry.ApplyDepId}'  and c.名称 like '%科室主任%'");
                            while (!res.IsEof())
                            {
                                users.Add(res.GetValue("系统序号"));
                                res.MoveNext();
                            }
                            if (users.Any())
                            {
                                Fun.MessagePush(msgType, ms内容, EMsgRec.User, users.ToArray());
                                AddOperateRecord(MainId, 0, 0, "平台推送给科室主任", $"医生超过{doctotTimeout}分钟未处置，推送给科室主任。");
                            }
                            break;
                        case "1":
                            var mssql = $"select nvl(b.联系电话,0) as 联系电话,B.名称,B.系统序号 from DOC_L科室员工关联表 A inner join  doc_t员工档案 B on a.员工序号r=B.系统序号 inner join doc_t管理职务 C on c.系统序号=b.管理职务i where a.科室序号i='{entry.ApplyDepId}'  and c.名称 like '%科室主任%'";
                            var ds = DB.ExecSQL(mssql);
                            while (!ds.IsEof())
                            {
                                var ms手机号码 = ds.GetValue("联系电话");
                                var ms姓名 = ds.GetValue("名称");
                                var userID = ds.GetValue("系统序号");
                                if (ms手机号码 != "0") Fun.DoSendTelMessage(ms姓名, ms手机号码, ms内容);
                                AddOperateRecord(MainId, 0, 0, "短信推送给科室主任", $"医生超过{doctotTimeout}分钟未处置，短信推送给科室主任，姓名：{ms姓名},电话：{ms手机号码}");
                                ds.MoveNext();

                            }
                            break;
                        case "2"://暂不支持
                            break;
                    }
                }
                ispushMainDoctor = true;

            }//如果服务器时间大于初次推送时间+医务室推送间隔时间，则推医务室
            else if (DateTime.Now > confirmTime.AddMinutes(mainDoctotTimeout) && !isDptPush)
            {

                var ds = DB.ExecSQL($"select A.科室序号I from DOC_L科室属性关联表 A inner join doc_b科室属性 B on A.科室属性I=B.系统序号 where B.系统序号=64  ");
                while (!ds.IsEof())
                {
                    var deptId = ds.GetValue("科室序号I");
                    switch (timeoutPush)
                    {
                        case "0"://平台推送
                            if (timeoutAccept == "1")
                            {
                                var ds1 = DB.ExecSQL($"select 系统序号 from DOC_T员工档案 where 隶属科室I='{deptId}'");
                                List<string> users = new List<string>();
                                while (!ds1.IsEof())
                                {
                                    users.Add(ds.GetValue("系统序号"));
                                    ds1.MoveNext();
                                }
                                if (users.Count > 0)
                                {
                                    Fun.MessagePush(msgType, ms内容, EMsgRec.User, users.ToArray());
                                    AddOperateRecord(MainId, 0, 0, "平台推送给医务科用户", $"平台推送给医务科完成");
                                    isDptPush = true;
                                }
                            }
                            else
                            {
                                var item = new List<string> { };
                                var clients = DB.ExecSQL(
                                    $"Select 终端序号I From doc_Vl终端科室关联表  Where 隶属机构I={Fun.GetOrgId()} and 科室序号I={deptId}");
                                while (!clients.IsEof())
                                {
                                    item.Add(clients.GetValue("终端序号I"));
                                    clients.MoveNext();
                                }
                                if (item.Any())
                                {
                                    Fun.MessagePush(msgType, ms内容, EMsgRec.Client, item.ToArray());
                                    AddOperateRecord(MainId, 0, 0, "平台推送给医务科终端", $"平台推送给医务科完成");
                                    isDptPush = true;
                                }
                            }
                            break;
                        case "1"://短信推送
                            var mssql = $"select nvl(联系电话,0) as 联系电话,名称, 系统序号 from DOC_T员工档案 where 隶属科室I='{deptId}'";
                            var rs = DB.ExecSQL(mssql);
                            while (!rs.IsEof())
                            {
                                var ms手机号码 = rs.GetValue("联系电话");
                                var ms姓名 = rs.GetValue("名称");
                                var userID = rs.GetValue("系统序号");
                                if (ms手机号码 != "0") Fun.DoSendTelMessage(ms姓名, ms手机号码, ms内容);
                                AddOperateRecord(MainId, 0, 0, "短信推送给医务科", $"短信推送给医务科，姓名：{ms姓名},电话：{ms手机号码}");
                                ds.MoveNext();
                                isDptPush = true;

                            }
                            break;
                        case "2"://暂不支持
                            break;
                    }

                    ds.MoveNext();
                }
                timer.Stop();
            }
        }

        /// <summary>
        /// 推送平台消息
        /// </summary>
        /// <param name="strContent"></param>
        /// <param name="entry"></param>
        /// <param name="StrAcceptObj"></param>
        /// <returns></returns>
        private int PushPlatformMsg(string strContent, CriticalMainEntry entry, string StrAcceptObj,int MainId,string msgType)
        {
            try
            {
                if (entry.ApplyDepId != 0)//处方科室不为空
                {
                    var str = strContent.Replace("<患者序号I>", entry.PatiendId.ToString());
                    var ms内容 = DB.ExecSingle(str);
                    if (string.IsNullOrEmpty(ms内容)) return 0;
                    if (StrAcceptObj == "1")
                    {
                        var ds = DB.ExecSQL($"select 系统序号 from DOC_T员工档案 where 隶属科室I='{entry.ApplyDepId}'");
                        List<string> users = new List<string>();
                        while (!ds.IsEof())
                        {
                            users.Add(ds.GetValue("系统序号"));
                            ds.MoveNext();
                        }
                        if (!users.Contains(entry.ApplyDoctorId.ToString()))
                        {
                            users.Add(entry.ApplyDoctorId.ToString());
                        }
                        if (users.Count > 0)
                        {
                            Fun.MessagePush(msgType, ms内容, EMsgRec.User, users.ToArray());
                            if (msgType == "11")
                            {
                                AddOperateRecord(MainId, 0, 0, "推送给科室下所有门诊医生", $"推送给科室下所有门诊医生");
                            }
                            else if(msgType=="12")
                            {
                                AddOperateRecord(MainId, 0, 0, "推送给科室下所有住院医生", $"推送给科室下所有住院医生");
                            }
                            else if (msgType == "2")
                            {
                                AddOperateRecord(MainId, 0, 0, "推送给科室下所有住院护士", $"推送给科室下所有住院护士");
                            }
                            var re = Fun.MessageId;
                            return Fun.StrToInt(Fun.MessageId);
                        }
                    }
                    else
                    {
                        var item = new List<string> { };
                        var ds = DB.ExecSQL(
                            $"Select 终端序号I From doc_Vl终端科室关联表  Where 隶属机构I={Fun.GetOrgId()} and 科室序号I={entry.ApplyDepId}");
                        while (!ds.IsEof())
                        {
                            item.Add(ds.GetValue("终端序号I"));
                            ds.MoveNext();
                        }
                        if (item.Any())
                        {
                            Fun.MessagePush(msgType, ms内容, EMsgRec.Client, item.ToArray());
                            if (msgType == "11")
                            {
                                AddOperateRecord(MainId, 0, 0, "推送给科室下所有门诊医生终端", $"推送给科室下所有门诊医生终端");
                            }
                            else if (msgType == "12")
                            {
                                AddOperateRecord(MainId, 0, 0, "推送给科室下所有住院医生终端", $"推送给科室下所有住院医生终端");
                            }
                            else if (msgType == "2")
                            {
                                AddOperateRecord(MainId, 0, 0, "推送给科室下所有住院护士终端", $"推送给科室下所有住院护士终端");
                            }
                            return Fun.StrToInt(Fun.MessageId);
                        }
                    }
                }
               
            }
            catch(Exception ex)
            {
                Fun.WriteLog($"消息推送发生异常:{ex.Message}\r\n{ex.StackTrace}");
            }
            
            return 0;//返回消息Id
        }
        /// <summary>
        /// 短信推送
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="ms内容sql"></param>
        /// <param name="StrAcceptObj"></param>
        /// <param name="MianId"></param>
        private void Do短信推送(CriticalMainEntry entry, string ms内容sql, string StrAcceptObj, int MianId)
        {
            try
            {

                var ms内容 = DB.ExecSingle(ms内容sql.Replace($"<患者序号I>", entry.PatiendId.ToString()));
                if (StrAcceptObj == "2")
                {
                    if (entry.HealthSerial <= 0) return;
                    var mssql = $"select 联系电话,姓名 from crm_d健康档案 where 系统序号='{entry.HealthSerial}' and 隶属机构I={Fun.GetOrgId()}";
                    var ds = DB.ExecSQL(mssql);
                    while (!ds.IsEof())
                    {
                        var ms手机号码 = ds.GetValue("联系电话");
                        var ms姓名 = ds.GetValue("姓名");
                        if (!string.IsNullOrWhiteSpace(ms手机号码)) Fun.DoSendTelMessage(ms姓名, ms手机号码, ms内容);
                        AddOperateRecord(MianId, 0, 1, "短信推送给患者", $"短信推送给患者：{ms姓名},电话：{ms手机号码}");
                        ds.MoveNext();
                    }
                }
                else
                {
                    if (entry.ApplyDepId <= 0) return;
                    var mssql =
                         $"select nvl(b.联系电话,0) as 联系电话,B.名称  from doc_l科室员工关联表 a left join doc_t员工档案 B on a.员工序号r=b.系统序号 where 科室序号I＝{entry.ApplyDepId} and 隶属机构I={Fun.GetOrgId()}";
                    var ds = DB.ExecSQL(mssql);
                    while (!ds.IsEof())
                    {
                        var ms手机号码 = ds.GetValue("联系电话");
                        var ms姓名 = ds.GetValue("名称");
                        if (ms手机号码 != "0") Fun.DoSendTelMessage(ms姓名, ms手机号码, ms内容);
                        AddOperateRecord(MianId, 0, 1, "短信推送给科室", $"短信推送给科室，姓名：{ms姓名},电话：{ms手机号码}");
                        ds.MoveNext();
                    }
                }
            }
            catch (Exception ex)
            {
                Fun.WriteLog($"消息推送发生异常:{ex.Message}\r\n{ex.StackTrace}");
            }
        }

        public void AddOperateRecord(int mainId,int FlowId,int Accepter,string status,string descr)
        {
            try
            {
                var id = DB.GetNextId("VC_D危急值过程记录");
                if (Accepter != 0)
                {
                    DB.ExecCmd($"insert into VC_D危急值过程记录(系统序号,主表序号I,流程节点N,接收人R,操作时间,操作人R,过程状态,过程描述) values('{id}','{mainId}','{FlowId}','{Accepter}',sysdate,'{Fun.GetUserId()}','{status}','{descr}')");
                }
                else
                {
                    DB.ExecCmd($"insert into VC_D危急值过程记录(系统序号,主表序号I,流程节点N,操作时间,操作人R,过程状态,过程描述) values('{id}','{mainId}','{FlowId}',sysdate,'{Fun.GetUserId()}','{status}','{descr}')");
                }

            }
            catch (Exception ex)
            {
                Fun.WriteLog($"消息推送发生异常:{ex.Message}\r\n{ex.StackTrace}");
            }
        }
        

    }
}
