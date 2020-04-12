using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace AntiWheelPop
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorSuspension), false)]
    class WheelComp : MyGameLogicComponent
    {
        IMyMotorSuspension wheel;
        ITerminalAction addWheel;
        float spawnHeight;
        int stage;
        int runtime;


        public override void Init (MyObjectBuilder_EntityBase objectBuilder)
        {
            wheel = Entity as IMyMotorSuspension;
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateBeforeSimulation100 ()
        {
            if (MyAPIGateway.Session == null || !MyAPIGateway.Session.IsServer)
                return;

            if (wheel.CubeGrid?.Physics == null)
            {
                NeedsUpdate = MyEntityUpdateEnum.NONE;
                return;
            }

            List<ITerminalAction> actions = new List<ITerminalAction>(1);
            MyAPIGateway.TerminalActionsHelper.GetActions(wheel.GetType(), actions, (a) => a.Id == "Add Top Part");
            if (actions.Count > 0)
                addWheel = actions [0];
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }


        public override void UpdateBeforeSimulation ()
        {
            if(addWheel != null)
            {
                if(wheel.IsAttached)
                {
                    if (stage == 1)
                        wheel.Height = spawnHeight;
                    stage = 0;
                    runtime = 0;

                }
                else if (!wheel.PendingAttachment)
                {
                    if(stage == 0)
                    {
                        spawnHeight = wheel.Height;
                        if (wheel.CubeGrid.GridSize == 0.5)
                            wheel.Height = 0.26f;
                        else
                            wheel.Height = 1.3f;
                        addWheel.Apply(wheel);
                        stage = 1;
                    }
                    else if (stage == 1)
                    {
                        wheel.Height = spawnHeight;
                        addWheel.Apply(wheel);
                        stage = 2;
                    }
                }
            }
        }

        public override void UpdateAfterSimulation100 ()
        {
            runtime++;
            if(runtime < 15 && stage == 2)
                addWheel.Apply(wheel);
        }
    }
}
