using System;
using System.IO;
using System.IO.Streams;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Timers;
using Terraria;
using Terraria.DataStructures;
using TerrariaApi.Server;
using TShockAPI;

namespace BuildMode
{
	[ApiVersion(2, 1)]
	public class BuildMode : TerrariaPlugin
	{
		public override string Author
		{
			get { return "MarioE"; }
		}
		public override string Description
		{
			get { return "Adds a building command."; }
		}
		public override string Name
		{
			get { return "BuildMode"; }
		}
	        Timer Timer = new Timer(1000);
		public override Version Version
		{
			get { return Assembly.GetExecutingAssembly().GetName().Version; }
		}

		public BuildMode(Main game)
			: base(game)
		{
			Order = 10;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Timer.Dispose();

				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
				ServerApi.Hooks.NetSendBytes.Deregister(this, OnSendBytes);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
			}
		}
		public override void Initialize()
		{
			Commands.ChatCommands.Add(new Command("buildmode", BuildModeCmd, "buildmode"));
			Commands.ChatCommands.Add(new Command("buildmode", BMCheck, "bmcheck"));

			ServerApi.Hooks.NetGetData.Register(this, OnGetData);
			ServerApi.Hooks.NetSendBytes.Register(this, OnSendBytes);
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);

			Timer.Elapsed += OnElapsed;
			Timer.Start();
		}

		void OnGreet(GreetPlayerEventArgs args)
		{
			var plr = TShock.Players[args.Who];

			if (plr == null || !plr.Active)
				return;

			plr.SetData("buildmode", false);
		}
		void OnElapsed(object sender, ElapsedEventArgs e)
		{
			for (int i = 0; i < TShock.Players.Length; i++)
			{
				if (TShock.Players[i] == null || !TShock.Players[i].Active || !TShock.Players[i].IsLoggedIn)
					continue;

				if (TShock.Players[i].GetData<bool>("buildmode"))
				{
					Player plr = Main.player[i];
					TSPlayer tsplr = TShock.Players[i];

					if (plr.hostile)
					{
						tsplr.SendErrorMessage("You cannot use Buildmode when PvP is active!");
						TShock.Players[i].SetData("buildmode", false);
						return;
					}

					NetMessage.SendData((int)PacketTypes.WorldInfo, i);
					if (plr.statLife < plr.statLifeMax && !plr.dead)
					{
						tsplr.Heal(plr.statLifeMax2 - plr.statLife);
					}
					    tsplr.SetBuff(1, Int16.MaxValue); //Obsidian Skin
					    tsplr.SetBuff(3, Int16.MaxValue); // Swiftness
					    tsplr.SetBuff(11, Int16.MaxValue); // Shine
					    tsplr.SetBuff(12, Int16.MaxValue); // Night owl
					//Removed panic, issues of unobtainable buffs tsplr.SetBuff(63, Int16.MaxValue); // Panic
					    tsplr.SetBuff(104, Int16.MaxValue); // Mining
					    tsplr.SetBuff(107, Int16.MaxValue); // Builder
					    tsplr.SetBuff(113, Int16.MaxValue); // Lifeforce
					    tsplr.SetBuff(104, Int16.MaxValue); // Mining
					    tsplr.SetBuff(107, Int16.MaxValue); // Builder
					    tsplr.SetBuff(113, Int16.MaxValue); // Lifeforce
					    tsplr.SetBuff(207, Int16.MaxValue); // Exquisitely Stuffed aka well fed t3

				}
			}
		}
		void OnGetData(GetDataEventArgs e)
		{
			if (!e.Handled && TShock.Players[e.Msg.whoAmI].GetData<bool>("buildmode"))
			{
				Player plr = Main.player[e.Msg.whoAmI];
				TSPlayer tsplr = TShock.Players[e.Msg.whoAmI];

				switch (e.MsgID)
				{
					case PacketTypes.PlayerHurtV2:
                       				 using (MemoryStream data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
							{
							    data.ReadByte();
							    PlayerDeathReason.FromReader(new BinaryReader(data));
							    int damage = data.ReadInt16();
							    tsplr.Heal((int)Terraria.Main.CalculateDamagePlayersTake(damage, plr.statDefense) + 2);
							}
						break;
					case PacketTypes.Teleport:
						if ((e.Msg.readBuffer[3] == 0))
						{
						    List<int> buffs = new List<int>(plr.buffType);
						    if (buffs.Contains(Terraria.ID.BuffID.ChaosState) && plr.inventory[plr.selectedItem].netID == Terraria.ID.ItemID.RodofDiscord) //rod 1326
						    {
							tsplr.Heal(plr.statLifeMax2 / 7);
						    }
						}
                        			break;
					case PacketTypes.PaintTile:
					case PacketTypes.PaintWall://0%
						{
							int count = 0;
							int type = e.Msg.readBuffer[7];
							Terraria.Item lastItem = null;
							foreach (Item i in plr.inventory)
							{
								if (i.paint == type)
								{
									lastItem = i;
									count += i.stack;
								}
							}
							if (count <= 10 && lastItem != null)
								tsplr.GiveItem(lastItem.type, lastItem.maxStack + 1 - count);
						}
						break;
					case PacketTypes.Tile://1%
						{
							int count = 0;
							int type = e.Msg.readBuffer[e.Index];
						    	switch (type)
							{
								case 1: //PlaceTile
								case 3: //PlaceWall
								case 21: //ReplaceTile
								case 22: //ReplaceWall
								    if (plr.inventory[plr.selectedItem].type != Terraria.ID.ItemID.StaffofRegrowth) //230
								    {
									int tile = e.Msg.readBuffer[e.Index + 5];
									if (tsplr.SelectedItem.tileWand > 0)
									    tile = tsplr.SelectedItem.tileWand;
									Item lastItem = null;
									foreach (Item i in plr.inventory)
									{
									    if (i.createTile == tile || i.createWall == tile)
									    {
										lastItem = i;
										count += i.stack;
									    }
									}
									if (count <= 10 && lastItem != null)
									    tsplr.GiveItem(lastItem.type, lastItem.maxStack + 1 - count);
								    }
								    break;
								    // Placing wires
								case 5:  //Red
								case 10: //Blue
								case 12: //Green
								case 16: //Yellow
								    foreach (Item i in plr.inventory)
								    {
									if (i.type == Terraria.ID.ItemID.Wire) //530
									    count += i.stack;
								    }
								    if (count <= 10)
									tsplr.GiveItem(Terraria.ID.ItemID.Wire, 1000 - count);
								    break;

								case 8: //Place Actuator
								    foreach (Item i in plr.inventory)
								    {
									if (i.type == Terraria.ID.ItemID.Actuator) //849
									    count += i.stack;
								    }
								    if (count <= 10)
									tsplr.GiveItem(Terraria.ID.ItemID.Actuator, 1000 - count);
								    break;
						    	}
						}
						break;
				}
			}
		}
		void OnSendBytes(SendBytesEventArgs e)
		{
			if (TShock.Players[e.Socket.Id] == null)
				return;

			bool build = TShock.Players[e.Socket.Id].GetData<bool>("buildmode");
			switch (e.Buffer[2])
			{
				case (byte)PacketTypes.WorldInfo: //7 
					using (var writer = new BinaryWriter(new MemoryStream(e.Buffer, 3, e.Count - 3)))
					{
						writer.Write(build ? 27000 : (int)Main.time);
						BitsByte bb = 0;
						bb[0] = build ? true : Main.dayTime;
						bb[1] = build ? false : Main.bloodMoon;
						bb[2] = build ? false : Main.eclipse;
						writer.Write(bb);

						writer.BaseStream.Position += 9;
						writer.Write(build ? (short)Main.maxTilesY : (short)Main.worldSurface);
						writer.Write(build ? (short)Main.maxTilesY : (short)Main.rockLayer);

						writer.BaseStream.Position += 4;
						writer.Write(Main.worldName);
						writer.Write(Main.ActiveWorldFileData.UniqueId.ToString());

						writer.BaseStream.Position += 49;
						writer.Write(build ? 0f : Main.maxRaining);
					}
					break;
				case (byte)PacketTypes.TimeSet: //18
					using (var writer = new BinaryWriter(new MemoryStream(e.Buffer, 3, e.Count - 3)))
					{
						writer.Write(build ? true : Main.dayTime);
						writer.Write(build ? 27000 : (int)Main.time);
					}
					break;
				case (byte)PacketTypes.MassWireOperationPay:
				    TSPlayer tsplr = TShock.Players[e.Buffer[7]];
				    if (tsplr.GetData<bool>("buildmode"))
				    {
					e.Handled = true; //Will never decrement wires/actuators in inventory
				    }
				    break;
				case (byte)PacketTypes.NpcUpdate: //23
					NPC npc = Main.npc[BitConverter.ToInt16(e.Buffer, 3)];
					if (!npc.friendly)
					{
						Buffer.BlockCopy(BitConverter.GetBytes(build ? 0f : npc.position.X), 0, e.Buffer, 5, 4);
						Buffer.BlockCopy(BitConverter.GetBytes(build ? 0f : npc.position.Y), 0, e.Buffer, 9, 4);
					}
					break;
				case (byte)PacketTypes.ProjectileNew: //27
					short id = BitConverter.ToInt16(e.Buffer, 3);
					int owner = e.Buffer[21];
                    			Projectile proj = Main.projectile[TShock.Utils.SearchProjectile(id, owner)];
					if (!proj.friendly)
						Buffer.BlockCopy(BitConverter.GetBytes((short)(build ? 0 : proj.type)), 0, e.Buffer, 22, 2);
					break;
			}
		}

		void BuildModeCmd(CommandArgs e)
		{
			if (e.TPlayer.hostile)
			{
				e.Player.SendErrorMessage("You cannot enable Buildmode with PvP active!");
				return;
			}

			e.Player.SetData<bool>("buildmode", !e.Player.GetData<bool>("buildmode"));

			e.Player.SendSuccessMessage((e.Player.GetData<bool>("buildmode") ? "En" : "Dis") + "abled build mode.");
			// Time
            		e.Player.SendData(PacketTypes.WorldInfo);
			// NPCs
			for (int i = 0; i < 200; i++)
			{
				if (!Main.npc[i].friendly)
					e.Player.SendData(PacketTypes.NpcUpdate, "", i);
			}
			// Projectiles
			for (int i = 0; i < 1000; i++)
			{
				if (!Main.projectile[i].friendly)
					e.Player.SendData(PacketTypes.ProjectileNew, "", i);
			}
		}

		void BMCheck(CommandArgs args)
		{
			var plStr = String.Join(" ", args.Parameters);

			var ply = TShockAPI.TSPlayer.FindByNameOrID(plStr);
			if (ply.Count < 1)
			{
				args.Player.SendErrorMessage("No players matched that name!");
			}
			else if (ply.Count > 1)
			{
				args.Player.SendMultipleMatchError(ply.Select(p => p.Name));
			}
			else
			{
				if (ply[0].GetData<bool>("buildmode"))
				{
					args.Player.SendInfoMessage(ply[0].Name + " has Buildmode enabled!");
				}
				else
					args.Player.SendInfoMessage(ply[0].Name + " does not have Buildmode enabled.");
			}
		}
	}
}
