using System;
using System.Threading;
using System.Threading.Tasks;
using GenshinDailyHelper.Client;
using GenshinDailyHelper.Constant;
using GenshinDailyHelper.Entities;
using GenshinDailyHelper.Exception;

namespace GenshinDailyHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            for(var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var buff = arg.ToCharArray();
                for(var j = 0; j < buff.Length; j++)
                {
                    if (buff[j] == '#' || buff[j] == ';' || buff[j] == '=')
                    {
                    }
                    else
                    {
                        buff[j] = '*';
                    }
                }

                Console.WriteLine("args[" + i.ToString() + "]:" + new string(buff));
            }

            WriteLineUtil.WriteLineLog("开始签到");

            if (args.Length <= 0)
            {
                throw new InvalidOperationException("获取参数不对");
            }

            try
            {
                

                var cookieString = string.Join(' ',args);
                var cookies = cookieString.Split("#");
                var threadArray = new Thread[cookies.Length];

                for (var accountIndex = 0; accountIndex < cookies.Length; accountIndex++)
                {
                    threadArray[accountIndex] = new Thread(ThreadProc);
                    threadArray[accountIndex].Start(new ThreadParameter() { cookie = cookies[accountIndex], accountIndex = accountIndex });
                }

                for (var accountIndex = 0; accountIndex < cookies.Length; accountIndex++)
                {
                    threadArray[accountIndex].Join();
                    WriteLineUtil.WriteLineLog("threadArray[" + accountIndex.ToString() + "]: done");
                }
            }
            catch (GenShinException e)
            {
                WriteLineUtil.WriteLineLog($"请求接口时出现异常{e.Message}");
                Environment.ExitCode = 1;
            }
            catch (System.Exception e)
            {
                WriteLineUtil.WriteLineLog($"出现意料以外的异常{e}");
                Environment.ExitCode = 2;
            }
            //抛出异常主动构建失败
            WriteLineUtil.WriteLineLog("签到结束");
        }


        static async void ThreadProc(object obj)
        {
            var threadParameter = (ThreadParameter)obj;
            var cookie = threadParameter.cookie;
            var accountIndex = threadParameter.accountIndex;




            WriteLineUtil.WriteLineLog($"开始签到 账号{accountIndex}");

            var client = new GenShinClient(
                cookie);

            var rolesResult =
                await client.GetExecuteRequest<UserGameRolesEntity>(Config.GetUserGameRolesByCookie,
                    "game_biz=hk4e_cn");

            //检查第一步获取账号信息
            rolesResult.CheckOutCodeAndSleep();

            int accountBindCount = rolesResult.Data.List.Count;

            WriteLineUtil.WriteLineLog($"账号{accountIndex}绑定了{accountBindCount}个角色");

            for (int i = 0; i < accountBindCount; i++)
            {
                WriteLineUtil.WriteLineLog(rolesResult.Data.List[i].ToString());

                var roles = rolesResult.Data.List[i];

                var signDayResult = await client.GetExecuteRequest<SignDayEntity>(Config.GetBbsSignRewardInfo,
                    $"act_id={Config.ActId}&region={roles.Region}&uid={roles.GameUid}");

                //检查第二步是否签到
                signDayResult.CheckOutCodeAndSleep();

                WriteLineUtil.WriteLineLog(signDayResult.Data.ToString());

                var data = new
                {
                    act_id = Config.ActId,
                    region = roles.Region,
                    uid = roles.GameUid
                };

                var signClient = new GenShinClient(cookie, true);

                var result =
                    await signClient.PostExecuteRequest<SignResultEntity>(Config.PostSignInfo,
                        jsonContent: new JsonContent(data));

                WriteLineUtil.WriteLineLog(result.CheckOutCodeAndSleep());
            }


        }
    }



    class ThreadParameter
    {
        public string cookie;
        public int accountIndex;
    }
}
