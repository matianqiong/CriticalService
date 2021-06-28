using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CriticalValueService.Models
{
    public class CriticalMainEntry
    {
        /// <summary>
        /// 患者序号I,病人唯一标识
        /// </summary>
        public int PatiendId { get; set; }
        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 年龄
        /// </summary>
        public int Age { get; set; }
        /// <summary>
        /// 年龄单位
        /// </summary>
        public string AgeUnit { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public string Sex { get; set; }
        /// <summary>
        /// 病人类型
        /// </summary>
        public string PatientType { get; set; }
        /// <summary>
        /// 健康序号I
        /// </summary>
        public int HealthSerial { get; set; }
        /// <summary>
        /// 身份证号
        /// </summary>
        public string IDCard { get; set; }
        /// <summary>
        /// 申请科室I
        /// </summary>
        public int ApplyDepId { get; set; }
        /// <summary>
        /// 申请医生R
        /// </summary>
        public int ApplyDoctorId { get; set; }
        /// <summary>
        /// 危急否B，0：否，1：是 
        /// </summary>
        public int IsCritical { get; set; }
        /// <summary>
        /// 急诊否B，0：否，1：是 
        /// </summary>
        public int IsEmergency { get; set; }
        /// <summary>
        /// 标本类型
        /// </summary>
        public string SampleType { get; set; }
        /// <summary>
        /// 报告时间
        /// </summary>
        public string AuditTime { get; set; }
        /// <summary>
        /// 生理周期
        /// </summary>
        public string MenstrualCycle { get; set; }
        /// <summary>
        /// 就餐情况
        /// </summary>
        public string DingSituation { get; set; }
        /// <summary>
        /// 主任护士R
        /// </summary>
        public int MainNurse { get; set; }
        /// <summary>
        /// 主任医生R
        /// </summary>
        public int MainDoctor { get; set; }
        /// <summary>
        /// 科室主任R
        /// </summary>
        public int DeptDiretor { get; set; }
        /// <summary>
        /// 医务科序号I
        /// </summary>
        public int MedicalDeptment { get; set; }
        /// <summary>
        /// 业务类别N：:1,'检验',2,'超声',3,'放射',4,'心电',5,'病理',6,'其他'
        /// </summary>
        public int BusinessClass { get; set; }
        /// <summary>
        /// 危急值
        /// </summary>
        public string CriticalValueInfo { get; set; }
        /// <summary>
        /// 诊断信息
        /// </summary>
        public string DiagonsisInfo { get; set; }
        /// <summary>
        /// 申请序号i
        /// </summary>
        public string ApplyId { get; set; }
        /// <summary>
        /// 组合序号
        /// </summary>
        public string GroupIds { get; set; }
        /// <summary>
        /// 隶属机构i
        /// </summary>
        public int OrgId { get; set; }
        /// <summary>
        /// 报告审核人R
        /// </summary>
        public int Auditor { get; set; }
        /// <summary>
        /// 危急值上报科室i
        /// </summary>
        public int ReportPdt
        {
            get; set;

        }
    }
}
