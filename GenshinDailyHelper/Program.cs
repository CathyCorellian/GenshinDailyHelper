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

            if (args.Length <= 0)
            {
                throw new InvalidOperationException("获取参数不对");
            }



                var cookieString = string.Join(' ', args);
                var cookies = cookieString.Split("#");
                //var threadArray = new Thread[cookies.Length];
                var taskArray = new Task[cookies.Length];

                for (var accountIndex = 0; accountIndex < cookies.Length; accountIndex++)
                {
                    taskArray[accountIndex] = ThreadProc(accountIndex, cookies[accountIndex]);

                    //threadArray[accountIndex] = new Thread(ThreadProc);
                    //threadArray[accountIndex].Start(new ThreadParameter() { cookie = cookies[accountIndex], accountIndex = accountIndex });
                }



                bool isAnyTaskRunning;
                do
                {
                    isAnyTaskRunning = false;

                    for (var accountIndex = 0; accountIndex < cookies.Length; accountIndex++)
                    {
                        if (taskArray[accountIndex] == null)
                        {
                        }
                        else
                        {
                            if (taskArray[accountIndex].IsCompleted)
                            {
                                WriteLineUtil.WriteLineLog("taskArray[" + accountIndex.ToString() + "]: done");
                                taskArray[accountIndex] = null;
                            }
                            else
                            {
                                isAnyTaskRunning = true;
                            }
                        }
                    }

                    Thread.Sleep(1);
                } while (isAnyTaskRunning);


            //抛出异常主动构建失败
            WriteLineUtil.WriteLineLog("ending...");
        }


        static async Task ThreadProc(int accountIndex, string cookie)
        {
            //var threadParameter = (ThreadParameter)obj;
            //var cookie = threadParameter.cookie;
            //var accountIndex = threadParameter.accountIndex;


            try
            {

                WriteLineUtil.WriteLineLog($"account{accountIndex}: starting");

                var client = new GenShinClient(
                    cookie);

                var rolesResult =
                    await client.GetExecuteRequest<UserGameRolesEntity>(Config.GetUserGameRolesByCookie,
                        "game_biz=hk4e_cn");

                //检查第一步获取账号信息
                rolesResult.CheckOutCodeAndSleep();

                int accountBindCount = rolesResult.Data.List.Count;

                WriteLineUtil.WriteLineLog($"account{accountIndex}: bind {accountBindCount} characters");

                for (int i = 0; i < accountBindCount; i++)
                {
                    var userGameRolesListItem = rolesResult.Data.List[i];

                    WriteLineUtil.WriteLineLog($"Nick:{userGameRolesListItem.Nickname}, Lv:{userGameRolesListItem.Level}, Area:{userGameRolesListItem.RegionName}");

                    var roles = rolesResult.Data.List[i];

                    var signDayResult = await client.GetExecuteRequest<SignDayEntity>(Config.GetBbsSignRewardInfo,
                        $"act_id={Config.ActId}&region={roles.Region}&uid={roles.GameUid}");

                    //检查第二步是否签到
                    signDayResult.CheckOutCodeAndSleep();

                    WriteLineUtil.WriteLineLog($"account{accountIndex}: sign days:{signDayResult.Data.TotalSignDay}, today:{signDayResult.Data.Today}, status:{(signDayResult.Data.IsSign ? "signed" : "not signed")}");

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
            catch (GenShinException e)
            {
                WriteLineUtil.WriteLineLog($"excepting durning requesting interface {e.Message}");
                Environment.ExitCode = 1;
            }
            catch (System.Exception e)
            {
                WriteLineUtil.WriteLineLog($"global exception {e}");
                Environment.ExitCode = 2;
            }


        }
    }

}
