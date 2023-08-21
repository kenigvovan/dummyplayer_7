using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace berg.src
{
    //
    //https://github.com/DArkHekRoMaNT
    //
    public class Config
    {
        public static Config Current { get; set; } = new Config();
        public class Part<Config>
        {
            public readonly string Comment;
            public readonly Config Default;
            private Config val;
            public Config Val
            {
                get => (val != null ? val : val = Default);
                set => val = (value != null ? value : Default);
            }
            public Part(Config Default, string Comment = null)
            {
                this.Default = Default;
                this.Val = Default;
                this.Comment = Comment;
            }
            public Part(Config Default, string prefix, string[] allowed, string postfix = null)
            {
                this.Default = Default;
                this.Val = Default;
                this.Comment = prefix;

                this.Comment += "[" + allowed[0];
                for (int i = 1; i < allowed.Length; i++)
                {
                    this.Comment += ", " + allowed[i];
                }
                this.Comment += "]" + postfix;
            }
        }
        public Part<bool> test { get; set; } = new Part<bool>(true);
        public Part<int> SECONDS_PVP_TAG_TIMER { get; set; } = new Part<int>(30);

        public Part<bool> DROP_ARMOR = new Part<bool>(false);

        public Part<bool> DROP_CLOTHS = new Part<bool>(false);

        public Part<bool> DROP_HOTBAR = new Part<bool>(true);

        public Part<bool> DROP_BAGS = new Part<bool>(true);

        public Part<bool> KILL_AFTER_LOGIN = new Part<bool>(true);

        public Part<int> TIME_TO_DISAPPEAR = new Part<int>(30);
    }
}