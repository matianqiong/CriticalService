using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CriticalValueService.Models
{
    public class TelRecordEntry
    {
        /// <summary>
        /// 主表序号I
        /// </summary>
        public int MainId { get; set; }
        /// <summary>
        /// 通知人
        /// </summary>
        public int Notifier { get; set; }
        /// <summary>
        /// 通知时间
        /// </summary>
        public string NotifyTime { get; set; }
        /// <summary>
        /// 接收人
        /// </summary>
        public int Accepter { get; set; }
        /// <summary>
        /// 接收时间
        /// </summary>
        public string AcceptTime { get; set; }
        /// <summary>
        /// 接收电话
        /// </summary>
        public string AcceptTel { get; set; }
    }
}
