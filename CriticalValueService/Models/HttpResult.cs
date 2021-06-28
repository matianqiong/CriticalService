using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CriticalValueService.Models
{
    public class HttpResult<T>
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// 状态信息
        /// </summary>
        public string Msg { get; set; }
        /// <summary>
        /// 数据实体
        /// </summary>
        public T ResultData { get; set; }


    }
}
