using PlayerCorpse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace dummyplayer.src
{
    public class Config
    {
        public int SECONDS_PVP_TAG_TIMER { get; set; } = 30;

        public bool DROP_ARMOR = false;

        public bool DROP_CLOTHS = false;

        public bool DROP_HOTBAR = true;

        public bool DROP_BAGS = true;

        public bool KILL_AFTER_LOGIN = true;

        public int TIME_TO_DISAPPEAR = 30;

        public bool SPAWN_CLONE_ON_PLAYER_LEAVE = false;
        public bool CLONE_IS_DAMAGABLE = true;

        public int TIME_TO_DISAPPEAR_WITH_SPAWN_CLINE_ON_PLAYER_LEAVE = 0;
        public void loadDatabaseConfig(ICoreAPI api)
        {
            try
            {              
                dummyplayer.config = api.LoadModConfig<Config>("dummyplayer.json");
                if (dummyplayer.config != null)
                {
                    api.StoreModConfig<Config>(dummyplayer.config, "dummyplayer.json");
                    return;
                }
            }
            catch (Exception e)
            {
            }

            dummyplayer.config = new Config();
            api.StoreModConfig<Config>(dummyplayer.config, "dummyplayer.json");
            return;
        }
    }
}
