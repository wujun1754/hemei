using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WJP.RepMS;
using System.Data;
using WJP.Util;
using System.Configuration;
using RSACode;
using System.IO;
using Models.EF;

namespace Deducted
{
    class Program
    {

        static void Main(string[] args)
        {
            var reps = RepSessionFactory.CreateRepSession();
            var table = getApply();
            Log.LogWrite("开始执行代扣程序", LogPath.DaiKou);
            if (table != null && table.Rows.Count > 0)
            {
                foreach (DataRow item in table.Rows)
                {
                    var applyID = item.Field<int>("ID");
                    var UserId = item.Field<int>("ApplyUserId");
                    decimal overdueMoney = 0;
                    var payMoney = getBackMoney(applyID, out overdueMoney);
                    if (payMoney - overdueMoney > 2)
                    {
                        var bank = getDefaultBank(UserId);
                        if (bank != null)
                        {
                            var bankID = bank.Field<int>("ID");
                            Log.LogWrite(string.Format("正在执行数据,bankID={0},applyID={1},payMoney={2}", bankID, applyID, payMoney), LogPath.DaiKou, false);
                            //reps.BusApplyBackRep.DoPay(applyID, payMoney, bankID, 1);
                            //payMoney = 1;
                            var result = reps.BusApplyBackRep.DoPayByXieYi(applyID, bankID, payMoney, 1, "", false);
                            if (result.stateType == DoResultType.success)
                            {
                                Log.LogWrite(string.Format("执行成功"), LogPath.DaiKou, false);
                            }
                            else
                            {
                                Log.LogWrite(string.Format("执行失败：{0}", result.stateMsg), LogPath.DaiKou, false);
                            }
                        }
                        else
                        {
                            Log.LogWrite(string.Format("没有获取到用户有效的银行卡,applyID={0},payMoney={1}", applyID, payMoney), LogPath.DaiKou, false);
                            string insertSql = "Insert Into BusinessLog (businessTitle,updateAfter,updateBefore,opResult,opType,opTime,BusId,BusTable,opID,opName,opSource) Values ('活付代扣','','没有获取到用户有效的银行卡ApplyID=" + applyID + "','失败',5,getdate()," + applyID + ",'自动代扣',0,'',0)";
                            Log.LogWrite("insertSql=" + insertSql, LogPath.DaiKou, false);
                            DBExecute.ExecuteInsert(insertSql);
                        }
                    }
                }
            }

            Log.LogWriteEnd(LogPath.DaiKou);

        }


        static DataTable getApply()
        {
            string sql = "Select ID,ApplyUserId From BusApply Where isDel = 0 And Status = 199";
            //string sql = "Select ID,ApplyUserId From BusApply Where ID=221";

            return DBExecute.ExecuteDataTable(sql);
        }

        static decimal getBackMoney(int applyID, out decimal overdueMoney)
        {
            var stageSql = "Select IsNull(Sum(OverPrice),0) as OverPrice From BusApplyBack Where State != 2 And BackTime <= '" + DateTime.Now + "'  And ApplyID = " + applyID;

            var overdueSql = "Select IsNull(Sum(OverPrice),0) as OverPrice From BusApplyOverdue Where Status != 2 And ApplyID = " + applyID;
            var backMoney = TypeHelper.parseDecimal(DBExecute.ExecuteScalar(stageSql));
            overdueMoney = TypeHelper.parseDecimal(DBExecute.ExecuteScalar(overdueSql));
            return backMoney + overdueMoney;
        }

        static DataRow getDefaultBank(int UserId)
        {
            string bankStr = "Select Top 1 ID From BaseEmpBank Where Status = 0 And EmpID=" + UserId + " Order By IsDefault Desc";
            var bank = DBExecute.ExecuteDataTable(bankStr);
            if (bank != null && bank.Rows.Count > 0)
            {
                return bank.Rows[0];
            }
            else
            {
                return null;
            }
        }

        static void AddPay(int ApplyID, decimal BackMoney)
        {
            //RepSession reps = RepSessionFactory.CreateRepSession();
            //var dbContext = DBContextFactory.CreateDBContext();
            //var model = new BusApplyBackInfo();
            //var apply = reps.BusApplyRep.Get(ApplyID);
            //model.BackTime = DateTime.Now;
            //model.BackMoney = BackMoney;
            //model.BackSource = 6;
            //model.BackSourceTitle = "宝付待扣";
            //model.BackType = 1;
            //model.ApplyID = ApplyID;
            //model.HospitalID = apply.HosptialId;
            //model.ApplyUserID = apply.ApplyUserId;
            //model.ApplyUserLogin = apply.ApplyUserPhone;
            //reps.BusApplyBackInfoRep.Add(model);
            //dbContext.SaveChanges();
            //reps.BusApplyBackRep.doBack(ApplyID, BackMoney, DateTime.Now);
        }
    }
}
