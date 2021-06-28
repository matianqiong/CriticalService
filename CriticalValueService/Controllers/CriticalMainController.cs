using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ANetConnect;
using CriticalValueService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CriticalValueService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CriticalMainController : ControllerBase
    {
        private PushCriticalMessage pushMessage;
        public CriticalMainController()
        {
            pushMessage = new PushCriticalMessage();
        }
        /// <summary>
        /// 提交危急值信息
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("CommitCritical")]
        public HttpResult<int> CommitCriticalValue(CriticalMainEntry entry)
        {
            Fun.WriteLog($"调用提交危急值数据接口：{JsonConvert.SerializeObject(entry)}");
            HttpResult<int> result = new HttpResult<int>();
            try
            {
                
                var criticalId = DB.GetNextId("VC_D危急值信息");
                var id = DB.GetNextId("VC_D危急值主表");
                var strSQL = $"insert into VC_D危急值主表(系统序号,患者序号I,姓名,年龄,年龄单位,性别, 病人类型,健康序号I,身份证号,申请科室I,申请医生R,危急否B,急诊否B,标本类型,报告时间,生理周期,就餐情况,主任护士R,主管医生R,科室主任R,医务科序号I,业务类别N,处置否B,申请序号I,组合序号,隶属机构i,报告审核人R,危急值报告科室I) " +
                    $"values('{id}','{entry.PatiendId}','{entry.Name}','{entry.Age}','{entry.AgeUnit}','{entry.Sex}','{entry.PatientType}','{entry.HealthSerial}','{entry.IDCard}','{entry.ApplyDepId}','{entry.ApplyDoctorId}','{entry.IsCritical}'," +
                    $"'{entry.IsEmergency}','{entry.SampleType}',TO_DATE('{entry.AuditTime}', 'yyyy-MM-dd hh24:mi:ss'),'{entry.MenstrualCycle}','{entry.DingSituation}','{entry.MainNurse}','{entry.MainDoctor}','{entry.DeptDiretor}','{entry.MedicalDeptment}','{entry.BusinessClass}',0,'{entry.ApplyId}','{entry.GroupIds}','{entry.OrgId}','{entry.Auditor}','{entry.ReportPdt}') ";
                var row = DB.ExecCmd(strSQL);
                if (row > 0)
                {
                    var r = DB.ExecCmd($"insert into VC_D危急值信息(系统序号,主表序号I,危急值信息,诊断信息) values('{criticalId}','{id}','{entry.CriticalValueInfo}','{entry.DiagonsisInfo}')");
                    if (r > 0)
                    {
                        pushMessage.AddOperateRecord(Convert.ToInt32(id), 0, entry.MainNurse, "提交危急值信息", "完成危急值信息提交到危急值系统");
                        result.Code = 0;
                        result.Msg = string.Empty;
                        result.ResultData = Convert.ToInt32(id);
                        pushMessage.DoStartPushMsgTask(entry, Convert.ToInt32(id));
                    }
                    else
                    {
                        result.Code = 1;
                        result.Msg = "危急值数据保存失败！";
                    }
                }
                else
                {
                    result.Code = 1;
                    result.Msg = "危急值数据保存失败！";
                }
            }
            catch(Exception ex)
            {
                result.Code = 2;
                result.Msg = $"调用接口发送异常：{ex.Message}\r\n{ex.StackTrace}";
            }
            
            return result;
        }
        //[Authorize]
        //[HttpGet("PushMsg")]
        //public HttpResult<int> PushMessage(string MsgContent,string userId)
        //{
        //    try
        //    {
        //        List<string> users = new List<string>();
        //        users.Add(userId);
        //        Fun.MessagePush("35", MsgContent, EMsgRec.User, users.ToArray());
        //        Fun.MessagePush("37", MsgContent, EMsgRec.User, users.ToArray());

        //        return new HttpResult<int> { Code = 0, Msg = "消息发送成功",ResultData=Fun.StrToInt(Fun.MessageId) };
        //    }
        //    catch(Exception ex)
        //    {
        //        return new HttpResult<int> { Code = 1, Msg = $"消息发送失败：{ex.Message}" };
        //    }
           
        //}
        /// <summary>
        /// 添加电话通知记录
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        [HttpPost("AddTelRecord")]
        public HttpResult<int> AddTelNotifyRecored(TelRecordEntry entry)
        {
            Fun.WriteLog($"调用AddTelNotifyRecored接口：{JsonConvert.SerializeObject(entry)}");
            HttpResult<int> result = new HttpResult<int>();
            try
            {
                var msNotifier = DB.ExecSingle($"select 名称 from DOC_T员工档案  where 系统序号='{entry.Notifier}'");
                var msAccepter = DB.ExecSingle($"select 名称 from DOC_T员工档案  where 系统序号='{entry.Accepter}'");
                pushMessage.AddOperateRecord(entry.MainId, 0, entry.Accepter, "医技科室电话通知", $"电话通知。通知人:{msNotifier},通知时间：{entry.NotifyTime},接收人:{msAccepter},接收时间：{entry.AcceptTime},接收电话：{entry.AcceptTel}");
                var row = DB.ExecCmd($"insert into VC_D电话通知记录(主表序号I,通知人R,通知时间,接收人R, 接收时间,接收人电话) values" +
                    $"('{entry.MainId}','{entry.Notifier}',TO_DATE('{entry.NotifyTime}', 'yyyy-MM-dd hh24:mi:ss'),'{entry.Accepter}',TO_DATE('{entry.AcceptTime}', 'yyyy-MM-dd hh24:mi:ss'),'{entry.AcceptTel}')");
                if (row > 0)
                {
                    result.Code = 0;
                    result.Msg = string.Empty;
                    result.ResultData = row;
                }

            }catch(Exception ex)
            {
                result.Code = 1;
                result.Msg = $"接口调用发生异常：{ex.Message}\r\n{ex.StackTrace}";
            }
            return result;
        }

        /// <summary>
        /// 开启病程记录超时任务
        /// </summary>
        /// <param name="businessClass">业务类别</param>
        /// <param name="mainId">主表id</param>
        /// <param name="dealTime">处置时间</param>
        /// <returns></returns>
        [HttpGet("StartMedicalRecordTask")]
        public HttpResult<int> StartMedicalRecordTask(int businessClass,int mainId, DateTime dealTime)
        {
            try
            {
                Fun.WriteLog($"调用StartMedicalRecordTask接口。businessClass:{businessClass},mainId:{mainId},dealTime:{dealTime}");
                pushMessage.WriteMedicalRecord(businessClass, mainId, dealTime);
                return new HttpResult<int>() { Code = 0 };
            }
            catch (Exception ex)
            {
                return new HttpResult<int>() { Code = 1, Msg = $"{ex.Message}\r\n{ex.StackTrace}", ResultData = 0 };
            }
           
        }

        /// <summary>
        /// 取消危急值
        /// </summary>
        /// <param name="PatientId">患者序号i</param>
        /// <param name="businessClass">业务类别:1,'检验',2,'超声',3,'放射',4,'心电',5,'病理',6,'其他'</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("CancleCriticalInfo")]
        public HttpResult<int> CancleCriticalInfo(int PatientId,int businessClass)
        {
            try
            {
                Fun.WriteLog($"调用CancleCriticalInfo接口。患者序号I：{PatientId},业务类别：{businessClass}");
                var row = DB.ExecCmd($"update VC_D危急值主表 set 处置否B=4 where 患者序号I='{PatientId}' and 业务类别N='{businessClass}'");
                if (row > 0)
                {
                    return new HttpResult<int>() { Code = 0, ResultData = row };
                }
                else
                {
                    return new HttpResult<int>() { Code = 1,Msg="操作失败！", ResultData = row };
                }
            }catch(Exception ex)
            {
                return new HttpResult<int>() { Code = 1, Msg = $"{ex.Message}\r\n{ex.StackTrace}", ResultData = 0 };
            }
        }

    }
}
