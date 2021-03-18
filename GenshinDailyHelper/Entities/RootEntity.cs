using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GenshinDailyHelper.Entities
{
    /// <summary>
    /// 返回头部信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RootEntity<T>
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("retcode")]
        public int Retcode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("data")]
        public T Data { get; set; } = Activator.CreateInstance<T>();

        /// <summary>
        /// 判断返回码并延迟
        /// </summary>
        /// <returns></returns>
        public string CheckOutCode()
        {

            return Retcode switch
            {
                0 => "执行成功",
                -5003 => $"{Message}",
                _ => throw new System.Exception($"请求异常, Retcode: ${Retcode}, Message: ${Message}")
            };
        }

        public override string ToString()
        {
            return $"返回码为{Retcode}";
        }
    }
}
