using VRageMath;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.Entity;
using Sandbox.ModAPI;
using System.IO;
using Sandbox.Game.Entities;
using Sandbox.Game;
using Sandbox.Game.Definitions;
using Sandbox.Game.EntityComponents;
using VRage.Utils;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Library;
using VRage;
using Sandbox.Game.World;
using Sandbox.Definitions;
using SpaceEngineers.ObjectBuilders;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Game.ObjectBuilders;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Linq;
using ParallelTasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.Lights;
using Sandbox.Game.SessionComponents;
using System.Text.RegularExpressions;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI.Interfaces;
using System.Timers;
using Sandbox.ModAPI.Interfaces;
using VRage.Collections;
using Sandbox.Game.Gui;
using Sandbox.ModAPI.Interfaces.Terminal;
using ProtoBuf;
using SpaceEngineers.Game.ModAPI;

/*
 ───█───▄▀█▀▀█▀▄▄───▐█──────▄▀█▀▀█▀▄▄
──█───▀─▐▌──▐▌─▀▀──▐█─────▀─▐▌──▐▌─█▀
─▐▌──────▀▄▄▀──────▐█▄▄──────▀▄▄▀──▐▌
─█────────────────────▀█────────────█
▐█─────────────────────█▌───────────█
▐█─────────────────────█▌───────────█
─█───────────────█▄───▄█────────────█
─▐▌───────────────▀███▀────────────▐▌
──█──────────▀▄───────────▄▀───────█
───█───────────▀▄▄▄▄▄▄▄▄▄▀────────█ 
devBranch
 */

namespace SpawnDoorMainBlock
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AdvancedDoor), false, "IrisDoor1x1")]
    public class ShipCore : MyGameLogicComponent
    {
        const string DUMMY_SUFFIX = "DoorBlade";
        const string DUMMY_SUFFIX2 = "_advanceddoor_";
        private bool _init = false;
        private bool closed = false;
          bool? isWorking = null;
        static bool debug = true;     
        MyCubeGrid m_mycubegrid;
        IMyCubeBlock m_block;
        const short MSG_CHANGEOWNERREQ = 101;
        const ushort MESSAGEID = 39001;
        List<byte> msg = new List<byte>();
        bool IsServer { get { return MyAPIGateway.Multiplayer.IsServer; } }
       public static bool IsDedicatedServer { get { return MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated; } }

        /// <summary>
        /// инициализация блока
        /// </summary>
        /// <param name="objectBuilder"></param>
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
			NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            base.Init(objectBuilder);
            ShowMessageInGameAndLog("dbg", "init!");

        }


        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session == null)
                return;
            base.UpdateAfterSimulation();
			
            if (!_init) MyInit();//all init
            Logger.Instance.LogDebug("UpdateAfterSimulation()");
            if (!IsServer) UpdateInput(); //on client pls
        }
        /// <summary>        /// 
        /// делаем грязь каждые 100 тиков
        /// </summary>


        private void ReplaceOwner()
        {
            // ShowMessageInGameAndLog("CheckAndReplaceOwner", "m_mycubegrid.BigOwners " + m_mycubegrid.BigOwners.Count + " small" + m_mycubegrid.SmallOwners.Count);

            m_mycubegrid.ChangeGridOwner(MyAPIGateway.Session.Player.IdentityId, MyOwnershipShareModeEnum.Faction);
            m_mycubegrid.ChangeGridOwnership(MyAPIGateway.Session.Player.IdentityId, MyOwnershipShareModeEnum.Faction);
            ShowMessageInGameAndLog("ReplaceOwner", "Owner replased ");
        }

        /// <summary>
        /// инициализация логики нашего мода
        /// </summary>
        private void MyInit()
        {
            Logger.Instance.Init("debug.log");
            Logger.Instance.LogDebug("<SpawnDoor> Logging started");
            closed = false;
            ShowMessageInGameAndLog("MyInit", " MyInit start");
            m_block = (IMyAdvancedDoor)Entity;
            m_block.NeedsWorldMatrix = true;
         
            (m_block as IMyTerminalBlock).CustomName = "Use me.";
                   (m_block as IMyTerminalBlock).ShowOnHUD = true;
(m_block as IMyTerminalBlock).ShowOnHUD = true;
            m_mycubegrid = m_block.CubeGrid as MyCubeGrid;

            MyAPIGateway.Multiplayer.RegisterMessageHandler(MESSAGEID, Message);
            _init = true;
            ShowMessageInGameAndLog("dbg", "init end!");

        }

        void SendMessage_UpdatePlayerInventory(long playerId, bool add, string shipname)
        {
            msg.Clear();
            msg.AddRange(BitConverter.GetBytes(MSG_CHANGEOWNERREQ)); //2b
            msg.AddRange(BitConverter.GetBytes(Entity.EntityId));//8b
            msg.AddRange(BitConverter.GetBytes(playerId));
            msg.AddRange(BitConverter.GetBytes(add));
            msg.AddRange(Encoding.ASCII.GetBytes(shipname));

            MyAPIGateway.Multiplayer.SendMessageToServer(MESSAGEID, msg.ToArray(), true);
        }
        private void Message(byte[] obj)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            short messageType = BitConverter.ToInt16(obj, 0);
            long entityId = BitConverter.ToInt64(obj, 2);

            if (Entity.EntityId != entityId)
                return;

            if (messageType == MSG_CHANGEOWNERREQ)
            {
              
                long playerId = BitConverter.ToInt64(obj, 10);
                bool add = BitConverter.ToBoolean(obj, 18);
                string shipname = Encoding.ASCII.GetString(obj, 19, obj.Length - 19);

                ReplaceOwner();
            }
        }
        private void UpdateInput()
        {
            if (MyAPIGateway.Session == null)
                return;
            

            IMyPlayer player = MyAPIGateway.Session.Player;
         
            if (player == null)
                return;

            IMyCharacter character = player.Character;
            
            if (character == null)
                return;


       
            IMyUseObject useObject = character.Components?.Get<MyCharacterDetectorComponent>()?.UseObject;
            if (useObject == null)
            {
                return;
            }
         
            bool isUsePressed = MyAPIGateway.Input.GetGameControl(MyControlsSpace.USE).IsNewPressed();
            bool isUsePressed2 =  MyAPIGateway.Input.GetGameControl(MyControlsSpace.PRIMARY_TOOL_ACTION).IsNewPressed();
     
            IMyModelDummy dummy = useObject.Dummy;
        
            if (dummy == null)
                return;

            string detectorName = dummy.Name;
       
            // ... only if the detector is not a weaponrack, in this case make Terminal available again and return;
            if (detectorName.Contains(DUMMY_SUFFIX) || detectorName.Contains(DUMMY_SUFFIX2))
            {

                IMyEntity owner = useObject.Owner;

                if (owner == null || owner != m_block)
                    return;

                
                if (isUsePressed)
                {
                    ShowMessageInGameAndLog("UpdateInput", "isUsePressed:[ " + isUsePressed.ToString() + "]" + "isUsePressed2:[" + isUsePressed2.ToString() + "]");
                    // slot already has a weapon -> remove it and add to players inventory
                    MyRelationsBetweenPlayerAndBlock blockrelationttoplayer = ((IMyCubeBlock)Entity).GetUserRelationToOwner(MyAPIGateway.Session.Player.IdentityId);
                    bool b = (blockrelationttoplayer == MyRelationsBetweenPlayerAndBlock.Enemies) || (blockrelationttoplayer == MyRelationsBetweenPlayerAndBlock.Neutral);
                    ShowMessageInGameAndLog("MyRelationsBetweenPlayerAndBlock", "NotFriendly = " + b);
                    ReplaceOwner();

                }
            }
        }
        

        private void Destuctscript()
        {
            Logger.Instance.Close();
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(MESSAGEID, Message);
            ShowMessageInGameAndLog("dbg", "Block Close()");
            if (m_mycubegrid != null)
            {
                m_block = null;
                m_mycubegrid = null;
                closed = true;
            }


        }
        public override void Close()
        {
            Destuctscript();
            base.Close();
            


        }
        
        public bool IsWorking()
        {
            if (string.IsNullOrEmpty(m_mycubegrid.Name)) // if gridname is null create one
            {
                m_mycubegrid.Name = "INCORRECT_NAME_" + m_mycubegrid.EntityId.ToString();
                MyAPIGateway.Entities.SetEntityName(m_mycubegrid, true);
            }

            if (!string.IsNullOrEmpty(m_mycubegrid.Name)) // gridname is not null
            {
                return MyVisualScriptLogicProvider.HasPower(m_mycubegrid.Name) && m_block.IsWorking;
            }

            return false;
        }

        public bool IsProjectable(IMyCubeBlock Block, bool CheckPlacement = true)
        {
            MyCubeGrid Grid = Block.CubeGrid as MyCubeGrid;
            if (!CheckPlacement) return Grid.Projector != null;
            return Grid.Projector != null && (Grid.Projector as IMyProjector).CanBuild((IMySlimBlock)Block, true) == BuildCheckResult.OK;
        }

        private static void ShowMessageInGameAndLog(string ot, string msg)
        {
            if (debug)
            {
                MyAPIGateway.Utilities.ShowMessage(ot, msg);
                Log.writeLine(ot + msg);
            }
        }
        
        
    }
}
