using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using Sandbox.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRage;
using Sandbox.Game.Entities;

namespace avaness.AntiWheelPop
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MotorSuspension), false)]
    class WheelComp : MyGameLogicComponent
    {
        IMyMotorSuspension wheel;
        IMyCubeGrid grid;
        MyObjectBuilder_CubeGrid builder;
        MyCubeBlockDefinition definition;
        //Vector3D pos;
        bool listening;
        ITerminalAction addWheel;

        public override void Init (MyObjectBuilder_EntityBase objectBuilder)
        {
            wheel = Entity as IMyMotorSuspension;
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void Close ()
        {
            if (wheel.TopGrid != null)
                wheel.TopGrid.OnClose -= TopGrid_OnClose;
            NeedsUpdate = MyEntityUpdateEnum.NONE;
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

            if (wheel.TopGrid != null)
            {
                wheel.TopGrid.OnClose += TopGrid_OnClose;
                GetOB(wheel.TopGrid);
                listening = true;
            }
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        private void TopGrid_OnClose (IMyEntity e)
        {
            e.OnClose -= TopGrid_OnClose;
            listening = false;
            if (!wheel.MarkedForClose && !wheel.Closed)
            {
                if (builder == null)
                    addWheel.Apply(wheel);
                else
                    SpawnWheel();
            }
        }

        public override void UpdateBeforeSimulation ()
        {
            if(wheel.TopGrid == null) // Needs new wheel
            {
                if(grid != null) // Wheel has been spawned
                    wheel.Attach();
            }
            else // Has wheel
            {
                if(!listening)
                {
                    wheel.TopGrid.OnClose += TopGrid_OnClose;
                    listening = true;
                }

                grid = null;
                if (builder == null) // No object builder exists yet
                    GetOB(wheel.TopGrid);
            }
        }

        private void GetOB (IMyCubeGrid grid)
        {
            builder = (MyObjectBuilder_CubeGrid)grid.GetObjectBuilder(true);
            foreach(var cube in ((MyCubeGrid)grid).GetFatBlocks())
            {
                if (cube is IMyAttachableTopBlock)
                {
                    definition = cube.BlockDefinition;
                    return;
                }
            }
        }

        private void SpawnWheel ()
        {
            MatrixD topGridMatrix = GetTopGridMatrix();
            if (definition != null && definition.Center != Vector3.Zero)
                topGridMatrix.Translation = Vector3D.Transform(-definition.Center * MyDefinitionManager.Static.GetCubeSize(definition.CubeSize), topGridMatrix);
            builder.PositionAndOrientation = new MyPositionAndOrientation(topGridMatrix);
            grid = (IMyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(builder);
            if (grid != null)
            {
                builder = null;
                grid.OnClose += TopGrid_OnClose;
                listening = true;
                wheel.Attach();
            }
        }

        protected MatrixD GetTopGridMatrix()
        {
            Vector3D position = Vector3D.Transform(wheel.DummyPosition + wheel.LocalMatrix.Forward * (float)wheel.Height, wheel.CubeGrid.WorldMatrix);
            MatrixD worldMatrix = wheel.WorldMatrix;
            Vector3D forward = worldMatrix.Forward;
            worldMatrix = wheel.WorldMatrix;
            Vector3D up = worldMatrix.Up;
            return MatrixD.CreateWorld(position, forward, up);
        }
    }
}
