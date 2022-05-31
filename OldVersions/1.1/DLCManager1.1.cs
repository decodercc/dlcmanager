using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Blueprints Manager (DLCManager Mod)", "Decoder", "1.1.0")]
    [Description("Let your users know all DLC blueprints at join!")]
    public class DLCManager : RustPlugin
    {
        #region Vars
        
        private Blueprints data = new Blueprints();
        private const string permDefault = "dlcmanager.default";
        private const string permAdmin = "dlcmanager.admin";
        
        private class Blueprints
        {
            public List<int> workbench1 = new List<int>();
            public List<int> workbench2 = new List<int>();
            public List<int> workbench3 = new List<int>();
            public List<int> allBlueprints = new List<int>();
            public List<int> defaultBlueprints = new List<int>();
        } 
        
        #endregion

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permDefault, this);
            cmd.AddConsoleCommand("dlcmanager", this, nameof(cmdBlueprintsConsole));
        }
        
        private void OnServerInitialized()
        {
            GiveDLC();
            CheckPlayers();
        }
        
        private void OnPlayerConnected(BasePlayer player)
        {
            CheckPlayer(player);
        }

        #endregion

        #region Core

        private void GiveDLC()
        {
            foreach (var bp in ItemManager.bpList)
            {
                if (bp.userCraftable && bp.NeedsSteamDLC)
                {
                    var itemID = bp.targetItem.itemid;
                    var shortname = bp.targetItem.shortname;
                    if (config.blacklist?.Contains(shortname) ?? false)
                    {
                        continue;
                    }

                    if (config.defaultBlueprints?.Contains(shortname) ?? false)
                    {
                        data.defaultBlueprints.Add(itemID);
                    }
                    
                    data.allBlueprints.Add(itemID);
                }
            }
        }

        private void CheckPlayers()
        {
            timer.Once(1f, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    OnPlayerConnected(player);
                }
            });
        }

        private void CheckPlayer(BasePlayer player)
        {
            var blueprints = GetBlueprints(player);
            UnlockBlueprints(player, blueprints);
        }

        private List<int> GetBlueprints(BasePlayer player)
        {
            var list = new List<int>();

            if (permission.UserHasPermission(player.UserIDString, permDefault))
            {
                list.AddRange(data.allBlueprints);
                return list;
            }

            return list;
        }

        private void UnlockBlueprints(BasePlayer player, List<int> blueprints)
        {
            var info = player.PersistantPlayerInfo;
             
            foreach (var blueprint in blueprints)
            {
                if (info.unlockedItems.Contains(blueprint) == false)
                {
                    info.unlockedItems.Add(blueprint);
                }
            }
            
            player.PersistantPlayerInfo = info;
            player.SendNetworkUpdateImmediate();
            player.ClientRPCPlayer(null, player, "UnlockedBlueprint", 0);
        } 

        private void ResetBlueprints(BasePlayer player)
        {
            var info = player.PersistantPlayerInfo;
            info.unlockedItems = new List<int>();
            player.PersistantPlayerInfo = info;
            player.SendNetworkUpdateImmediate();
            player.ClientRPCPlayer(null, player, "UnlockedBlueprint", 0);
        }

        #endregion
        
        #region Configuration 1.1.2

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Blacklist")]
            public List<string> blacklist = new List<string>();
            
            [JsonProperty(PropertyName = "Default blueprints")]
            public List<string> defaultBlueprints = new List<string>();
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                blacklist = new List<string>
                {
                    "shortname",
                    "shortname",
                    "shortname",
                },
                defaultBlueprints = new List<string>
                {
                   "shortname",
                   "shortname",
                   "shortname",
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                
                timer.Every(10f, () =>
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                });
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
    }
}