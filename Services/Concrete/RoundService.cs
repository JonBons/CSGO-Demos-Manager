﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Models;
using Core.Models.Events;
using DemoInfo;
using Services.Interfaces;
using Services.Models.Timelines;

namespace Services.Concrete
{
	public class RoundService : IRoundService
	{
		public async Task<List<RoundEvent>> GetTimeLineEventList(Demo demo, Round round)
		{
			List<RoundEvent> roundEventList = new List<RoundEvent>();
			await Task.Factory.StartNew(() =>
			{
				foreach (KillEvent e in demo.Kills)
				{
					if (e.RoundNumber == round.Number)
					{
						roundEventList.Add(new RoundEvent
						{
							StartTime = DateTime.Today.AddSeconds(e.Seconds - round.StartTimeSeconds),
							EndTime = DateTime.Today.AddSeconds(e.Seconds - round.StartTimeSeconds + 1),
							Category = Properties.Resources.Kills,
							Message = string.Format(Properties.Resources.Killed, e.KillerName, e.KilledName),
							Type = "kill"
						});
					}
				}
				foreach (WeaponFireEvent e in demo.WeaponFired)
				{
					if (e.RoundNumber == round.Number)
					{
						string type = string.Empty;
						string message = string.Empty;
						string category = string.Empty;
						switch (e.Weapon.Element)
						{
							case EquipmentElement.Flash:
								type = "flash";
								category = Properties.Resources.Flashbang;
								message = string.Format(Properties.Resources.ThrownFlashbang, e.ShooterName);
								break;
							case EquipmentElement.Smoke:
								type = "smoke";
								category = Properties.Resources.Smoke;
								message = string.Format(Properties.Resources.ThrownSmoke, e.ShooterName);
								break;
							case EquipmentElement.Decoy:
								type = "decoy";
								category = Properties.Resources.Decoy;
								message = string.Format(Properties.Resources.ThrownDecoy, e.ShooterName);
								break;
							case EquipmentElement.HE:
								type = "he";
								category = Properties.Resources.HE;
								message = string.Format(Properties.Resources.ThrownHeGrenade, e.ShooterName);
								break;
							case EquipmentElement.Molotov:
								type = "molotov";
								category = Properties.Resources.Molotov;
								message = string.Format(Properties.Resources.ThrownMolotov, e.ShooterName);
								break;
							case EquipmentElement.Incendiary:
								type = "incendiary";
								category = Properties.Resources.Molotov;
								message = string.Format(Properties.Resources.ThrownIncendiary, e.ShooterName);
								break;
						}

						if (type != string.Empty)
						{
							roundEventList.Add(new RoundEvent
							{
								StartTime = DateTime.Today.AddSeconds(e.Seconds - round.StartTimeSeconds),
								EndTime = DateTime.Today.AddSeconds(e.Seconds - round.StartTimeSeconds + 1),
								Category = category,
								Message = message,
								Type = type
							});
						}
					}
				}

				if (round.BombPlanted != null)
				{
					roundEventList.Add(new RoundEvent
					{
						StartTime = DateTime.Today.AddSeconds(round.BombPlanted.Seconds - round.StartTimeSeconds),
						EndTime = DateTime.Today.AddSeconds(round.BombPlanted.Seconds - round.StartTimeSeconds + 1),
						Category = Properties.Resources.Bomb,
						Message = string.Format(Properties.Resources.PlantedTheBombOnBombSite, round.BombPlanted.PlanterName, round.BombPlanted.Site),
						Type = "bomb_planted"
					});
				}

				if (round.BombDefused != null)
				{
					roundEventList.Add(new RoundEvent
					{
						StartTime = DateTime.Today.AddSeconds(round.BombDefused.Seconds - round.StartTimeSeconds),
						EndTime = DateTime.Today.AddSeconds(round.BombDefused.Seconds - round.StartTimeSeconds + 1),
						Category = Properties.Resources.Bomb,
						Message = string.Format(Properties.Resources.DefusedTheBombOnBombSite, round.BombDefused.DefuserName, round.BombDefused.Site),
						Type = "bomb_defused"
					});
				}
				if (round.BombExploded != null)
				{
					roundEventList.Add(new RoundEvent
					{
						StartTime = DateTime.Today.AddSeconds(round.BombExploded.Seconds - round.StartTimeSeconds),
						EndTime = DateTime.Today.AddSeconds(round.BombExploded.Seconds - round.StartTimeSeconds + 1),
						Category = Properties.Resources.Bomb,
						Message = string.Format(Properties.Resources.TheBombExplodedOnBombSite, round.BombExploded.Site),
						Type = "bomb_exploded"
					});
				}
			});

			return roundEventList;
		}

		public Task<Round> MapRoundValuesToSelectedPlayer(Demo demo, Round round, long playerSteamId = 0)
		{
			if (playerSteamId != 0)
			{
				int playerKillCount = round.Kills.Count(k => k.KillerSteamId == playerSteamId);
				round.KillCount = round.Kills.Count(k => k.KillerSteamId == playerSteamId);
				round.OneKillCount = playerKillCount == 1 ? 1 : 0;
				round.TwoKillCount = playerKillCount == 2 ? 1 : 0;
				round.ThreeKillCount = playerKillCount == 3 ? 1 : 0;
				round.FourKillCount = playerKillCount == 4 ? 1 : 0;
				round.FiveKillCount = playerKillCount == 5 ? 1 : 0;
				round.TradeKillCount = round.Kills.Count(k => k.KillerSteamId == playerSteamId && k.IsTradeKill);
				round.CrouchKillCount = round.Kills.Count(k => k.KillerSteamId == playerSteamId && k.IsKillerCrouching);
				round.JumpKillCount = round.Kills.Count(k => k.KillerSteamId == playerSteamId && k.KillerVelocityZ > 0);
				round.DamageHealthCount = round.PlayersHurted.Where(h => h.AttackerSteamId == playerSteamId)
					.Sum(h => h.HealthDamage);
				round.DamageArmorCount = round.PlayersHurted.Where(h => h.AttackerSteamId == playerSteamId)
					.Sum(h => h.ArmorDamage);
				round.AverageHealthDamagePerPlayer = demo.Rounds.Count > 0 ? Math.Round((double)round.DamageHealthCount / demo.Rounds.Count, 1) : 0;
				round.BombExplodedCount = round.BombExploded != null && round.BombExploded.PlanterSteamId == playerSteamId ? 1 : 0;
				round.BombPlantedCount = round.BombPlanted != null && round.BombPlanted.PlanterSteamId == playerSteamId ? 1 : 0;
				round.BombDefusedCount = round.BombDefused != null && round.BombDefused.DefuserSteamId == playerSteamId ? 1 : 0;
				round.FlashbangThrownCount = round.WeaponFired.Count(e => e.ShooterSteamId == playerSteamId && e.Weapon.Element == EquipmentElement.Flash);
				round.SmokeThrownCount = round.WeaponFired.Count(e => e.ShooterSteamId == playerSteamId && e.Weapon.Element == EquipmentElement.Smoke);
				round.HeGrenadeThrownCount = round.WeaponFired.Count(e => e.ShooterSteamId == playerSteamId && e.Weapon.Element == EquipmentElement.HE);
				round.DecoyThrownCount = round.WeaponFired.Count(e => e.ShooterSteamId == playerSteamId && e.Weapon.Element == EquipmentElement.Decoy);
				round.MolotovThrownCount = round.WeaponFired.Count(e => e.ShooterSteamId == playerSteamId && e.Weapon.Element == EquipmentElement.Molotov);
				round.IncendiaryThrownCount = round.WeaponFired.Count(e => e.ShooterSteamId == playerSteamId && e.Weapon.Element == EquipmentElement.Incendiary);
			}

			return Task.FromResult(round);
		}
	}
}
