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
            //for(var i = 0; i < args.Length; i++)
            //{
            //    var arg = args[i];
            //    var buff = arg.ToCharArray();
            //    for(var j = 0; j < buff.Length; j++)
            //    {
            //        if (buff[j] == '#' || buff[j] == ';' || buff[j] == '=')
            //        {
            //        }
            //        else
            //        {
            //            buff[j] = '*';
            //        }
            //    }

            //    Console.WriteLine("args[" + i.ToString() + "]:" + new string(buff));
            //}

            var cookies = string.Join(' ', args).Split("#");
            var taskArray = new Task<int>[cookies.Length];

            for (var taskIndex = 0; taskIndex < taskArray.Length; taskIndex++)
            {
                WriteLineUtil.WriteLineLog($"task{taskIndex}: scheduling");
                taskArray[taskIndex] = TaskProc(taskIndex, cookies[taskIndex]);
                WriteLineUtil.WriteLineLog($"task{taskIndex}: scheduled");
            }


            var returnCodeSum = 0;

            while(true)
            {
                var isAnyTaskRunning = false;

                for (var taskIndex = 0; taskIndex < taskArray.Length; taskIndex++)
                {
                    if (taskArray[taskIndex] == null)
                    {
                    }
                    else
                    {
                        if (taskArray[taskIndex].IsCompleted)
                        {
                            var taskResult = taskArray[taskIndex].Result;
                            returnCodeSum += taskResult;

                            WriteLineUtil.WriteLineLog($"task{taskIndex}: done with return code {taskResult}");
                            taskArray[taskIndex] = null;
                        }
                        else
                        {
                            isAnyTaskRunning = true;
                        }
                    }
                }

                if (!isAnyTaskRunning)
                    break;

                Thread.Sleep(10);
            }

            WriteLineUtil.WriteLineLog("exiting");
            Environment.ExitCode = returnCodeSum;
        }


        static async Task<int> TaskProc(int taskIndex, string cookie)
        {
            await Task.Yield();

            try
            {
                WriteLineUtil.WriteLineLog($"task{taskIndex}: GetExecuteRequest<UserGameRolesEntity>");

                var client = new GenShinClient(
                    cookie);

                var rolesResult =
                    await client.GetExecuteRequest<UserGameRolesEntity>(Config.GetUserGameRolesByCookie,
                        "game_biz=hk4e_cn");

                //检查第一步获取账号信息
                rolesResult.CheckOutCode();

                int accountBindCount = rolesResult.Data.List.Count;

                WriteLineUtil.WriteLineLog($"task{taskIndex}: bind {accountBindCount} characters");

                for (int i = 0; i < accountBindCount; i++)
                {
                    var userGameRolesListItem = rolesResult.Data.List[i];

                    WriteLineUtil.WriteLineLog($"task{taskIndex}: Nick:{userGameRolesListItem.Nickname}, Lv:{userGameRolesListItem.Level}, Area:{userGameRolesListItem.RegionName}");
                    
                    var roles = rolesResult.Data.List[i];

                    var signDayResult = await client.GetExecuteRequest<SignDayEntity>(Config.GetBbsSignRewardInfo,
                        $"act_id={Config.ActId}&region={roles.Region}&uid={roles.GameUid}");

                    //检查第二步是否签到
                    signDayResult.CheckOutCode();

                    WriteLineUtil.WriteLineLog($"task{taskIndex}: sign days:{signDayResult.Data.TotalSignDay}, today:{signDayResult.Data.Today}, status:{(signDayResult.Data.IsSign ? "signed" : "not signed")}");

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

                    WriteLineUtil.WriteLineLog($"task{taskIndex}: {result.CheckOutCode()}");
                }

                return 0;
            }
            catch (GenShinException e)
            {
                WriteLineUtil.WriteLineLog($"excepting durning requesting interface {e.ToString()}");
                return 1;
            }
            catch (System.Exception e)
            {
                WriteLineUtil.WriteLineLog($"global exception {e.ToString()}");
                return 2;
            }

        }
    }

}
