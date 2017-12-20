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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AdvancedDoor), false, "SpawnIrisDoor1x1")]
    public class ShipCore : MyGameLogicComponent
    {
        const string DUMMY_SUFFIX = "DoorBlade";
        const string DUMMY_SUFFIX2 = "_advanceddoor_";
        const short MSG_CHANGEOWNERREQ = 101;
        const ushort MESSAGEID = 39001;
        const string secretword = "·";
        static bool debug = false;

        private bool _init = false;
        private bool closed = false;
        private bool? isWorking = null;
        private bool needdestuct = false;
        private MyCubeGrid m_mycubegrid;
        private IMyCubeBlock m_block;
        private List<byte> msg = new List<byte>();
        private IMyAdvancedDoor spawnIMyAdvancedDoor;
        MyObjectBuilder_EntityBase m_objectBuilder = null;
        private static readonly List<IMyPlayer> _playerCache = new List<IMyPlayer>();
        public List<SerializableDefinitionId> InvDefinitionIds = new List<SerializableDefinitionId>();
        private bool IsServer { get { return MyAPIGateway.Multiplayer.IsServer; } }
        private bool IsDedicatedServer { get { return MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated; } }

   
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false) => m_objectBuilder;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_objectBuilder = objectBuilder;
            spawnIMyAdvancedDoor = Container.Entity as IMyAdvancedDoor;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            if ((!IsDedicatedServer) && ((spawnIMyAdvancedDoor.CustomData == secretword))) NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            // ShowMessageInGameAndLog("Init", "end. NeedsUpdate: " + NeedsUpdate);
        }

        public override void UpdateAfterSimulation100()
        {
            if ((MyAPIGateway.Session == null) || (MyAPIGateway.Utilities == null))
                return;
            // ShowMessageInGameAndLog("UpdateAfterSimulation100", "1.");
            if (spawnIMyAdvancedDoor?.CustomData != secretword) return;
            // ShowMessageInGameAndLog("UpdateAfterSimulation100", "2.");
            if (spawnIMyAdvancedDoor.CubeGrid.Physics == null) return;
           // ShowMessageInGameAndLog("UpdateAfterSimulation100", "before Init.");
            if (!_init) MyInit();//all init
        }
        public override void UpdateAfterSimulation()
        {
            if ((MyAPIGateway.Session == null) || (MyAPIGateway.Utilities == null))
                return;
            //ShowMessageInGameAndLog("UpdateAfterSimulation", "1.");
            if (spawnIMyAdvancedDoor?.CustomData != secretword) return;
            //ShowMessageInGameAndLog("UpdateAfterSimulation", "2.");


            //if (Entity.Flags == EntityFlags.ShadowBoxLod || Entity.Flags == EntityFlags.Transparent) return;

            //ShowMessageInGameAndLog("UpdateAfterSimulation", "before phys.");
            if (spawnIMyAdvancedDoor.CubeGrid.Physics == null) return;
            //ShowMessageInGameAndLog("UpdateAfterSimulation", "before Init.");
            if (!_init) MyInit();//all init
            if (!IsServer) UpdateInput(); //on client pls
        }



        private void ReplaceOwner(long owner)
        {
            m_mycubegrid?.ChangeGridOwner(owner, MyOwnershipShareModeEnum.Faction);
            m_mycubegrid?.ChangeGridOwnership(owner, MyOwnershipShareModeEnum.Faction);
            ShowMessageInGameAndLog("ReplaceOwner", "Owner replaced ");
        }

        /// <summary>
        /// инициализация логики нашего мода
        /// </summary>
        private void MyInit()
        {Logger.Instance.Init("debug");
            ShowMessageInGameAndLog("MyInit", "start.");
            if (IsDedicatedServer) MyAPIGateway.Multiplayer.RegisterMessageHandler(MESSAGEID, Message);
            if (!IsDedicatedServer) NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            closed = false;
            m_block = (IMyAdvancedDoor)Entity;
            m_mycubegrid = m_block.CubeGrid as MyCubeGrid;


            _init = true;


            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            m_block.CubeGrid.GetBlocks(blocks);
            bool flag = false;
            foreach (var block in blocks)
            {
                if (IsProjectable(m_mycubegrid, block as IMyCubeBlock))
                    flag = true;
            }
            if (flag)
            {
                ShowMessageInGameAndLog("", "projectable: " + flag);
                if (IsDedicatedServer) NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
                _init = false;
                //Destuctscript();
                return;
            }

            m_block.NeedsWorldMatrix = true;

            (m_block as IMyTerminalBlock).CustomName = "Use me.";
            (m_block as IMyTerminalBlock).ShowOnHUD = true;




            ShowMessageInGameAndLog("MyInit", "end.");

        }

        void SendMessage_ReplaceDoor(long playerId, bool add, string shipname)
        {
            msg.Clear();
            msg.AddRange(BitConverter.GetBytes(MSG_CHANGEOWNERREQ)); //2b
            msg.AddRange(BitConverter.GetBytes(Entity.EntityId));//8b
            msg.AddRange(BitConverter.GetBytes(playerId));
            msg.AddRange(BitConverter.GetBytes(add));
            msg.AddRange(Encoding.ASCII.GetBytes(shipname));

            MyAPIGateway.Multiplayer.SendMessageToServer(MESSAGEID, msg.ToArray(), true);
            ShowMessageInGameAndLog("SendMessage_ReplaceDoor", "sended.");
        }
        private void Message(byte[] obj)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            try
            {
                ShowMessageInGameAndLog("Message", "start.");


                short messageType = BitConverter.ToInt16(obj, 0);
                long entityId = BitConverter.ToInt64(obj, 2);

                if (Entity.EntityId != entityId)
                    return;

                if (messageType == MSG_CHANGEOWNERREQ)
                {



                    m_block = (IMyAdvancedDoor)Entity;
                    m_mycubegrid = m_block.CubeGrid as MyCubeGrid;



                    long playerId = BitConverter.ToInt64(obj, 10);
                    ShowMessageInGameAndLog("Message", "got message from " + playerId.ToString());
                    bool add = BitConverter.ToBoolean(obj, 18);
                    string shipname = Encoding.ASCII.GetString(obj, 19, obj.Length - 19);

                    //ShowMessageInGameAndLog("Message", " 1");

                    IMyPlayer player = GetPlayerById(playerId);
                    List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                    (m_mycubegrid as IMyCubeGrid).GetBlocks(blocks);
                    if (blocks.Count < 54) return;
                    Vector3 forvarddirection = new Vector3(0f, 0f, 0f);
                    IMyCockpit FoundCockpit = null;
                    IMyShipMergeBlock FoundMerge = null;
                    IMyThrust FoundTrust = null;
                    IMyShipMergeBlock FoundBaseMergeBlock = null;
                    IMyProjector FoundBaseProjector = null;
                    IMyCargoContainer FoundCargoCont = null;
                    IMyParachute FoundParachute = null;
                    IMyParachute FoundParachute2 = null;
                    IMyBeacon FoundMyBeacon = null;
                    ShowMessageInGameAndLog("Message", " blocks.Count: " + blocks.Count);

                    foreach (IMySlimBlock blok in blocks)
                    {
                        if (blok.FatBlock == null) continue;
                        ShowMessageInGameAndLog("Message", " Block: " + blok.FatBlock.Name + "display nametext: " + blok.FatBlock.DisplayNameText);

                        if (blok.FatBlock.DisplayNameText == "MyMergeBlock")
                        {

                            FoundMerge = (IMyShipMergeBlock)blok.FatBlock;
                            ShowMessageInGameAndLog("Message", " found MyMergeBlock");
                            continue;
                        }

                        if (blok.FatBlock.DisplayNameText == "MyParachute")
                        {
                            FoundParachute = (IMyParachute)blok.FatBlock;
                            ShowMessageInGameAndLog("Message", " found FoundParachute");
                            continue;
                        }

                        if (blok.FatBlock.DisplayNameText == "MyParachute2")
                        {
                            FoundParachute2 = (IMyParachute)blok.FatBlock;
                            ShowMessageInGameAndLog("Message", " found FoundParachute");
                            continue;
                        }

                        if (blok.FatBlock.DisplayNameText == "MyCockpit")
                        {
                            FoundCockpit = (IMyCockpit)blok.FatBlock;
                            ShowMessageInGameAndLog("Message", " found MyCockpit");
                            continue;
                        }


                        if (blok.FatBlock.DisplayNameText == "MyThrust")
                        {
                            FoundTrust = (IMyThrust)blok.FatBlock;
                            ShowMessageInGameAndLog("Message", " found MyThrust");
                            continue;
                        }


                        if (blok.FatBlock.DisplayNameText == "BaseMergeBlock")
                        {
                            FoundBaseMergeBlock = (IMyShipMergeBlock)blok.FatBlock;
                            ShowMessageInGameAndLog("Message", " found BaseMergeBlock");
                            continue;
                        }




                        if (blok.FatBlock.DisplayNameText == "BaseProjector")
                        {
                            FoundBaseProjector = (IMyProjector)blok.FatBlock;
                            ShowMessageInGameAndLog("Message", " found BaseProjector");
                            continue;
                        }

                        if (blok.FatBlock.DisplayNameText == "MyCargo")
                        {
                            FoundCargoCont = (IMyCargoContainer)blok.FatBlock;
                            ShowMessageInGameAndLog("Message", " found MyCargo");
                            continue;
                        }

                        if (blok.FatBlock.DisplayNameText == "MyBeacon")
                        {
                            FoundMyBeacon = (IMyBeacon)blok.FatBlock;
                            ShowMessageInGameAndLog("Message", " found MyBeacon");
                            continue;
                        }

                    }

                    ShowMessageInGameAndLog("Message", " fFoundCockpit" + (FoundCockpit != null) + (FoundMerge != null) + (FoundTrust != null) + (m_block.CubeGrid.CustomName == shipname));

                    if (FoundMyBeacon != null && FoundCargoCont != null && FoundCockpit != null && FoundMerge != null && FoundTrust != null && FoundParachute != null && FoundParachute2 != null
                        && m_block.CubeGrid.CustomName == shipname)
                    {
                        (m_block as IMyAdvancedDoor).OpenDoor();
                        (m_block as IMyAdvancedDoor).CustomData = "";
                        // (m_block as IMyAdvancedDoor).CustomName = "Use me.";
                        // (m_block as IMyAdvancedDoor).ShowOnHUD = true;

                        forvarddirection = FoundTrust.WorldMatrix.Backward;
                        //ShowMessageInGameAndLog("Message", " findedthrust vector");

                        ////Started resourses
                        
                        SerializableDefinitionId Item_Hatch = new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "Canvas");
                         ShowMessageInGameAndLog("Message", " before add to conteiner");
                        addtoContainer2(FoundCargoCont, new MyObjectBuilder_AmmoMagazine { SubtypeName = "NATO_5p56x45mm", ProjectilesCount = 50 }, 2);

                        addtoContainer2(FoundCargoCont, this.CreateGunContent("AutomaticRifleItem"), 1);
                        addtoContainer2(FoundCargoCont, this.CreateGunContent("WelderItem"), 2);
                        addtoContainer2(FoundCargoCont, this.CreateGunContent("AngleGrinderItem"), 1);
                        addtoContainer2(FoundCargoCont, this.CreateGunContent("HandDrillItem"), 1);

                      
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Ore), "Ice"), 1000);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "SteelPlate"), 886);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "Construction"), 203);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "InteriorPlate"), 225);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "MetalGrid"), 90);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "RadioCommunication"), 48);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "Computer"), 12);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "LargeTube"), 39);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "Motor"), 60);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "Display"), 12);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "Medical"), 18);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "SmallTube"), 110);
                        addtoContainer(FoundCargoCont, new SerializableDefinitionId(typeof(MyObjectBuilder_Component), "PowerCell"), 110);

                        addtoParachute(FoundParachute, Item_Hatch, 1);
                        addtoParachute(FoundParachute2, Item_Hatch, 1);
                        //

                        FoundMerge.Enabled = false;

                        ReplaceOwner(playerId);

                        player.Character.SetPosition(FoundCockpit.GetPosition());
                        FoundCockpit.AttachPilot(player.Character);
                      
                        (FoundBaseMergeBlock as MyCubeBlock)?.ChangeOwner(144115188075855876, MyOwnershipShareModeEnum.None);//TODO: change it to nps ID
                        (FoundBaseProjector as MyCubeBlock)?.ChangeOwner(144115188075855876, MyOwnershipShareModeEnum.None);

                        (m_mycubegrid as IMyEntity).Physics.LinearVelocity = forvarddirection * 200;
                        FoundMyBeacon.CustomName = "I'm noob ! ;3 ";

                        FoundMyBeacon.Enabled = true;







                    }
                    ShowMessageInGameAndLog("Message", " end");
                    // NeedsUpdate = MyEntityUpdateEnum.NONE;
                    //ShowMessageInGameAndLog("Message", "end.");
                    //Destuctscript();



                }
            }
            catch { ShowMessageInGameAndLog("Message", "EXEPTION! "); }
        }
        private void addtoContainer(IMyCargoContainer FoundCargoCont, SerializableDefinitionId item, int amount)
        {
            IMyInventory inventory = ((Sandbox.ModAPI.Ingame.IMyTerminalBlock)FoundCargoCont).GetInventory(0) as IMyInventory;
            if (!inventory.CanItemsBeAdded(amount, item))
            {
                return;
            }
            MyFixedPoint amount2 = (MyFixedPoint)Math.Min(amount, 9999);
            inventory.AddItems(amount2, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(item));

        }
        private void addtoContainer2(IMyCargoContainer FoundCargoCont, MyObjectBuilder_PhysicalObject item, int amount)
        {
            IMyInventory inventory = ((Sandbox.ModAPI.Ingame.IMyTerminalBlock)FoundCargoCont).GetInventory(0) as IMyInventory;                    
            inventory.AddItems(amount, item);
        }
        private MyObjectBuilder_PhysicalGunObject CreateGunContent(string subtypeName)
        {
            MyDefinitionId v = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), subtypeName);
            return (MyObjectBuilder_PhysicalGunObject)MyObjectBuilderSerializer.CreateNewObject(v);
        }
        private void addtoParachute(IMyParachute FoundCargoCont, SerializableDefinitionId item, int amount)
        {
            IMyInventory inventory = ((Sandbox.ModAPI.Ingame.IMyTerminalBlock)FoundCargoCont).GetInventory(0) as IMyInventory;
            if (!inventory.CanItemsBeAdded(amount, item))
            {
                return;
            }
            MyFixedPoint amount2 = (MyFixedPoint)Math.Min(amount, 9999);
            inventory.AddItems(amount2, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(item));

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
            bool isUsePressed2 = MyAPIGateway.Input.GetGameControl(MyControlsSpace.PRIMARY_TOOL_ACTION).IsNewPressed();

            IMyModelDummy dummy = useObject.Dummy;

            if (dummy == null)
                return;

            string detectorName = dummy.Name;

            // ... only if the detector is not a Spawndoor, in this case make Terminal available again and return;
            if (detectorName.Contains(DUMMY_SUFFIX) || detectorName.Contains(DUMMY_SUFFIX2))
            {

                IMyEntity owner = useObject.Owner;

                if (owner == null || owner != m_block)
                    return;


                if ((isUsePressed || isUsePressed2) && CheckReadynessClient())
                {
                    try
                    {





                        ShowMessageInGameAndLog("UpdateInput", "isUsePressed:[ " + isUsePressed.ToString() + "]" + "isUsePressed2:[" + isUsePressed2.ToString() + "]");
                        // slot already has a weapon -> remove it and add to players inventory
                        MyRelationsBetweenPlayerAndBlock blockrelationttoplayer = ((IMyCubeBlock)Entity).GetUserRelationToOwner(MyAPIGateway.Session.Player.IdentityId);
                        bool b = (blockrelationttoplayer == MyRelationsBetweenPlayerAndBlock.Enemies) || (blockrelationttoplayer == MyRelationsBetweenPlayerAndBlock.Neutral);

                        ShowMessageInGameAndLog("MyRelationsBetweenPlayerAndBlock", "NotFriendly = " + b);

                        if (b)
                        {
                            // ShowMessageInGameAndLog("UpdateInput", " ReplaceOwner before!");
                            // ReplaceOwner(player.IdentityId);
                            ShowMessageInGameAndLog("UpdateInput", "Try send msg to server ");
                            SendMessage_ReplaceDoor(MyAPIGateway.Session.Player.IdentityId, true, m_block.CubeGrid.CustomName);
                            //(m_block as IMyAdvancedDoor).OpenDoor();
                            NeedsUpdate = MyEntityUpdateEnum.NONE;
                            Destuctscript();
                        }
                        else (m_block as IMyAdvancedDoor).OpenDoor();

                    }
                    catch (Exception tmp) { ShowMessageInGameAndLog("UpdateInput", "EXEPTION! " + tmp?.ToString()); }
                }
            }
        }


        private void Destuctscript()
        {
            NeedsUpdate = MyEntityUpdateEnum.NONE;
            ShowMessageInGameAndLog("Destuctscript", "start");

            MyAPIGateway.Multiplayer.UnregisterMessageHandler(MESSAGEID, Message);

            if (m_mycubegrid != null)
                m_mycubegrid = null;
            if (m_block != null) m_block = null;

            needdestuct = false;
            closed = true;

        }
        public override void Close()
        {
            ShowMessageInGameAndLog("Close", "start");

            Destuctscript();
         //  Logger.Instance.Close();
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

        public bool IsProjectable(MyCubeGrid Grid, IMyCubeBlock Block)
        {
            /*  if (Block == null) return true;
              if (Block.CubeGrid == null) return true;
              if ((Grid.Projector as IMyProjector) == null) return false;
              if ((IMySlimBlock)Block == null) {
                  ShowMessageInGameAndLog("IsProjectable", "(IMySlimBlock)Block == null");
                  return false; }
             */
            var a = Grid.Projector != null && (Grid.Projector as IMyProjector).CanBuild((IMySlimBlock)Block, true) == BuildCheckResult.OK;
            return a;
        }

        private void ShowMessageInGameAndLog(string ot, string msg)
        {
            if (debug)
            {

                //MyAPIGateway.Utilities.ShowNotification(msg, 1000);
                var server = IsDedicatedServer;
                if (!server) MyAPIGateway.Utilities.ShowMessage(ot, msg);
                Logger.Instance.LogMessage("[" + server + "]" + ot + msg);
            }
        }
        public static IMyPlayer GetPlayerById(long identityId)
        {
            _playerCache.Clear();
            MyAPIGateway.Players.GetPlayers(_playerCache);
            return _playerCache.FirstOrDefault(p => p.IdentityId == identityId);
        }
        private bool CheckReadynessClient()
        {
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();

            (m_mycubegrid as IMyCubeGrid).GetBlocks(blocks);

            if (blocks.Count < 54) return false;


            ShowMessageInGameAndLog("Message", " blocks.Count: " + blocks.Count);
            bool flag = false;
            foreach (IMySlimBlock blok in blocks)
            {
                if (blok.FatBlock == null) continue;
                ShowMessageInGameAndLog("Message", " Block: " + blok.FatBlock.Name + "display nametext: " + blok.FatBlock.DisplayNameText);

                if (blok.FatBlock.DisplayNameText == "MyMergeBlock")
                {
                    flag = true;
                }

                if (blok.FatBlock.DisplayNameText == "MyParachute")
                {
                  flag = true;
                }

                if (blok.FatBlock.DisplayNameText == "MyParachute2")
                {
                    flag = true;
                }

                if (blok.FatBlock.DisplayNameText == "MyCockpit")
                {
                    flag = true;
                }


                if (blok.FatBlock.DisplayNameText == "MyThrust")
                {
                    flag = true;
                }


                if (blok.FatBlock.DisplayNameText == "BaseMergeBlock")
                {
                    flag = true;
                }




                if (blok.FatBlock.DisplayNameText == "BaseProjector")
                {
                    flag = true;
                }

                if (blok.FatBlock.DisplayNameText == "MyCargo")
                {
                    flag = true;
                }

                if (blok.FatBlock.DisplayNameText == "MyBeacon")
                {
                    flag = true;
                }


            }


            if (flag) {
                ShowMessageInGameAndLog("Message", "CheckReadynessClient true");
                return true; } return false;




        }
    }
}
