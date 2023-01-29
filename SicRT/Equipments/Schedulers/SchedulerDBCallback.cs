using System;
using Aitex.Core.Common;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Jobs;
using MECF.Framework.Common.SubstrateTrackings;

namespace SicRT.Equipments.Schedulers
{

    class SchedulerDBCallback 
    {
        public void LotCreated(ControlJobInfo cj)
        {
            ModuleName module = ModuleHelper.Converter(cj.Module);
            Guid carrierGuid = CarrierManager.Instance.GetCarrier(cj.Module).InnerId;

            LotDataRecorder.StartLot(cj.LotInnerId.ToString(), carrierGuid.ToString(), "", cj.LotName, cj.Module, cj.Module, cj.LotWafers.Count);

            foreach (var waferInfo in cj.LotWafers)
            {
                LotDataRecorder.InsertLotWafer(cj.LotInnerId.ToString(), waferInfo.InnerId.ToString());

                WaferDataRecorder.SetWaferSequence(waferInfo.InnerId.ToString(), waferInfo.ProcessJobID);
            }
        }


        public void LotFinished(ControlJobInfo cj)
        {

            int unprocessed = 0;
            int aborted = 0;
            foreach (var waferInfo in cj.LotWafers)
            {
                if (waferInfo.ProcessState == EnumWaferProcessStatus.Failed)
                {
                    aborted++;
                    continue;
                }

                if (waferInfo.ProcessState != EnumWaferProcessStatus.Completed)
                {
                    unprocessed++;
                    continue;
                }

            }

            LotDataRecorder.EndLot(cj.LotInnerId.ToString(), aborted, unprocessed);
        }
    }
}
